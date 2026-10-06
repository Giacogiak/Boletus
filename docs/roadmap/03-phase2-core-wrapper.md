# 03 — Phase 2: `Boletus.Core` P/Invoke wrapper

> Detail for the [roadmap](README.md). **Status: DONE (2026-06-17).** The thin, idiomatic,
> Rhino-free managed layer over the (then 9-entry-point) C ABI, with a verified boundary.
> *(DualC v0.3.0 added two `*_with_meshes` entry points — 11 total; both are now wrapped and
> `DualcField.FromJson(json, meshes)` is implemented — the diskless-mesh flip, DONE 2026-06-19,
> [07 §1](07-upstream-coordination/01-in-memory-mesh.md#1-in-memory-mesh-source-resolver--done-dualc-v030-upstream--the-boletus-side-flip-both-2026-06-19).)*

## Goal

A managed wrapper that lets C# code (1) build a field from a field-graph string, (2)
contour it to an in-memory mesh (the proxy), and (3) export it to a file — with C#
exceptions instead of integer codes, `IDisposable`/deterministic cleanup, and managed
arrays instead of raw pointers. No Rhino dependency (keeps it unit-testable and reusable).

## What was built

Solution at `D:\Boletus` (one `.sln`): `Boletus.Core` + `Boletus.Core.Tests`. The
`Boletus.Grasshopper` project is deferred to Phase 3.

| File | Role |
|---|---|
| `native/x64/dualc_capi.dll` | **Vendored** native C ABI (see [02](02-dependency-strategy.md)). |
| `src/Boletus.Core/Boletus.Core.csproj` | netstandard2.0; x64; copies the native DLL to output (flows to consumers). |
| `src/Boletus.Core/NativeMethods.cs` | P/Invoke for all 11 entry points (9 Phase-1 + the two v0.3.0 `*_with_meshes` twins); blittable `ContourParamsNative` (flattened `double[3]`) + `MeshNative` + `DualcMeshSourceNative` — [07 §1](07-upstream-coordination/README.md). |
| `src/Boletus.Core/DualcFieldHandle.cs` | `SafeHandleZeroOrMinusOneIsInvalid` — release-exactly-once lifetime; NULL handle treated invalid. |
| `src/Boletus.Core/Native.cs` | UTF-8 `byte[]` marshaling helpers, `Marshal.ReadByte` string read, `DualcStatus` enum, `DualcException`. |
| `src/Boletus.Core/DualcContourParams.cs` | Idiomatic params + `Default()` (from `dualc_default_params`) + `ToNative()`. |
| `src/Boletus.Core/DualcMeshData.cs` | Managed `float[]`/`int[]` copy of a contoured mesh; `CopyFrom(in MeshNative)`. |
| `src/Boletus.Core/DualcField.cs` | Public surface: `Version`, `FromExpr`/`FromJson`, `Contour`, `Export`, `ExportTiledStl`, `Dispose`. |
| `tests/Boletus.Core.Tests/` | xUnit (net9, x64): boundary/marshaling gate, error paths, CLI parity. |

### Key design choices (and why)

- **netstandard2.0** for `Boletus.Core`: one wrapper loads in Rhino 8's .NET 7 host **and**
  a .NET Framework 4.8 host **and** the net9 test runner. The cost is no
  `[LibraryImport]` / `Marshal.PtrToStringUTF8`, handled by manual UTF-8 `byte[]` /
  `Marshal.ReadByte`.
- **`SafeHandle`** (not raw `IntPtr`) for the field handle: finalizer-guaranteed,
  release-exactly-once cleanup even under partial-trust / async teardown; passed directly
  to P/Invoke so the runtime ref-counts it across the call.
- **Blittable interop structs** (split `double[3]` → scalars): exact layout match, no
  custom marshaling — the fastest, least-ambiguous path. `DualcMesh` extraction always
  copies out then `dualc_mesh_release`s in a `finally`.
- **Status → exception** mapping: every non-OK return becomes a `DualcException` carrying
  the `DualcStatus` code and the native message (with its locator for graph errors).

## Verification — 9/9 tests green

The marshaling gate uses DualC's **verified golden counts** (DualC `docs/roadmap/14-c-abi/03-implementation-and-verification.md` § 8; the one Boletus home is [design 07](../design/07-invariants-and-limits.md#the-golden-contour-counts)): a single
exact-equality assertion validates struct layout/packing, string marshaling, and array
extraction at once (a silent struct-padding bug shifts the counts rather than failing to
compile).

| Test | What it proves |
|---|---|
| `Version_is_non_empty` | DLL loads; pointer→string read works. |
| `DefaultParams_match_documented_library_defaults` | `dualc_default_params` struct round-trips (7/3/0/false/true/0). |
| `Contour_analytic_graph_hits_the_golden_counts` | **101,476 v / 163,740 t** at `maxDepth=6` — the core marshaling gate. |
| `Contour_can_run_repeatedly_on_one_field` | Field is reusable; handle lifetime stable. |
| `Export_writes_a_nontrivial_binary_stl` | Export path + extension dispatch; file > 84 bytes. |
| `FromExpr_with_positional_thickness_throws_graph_error_with_locator` | `DUALC_ERR_GRAPH` (Code 3) mapping + locator message. |
| `Contour_unbounded_field_without_bounds_throws_bounds_error` | `DUALC_ERR_BOUNDS` (Code 1) mapping for an infinite field. |
| `Contour_through_a_mesh_path_source_hits_the_golden_counts` | File-path mesh resolver: **70,032 v / 120,612 t** (the headline Rhino-mesh path). |
| `Wrapper_export_is_byte_identical_to_the_cli` | Wrapper STL == `dualc_field.exe` STL (**SHA-1 match**); skips if CLI absent. |

Also verified: the vendored `dualc_capi.dll` is **auto-copied to a referencing project's
output** (the test bin contains it) — confirming the deployment story for the future
`.gha`.

### NuGet note

Phase 2's test packages restore from the **local NuGet cache**, pinned to cached versions
(`Microsoft.NET.Test.Sdk 17.12.0`, `xunit 2.9.3`, `xunit.runner.visualstudio 2.8.2`).
Online managed-package restore is allowed in general (clarified 2026-06-18 — the only hard
rule is that the *native code* is never published; Phase 3b's `.gha` build, for instance,
restores Rhino/GH packages online). See [00](00-references-and-environment.md) and the
[local-only convention](../design/06-conventions.md#local-only) (decision D-02). *(2026-10-06:
D-02 reversed by D-46 — the repository is public; the hard rule is that no native binary is
committed, [10](10-public-delivery/README.md).)*

## RAM caution

Keep all interactive/test contours at **coarse depth**. A deep contour of a dense lattice
is exponential (~4–8× per `maxDepth` level) and is what OOM'd the box earlier; the golden
counts and all tests run at `maxDepth=6`.

## Not done here (Phase 3+)

The wrapper is signature-agnostic (it passes a string), so **op param signatures are not
pinned here** — that concern lives in the serializer ([04](04-phase3-field-graph-serializer.md)).
No Rhino types, no GH components, no native-DLL resolver yet (those are Phase 3).

---

← Back to the [roadmap](README.md) · prev: [02 — Dependency strategy](02-dependency-strategy.md) · next: [04 — Field-graph serializer](04-phase3-field-graph-serializer.md)
