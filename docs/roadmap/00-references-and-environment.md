# 00 — References, sources of truth & dev environment

> Detail for the [roadmap](README.md). Everything an agent or developer needs to **pick up
> the project cold**: where the authoritative DualC sources live, the fixtures and binaries
> to test against, and the dev-environment facts assumed throughout these docs. If a fact
> in another roadmap doc seems to need outside context, it is pinned here.

## The DualC dependency — what Boletus is built on

Boletus is a front-end for **DualC**, a separate C++ dual-contouring / implicit-field
engine. Its **field-graph** is the single source of truth: an immutable tree of ops
(sources → decorators/warps → booleans) that DualC can dual-contour (export) or compile to
GLSL (live preview). Boletus drives DualC through the finished **C ABI**. See
[01 — Architecture & contract](01-architecture-and-contract.md) for how the layers fit.

**DualC repository:** `D:\DualC` (local). Authoritative files (read these to extend Boletus):

| Path (under `D:\DualC`) | What it is | Why Boletus needs it |
|---|---|---|
| `capi/dualc_c.h` | The C ABI header — **the contract** (the entry points incl. the v0.3.0 `*_with_meshes` in-memory-mesh create twins + `DualcMeshSource`, the structs, the status codes). | The exact P/Invoke surface; the wrapper-side view is [design 01](../design/01-native-interop.md). |
| `capi/CSHARP_WRAPPER_HANDOFF.md` | Hand-over spec for the C# wrapper (reproduces the header + a reference implementation). | The blueprint Phase 2 followed. |
| `capi/dualc_c_demo.c` | Worked C harness exercising the ABI end-to-end. | The pattern the C# tests mirror. |
| `docs/roadmap/14-c-abi/` (the record: `03-implementation-and-verification.md` § 8 for the **golden test counts**; `04-abi-0-4-0.md` for the 0.4.0-and-later entries) | Full ABI development record + per-function reference. | Verification oracle; semantics. *(2026-09-21, roadmap 09 Phase 4: the retired `18-c-abi-continued` tombstone dropped from this row.)* |
| `docs/command_reference/11-dualc_field/` (vocabulary: `01-op-vocabulary.md`) | Field-graph **vocabulary**: op tokens, params, the `--expr` grammar, JSON shape. | The spec for the Phase 3a serializer. |
| `docs/command_reference/02-dualc_primitive.md` | The 30 analytic primitives + full positional parameter order. | Pinning primitive signatures. |
| `docs/command_reference/12-dualc_field_view/` | The GPU raymarch viewer (future side-car preview). | Phase-later live preview. |
| `docs/README.md` | DualC's own docs map: which layer owns which class of fact. | Where to look for any engine fact before restating it. |
| `docs/design/README.md` | The engine as it is: `08-implicit-field-layer.md` (the field layer), `09-conventions.md` (1 unit = 1 mm at export, the format dispatch, primitive parameter forms), `10-invariants-and-tolerances.md` (the resolution rule, `mix` blends values not shapes, the tile-depth range). | The engine facts Boletus links instead of restating. |
| `docs/decisions/README.md` | DualC's decisions index; the Boletus-driven rows are DualC D-12 (DAG-ref serialization), D-17 (section-plane sliders), D-21 (tiled 3MF / `--mem` behind the ABI), D-29 (viewer startup time). | The upstream side of Boletus's deferred asks. |
| `capi/README.md` | The C ABI's own page: entry-point list and version. | The count and the version live there, never here. |
| `docs/roadmap/README.md` | DualC's own roadmap index. | Broad context on engine capabilities. |
| `examples/samples/gyroid_box.json`, `examples/samples/mesh_lattice.json` | Ready-to-run sample field graphs. | Serializer round-trip fixtures. |
| `build/capi/Release/dualc_capi.dll` | The prebuilt C ABI (vendored into Boletus). | The native library we P/Invoke. |
| `build/examples/Release/dualc_field.exe` | The CLI: `--list` (op vocabulary), `--dump-json` (canonicalise), `-o` export. | Serializer pinning + CLI parity tests. |
| `build/examples/Release/dualc_field_view(.exe)` | The standalone **GPU raymarch viewer** (OpenGL 3.3 / GLFW); takes a field-graph (`.fld`/`.json`/`--expr`/stdin), `--snapshot` for headless PNG. | Phase 5 live preview (separate-process launch; binary to vendor — see [07](07-upstream-coordination/README.md)). GPU-bound, no headless/CI. |

**Vendored DLL provenance:** the DualC version and commit the current `dualc_capi.dll` was built
from, and the refresh procedure, are [`native/README.md`](../../native/README.md) — the one home of
that fact; the wrapper-side view of the ABI is [design 01](../design/01-native-interop.md).
*(2026-09-21, roadmap 09 Phase 3: this paragraph carried a commit hash that had drifted; it is a
link now that the provenance has one owner.)*

### Tools to lean on when building the serializer (Phase 3a)

- `dualc_field --list` — prints the live op vocabulary (authoritative op tokens).
- `dualc_field <graph> --dump-json` — canonicalises any `--expr`/JSON input to canonical
  JSON; the **round-trip oracle** for the serializer ([04](04-phase3-field-graph-serializer.md)).
- `dualc_field --expr "<g>" --depth N -o ref.stl` — produces a reference mesh; the wrapper's
  export is **byte-identical** (both route through the same writer).

## Dev environment (assumed by these docs)

| Item | Value / location |
|---|---|
| OS / arch | Windows 10, **x64** (matches Rhino 8). |
| .NET SDK | **9.0.307** installed (also 6.0, 5.0, 2.1). `Boletus.Core` = **netstandard2.0**; tests = **net9.0 x64**; `Boletus.Grasshopper` = **net7.0-windows** (resolved 2026-06-18 — Rhino 8.32's .NET Core host runs on the .NET 7 runtime, so net8/9 won't load). Ref packs on this box: WindowsDesktop/NETCore for net6/net8/net9 + net48 (no net7 ref pack was cached initially — restored online on first `.gha` build). |
| Rhino | **Rhino 8.32** (`8.32.26160.13001`) at `C:\Program Files\Rhino 8`. RhinoCommon ships twice: `…\System\RhinoCommon.dll` (**.NET Framework 4.8** build) and `…\System\netcore\RhinoCommon.dll` (**.NET 7** build); `Grasshopper.dll`/`GH_IO.dll` live in `…\Plug-ins\Grasshopper\`. Boletus references Rhino/GH via the compile-only `Grasshopper` 8.x NuGet metapackage (not these local DLLs); none are redistributed. |
| NuGet | **Online restore is allowed** (clarified 2026-06-18 — the only hard rule is that the *native code* is never published; managed-package restore from nuget.org is fine). Test packages are pinned to versions present in the local cache (`Microsoft.NET.Test.Sdk 17.12.0`, `xunit 2.9.3`, `xunit.runner.visualstudio 2.8.2`); the `Boletus.Grasshopper` build additionally restores the `Grasshopper` 8.0.x metapackage (net48 ref assemblies, consumed via `AssetTargetFallback` → NU1701, suppressed) + the net7/WindowsDesktop reference packs, online. |
| git | Boletus is a local git repo on `main`. **Local only — no push / no publish** (see below). |

## Canonical test oracle — the golden counts

The golden counts, the graphs that produce them, the tests that assert them and the coarse-depth
rule live in one place: [design 07 § The golden contour counts](../design/07-invariants-and-limits.md#the-golden-contour-counts).
*(2026-09-21, roadmap 09 Phase 3: the numbers that stood here moved to their one home; decision D-40.)*

## Standing constraints (recap)

The constraints as they hold — local only, MVP first with consolidated multi-mode components,
the meshless `Volume` currency and the diskless handoff — are
[design 06 § Local only](../design/06-conventions.md#local-only),
[design README § The `Volume` contract](../design/README.md#the-volume-contract) and
[design 03](../design/03-volume-and-resolvers.md); the decisions are D-02, D-03, D-13 and D-14
in the [decisions index](../decisions/01-settled.md). *(2026-09-21, roadmap 09 Phase 5, the first semantic lint: the three
bullets that restated them here reduced to this sentence.)* *(2026-10-06, roadmap 10: "local
only" is reversed by D-46 — Boletus is public, the git row's rule is "no push carrying
pre-publication history, no `yak push` or NuGet publish without the owner's say-so, no
committed binary"; and the DualC repository of the table is the git submodule
`external/DualC` at the pin, public at `https://github.com/Giacogiak/DualC` — the `D:\DualC`
paths stand as the sibling checkout of the time; [10](10-public-delivery.md).)*

---

← Back to the [roadmap](README.md) · next: [01 — Architecture & contract](01-architecture-and-contract.md)
