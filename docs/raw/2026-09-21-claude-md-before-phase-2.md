# CLAUDE.md as it stood before roadmap 09 Phase 2

Snapshot taken 2026-09-21, before the file became the one-line `@AGENTS.md` import. Its
content is dispersed by the plan: the status narrative to `docs/roadmap/README.md`, the
build commands to `AGENTS.md`, the architecture and gotchas to `docs/design/` (Phase 3),
the DualC pointers to `docs/README.md` and `native/README.md`. Kept verbatim so nothing is
lost while Phase 3 harvests it. Never edited.

---

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Boletus is the .NET / Rhino 8 / Grasshopper front-end for **DualC** (a separate C++
implicit-field engine at `D:\DualC`). It drives DualC's field-graph engine through DualC's
native C ABI (`dualc_capi.dll`). **Read [`docs/roadmap/roadmap.md`](../../docs/roadmap/README.md)
first, and [`docs/roadmap/00-references-and-environment.md`](../../docs/roadmap/00-references-and-environment.md)
to pick the project up cold** — those docs are the authoritative plan and the index of
external sources of truth.

**Current state:** Phase 2 (`Boletus.Core` C-ABI wrapper) and Phase 3a (the managed field-graph
model + canonical-JSON serializer) are both DONE and verified. Phase 3a landed the **full ~60-op
vocabulary** in `src/Boletus.Core/FieldGraph/` (`FieldNode`/`FieldValue`, `FieldGraphSerializer`
that byte-matches DualC's `--dump-json`, `Ops` schema, `Field` builders for every op,
`FieldGraphValidator`): all 30 analytic primitives (7 grouped-key + 23 flat-`params`), 6 TPMS,
`mesh`/`winding`, the 8 booleans/graded, and the 14 decorators/domain ops — each op gated by a
`--dump-json` round-trip (70/70 when 3a landed; the suite is now **86/86** — see Phase 3b). See
[`docs/roadmap/04`](../../docs/roadmap/04-phase3-field-graph-serializer.md).

**Phase 3b (the Grasshopper `.gha`) is IN PROGRESS** — `src/Boletus.Grasshopper/`
(`net7.0-windows`, x64, output `.gha`). 3b.1 (thin slice, verified live in Rhino) + 3b.2
(2026-06-18) + the **diskless-mesh flip** (2026-06-19) + **tiled STL export** (2026-06-19) +
**capped proxy preview** (2026-06-20) + **component icons** (2026-06-20) + **domain-warp decorators**
(Offset/Twist/Bend/Displace, 2026-06-25) + the **`isolate → thicken → skin → union` example graph**
(2026-07-03) are DONE; the rest of 3b.3 (broader primitives) is next.
Key design: components exchange a runtime **`Volume`** — a **meshless, lightweight** field-graph
(`FieldNode`) plus, for input-mesh leaves, in-memory `MeshBuffer`s; **input-mesh leaves never touch
disk** — at Contour/Export they are handed to DualC **in RAM** via `mesh(id=…)` + the v0.3.0
`*_with_meshes` create call (`VolumeResolver` rewrites the `mem://` placeholder to `id=`;
`DualcField.FromJson(json, meshes)` marshals the buffers, pinned for the call only). (Only an
explicit export `Path` writes a file — the user's `.stl`/`.obj`/`.3mf`.) The native-DLL
resolver (`GH_AssemblyPriority` → `NativeLibrary.SetDllImportResolver`) is registered against the
**Core** assembly (where the `[DllImport]`s live). Palette: `TPMS` (type+wavelength),
`Primitive`, `Mesh → Volume`, `Onion`/`Graded-onion` (boundary clip applied **after** the
thickness), `Normalize`, `Transform`, the domain warps `Offset`/`Twist`/`Bend`/`Displace`
(single-child decorators — twist/bend take an axis dropdown mapped int→`"x"/"y"/"z"` via
`AxisName`; displace takes a fn dropdown int→`"sine"/"gyroid"/"bumps"` + optional amplitude/frequency;
all four wrap the already-`--dump-json`-gated `Field.Offset/Twist/Bend/Displace` — **no Core
change**), `Boolean`, `Write to File` (the export-only terminal — **async + manual**, see below),
`Proxy preview` (**viewport-only** LOD — no Mesh output; depth **clamped** to a hard
ceiling `MaxProxyDepth=7` so it can never OOM; contours once in `SolveInstance` + caches, draws via
`DrawViewportMeshes`; shares `RhinoMeshConvert.ToRhinoMesh`), and the **strut-lattice trio**
(2026-07-10, DualC `d6b2808`): `Strut Lattice` (crystal dropdown `sc`/`bcc`/`fcc`/`octet` +
`Center`/`Wavelength`/`Radius`/optional `nodeRadius` taper — a true SDF so **metric, no Normalize**;
infinite → posts a clip `Remark`), `Graded Offset` (2-child `graded-offset` — grades a **solid**
strut radius; use it, not Onion, on struts), `Mix` (3-child `mix` value-lerp morph — watertight only
for a same-family radius morph). All three wrap already-`--dump-json`-gated `Field.*` builders — **no
serializer/validator change** (strut = `MetricSource`; `graded-offset` reuses `GradedOnion`; `mix`
reuses `HardBoolean`). **The DLL was re-vendored from `d6b2808`** — the ABI is unchanged, but the
field-graph *parser* is compiled inside the DLL, so the old `e345bf3` DLL rejects the new ops; note
the **version trap** (`dualc_version()` still returns `"dualc 0.3.0"` for both). **Component icons are
procedurally drawn** in `BoletusIcons.cs` (20 GDI+ glyphs, fresh `Bitmap` per `Icon` getter) —
**there are no PNG/embedded-resource icon assets**. Builds 0/0; **117/117** Core tests (golden-count
gates on the temp-file and in-memory mesh paths + onion-before-clip + the `VolumeResolver` rewrite +
the tiled-STL facet count + the `MeshMaterializingResolver` rewrite + the `ExportPlan` planning table +
the strut/graded-offset/mix `--dump-json` round-trips + clipped bcc/graded-offset/mix contours). See
[`docs/roadmap/05`](../../docs/roadmap/05-phase3-grasshopper-components/README.md).

**The `Write to File` terminal is async + manual (2026-07-05).** It replaces the old dual-purpose
`Contour / Export` (which wrote synchronously on the solve thread and froze GH for minutes on a heavy
tiled-STL). `WriteToFileComponent` is **export-only** (no Mesh output — preview is the `Proxy preview`
/ `Live Preview` job) and **never writes on auto-solve**: an **on-canvas Write button**
(`WriteToFileAttributes : GH_ComponentAttributes`, a momentary hit-tested capsule) launches the write
on a **dedicated long-running thread**, so GH stays responsive; a `System.Windows.Forms.Timer` ticks a
"Writing… Ns" `Message`; completion republishes via `RhinoApp.InvokeOnUiThread` →
`OnPingDocument().ScheduleSolution(…, ExpireSolution(false))`. **Single-flight** with **BUSY**
detection (an input-hash compared to the launch-time snapshot): a mid-write definition change reports
BUSY and never auto-restarts; clicking Write while busy queues **one** restart that fires on
completion. Path is **mandatory**; `Format` (int 0=STL/1=3MF) sets the extension automatically (bare
names gain one; a conflicting typed extension is replaced + warned), and `Mode` (int 0=Tiled/1=Mono)
picks the writer (3MF is always monolithic). The deterministic half — path/extension/strategy/tile
resolution — is the Rhino-free, unit-tested `Boletus.Core/Export/ExportPlan.cs`; the component owns
only the threading + native calls (`DualcField.Export`/`ExportTiledStl`, unchanged). **Constraint:**
the native export call **can't be hard-cancelled from .NET** (no `Thread.Abort` in net7; the C ABI
takes no cancel token), so a true mid-flight abort + a real progress bar are the deferred **Phase C**
upgrade (an upstream DualC per-tile progress/cancel callback — [`docs/roadmap/07 §7`](../../docs/roadmap/07-upstream-coordination/03-export-callback-and-strut-sync.md)).
**No unit test covers the component's thread/button/GH path** — gated by the manual Rhino smoke test,
same precedent as `LivePreviewComponent`.

**Phase 5 (raymarch preview side-car) is DONE (2026-07-03, verified live in Rhino).** The
`LivePreviewComponent` (Terminals) launches the vendored, self-contained GPU viewer
`dualc_field_view.exe` (GLFW/glad static, copied beside the `.gha`, located via the same
assembly-dir idiom as the DLL) as a **separate child process** on a temp `.json` of the connected
`Volume`. Because a separate process **can't read the in-RAM mesh buffers** (the `*_with_meshes`
channel reaches only the in-process DLL), the Rhino-free `MeshMaterializingResolver` (Core, twin of
`VolumeResolver`) writes any in-memory `mesh`/`winding` leaf to a **temp OBJ** and rewrites
`mem://`→`path=` (mesh-free graphs pass through). The viewer uses its **own** orbit/dolly camera —
**no Rhino-camera binding**. Component does atomic temp-`.json` writes + skip-if-unchanged + lifecycle
cleanup; **no unit test covers its Process/GL path** (it was gated by the manual Rhino smoke test).
**5b (upstream DualC):** `dualc_field_view` polls its input file's mtime and auto-reloads
(+ recompute-bounds-on-reload) — so rewriting the temp `.json` each solve updates the window on every
GH change, **no keypress, no IPC** (confirmed live in Rhino: a slider drag refreshes the viewport on
its own). The DualC-side change (`examples/dualc_field_view.cpp`, `libdualc` untouched) and its reason
are documented upstream in `D:\DualC\docs\roadmap\12 §D` + `command_reference/12`. Per-parameter
uniform push (the instant `[`/`]` tier) stays deferred — it needs a real socket/stdin channel. See
[`docs/roadmap/07 §5`](../../docs/roadmap/07-upstream-coordination/02-viewer-and-uniform-push.md).

## Commands

Windows x64, .NET SDK 9. The native `dualc_capi.dll` is vendored, so no C++ toolchain or
DualC checkout is needed to build/test Boletus.

```sh
dotnet build src/Boletus.Core/Boletus.Core.csproj -c Debug
dotnet test  tests/Boletus.Core.Tests/Boletus.Core.Tests.csproj -c Debug

# the Grasshopper .gha (net7.0-windows; first restore goes online — see NuGet note)
dotnet build src/Boletus.Grasshopper/Boletus.Grasshopper.csproj -c Debug
# or the whole solution
dotnet build Boletus.sln -c Debug

# run a single test
dotnet test tests/Boletus.Core.Tests/Boletus.Core.Tests.csproj \
  --filter "FullyQualifiedName~Contour_analytic_graph_hits_the_golden_counts"
```

**NuGet: online restore is allowed** (clarified 2026-06-18 — the only hard rule is that the
**native code is never published/exposed**; managed-package restore from nuget.org is fine).
The `Boletus.Grasshopper` build restores the `Grasshopper` 8.x metapackage + the
net7/WindowsDesktop reference packs online (the `Grasshopper`/`RhinoCommon` packages are
net48-ref, consumed via `AssetTargetFallback` → **NU1701**, suppressed in the csproj). The
test project's packages are pinned to cached versions (`Microsoft.NET.Test.Sdk 17.12.0`,
`xunit 2.9.3`, `xunit.runner.visualstudio 2.8.2`).

## Architecture (the big picture)

```
Boletus.Grasshopper (.gha, net7.0-windows)   ← Phase 3b — GH palette + Rhino glue (3b.1+3b.2 + diskless-mesh flip + tiled STL + capped proxy preview + icons DONE; only VolumeGoo/VolumeParameter + components; rest of 3b.3 pending)
        │ project reference
Boletus.Core (netstandard2.0, NO Rhino dep)  ← P/Invoke wrapper + field-graph model/serializer (FieldGraph/, full op set) + the Rhino-free Volume datatype (Volume/VolumeResolver/FieldTree)
        │ P/Invoke (Cdecl, x64)
dualc_capi.dll (native C ABI, 11 entry points) ← vendored at native/x64/ (DualC 0.3.0, commit d6b2808)
```

**The pivotal property: the field-graph *string* is the construction API.** DualC's C ABI
has **no per-primitive factories** — you compose a shape by sending one JSON or `--expr`
string (`DualcField.FromJson`/`FromExpr`) and get back a proxy mesh and/or a file export.
Consequences:
- `Boletus.Core` is thin and signature-agnostic; new DualC ops grow the *string vocabulary*,
  not the C surface, so the wrapper doesn't change when DualC adds a primitive.
- The real work is the **managed field-graph serializer** (Phase 3a): GH components build a
  managed node tree → canonical JSON → the wrapper. Op tokens/params/defaults must be pinned
  from DualC's `docs/command_reference/11-dualc_field/01-op-vocabulary.md` + `dualc_field --list` /
  `--dump-json` (not from memory), and verified per-op by diffing against `--dump-json`.

**`Boletus.Core` is deliberately Rhino-free** so it unit-tests on a plain runner and is
reusable. Rhino-specific concerns (the native-DLL resolver at plugin startup, Rhino↔DualC
mesh conversion, temp-file mesh export, unit scaling) belong in `Boletus.Grasshopper`, not
here. The interop is a SafeHandle (`DualcFieldHandle`) + blittable structs + UTF-8 `byte[]`
marshaling (netstandard2.0 has no `[LibraryImport]`/`PtrToStringUTF8`).

## Project-specific gotchas

- **LOCAL ONLY — never publish.** No `git push`/PR, no `yak push`, no NuGet publish without
  the user's explicit authorization. Local commits and local builds are fine.
- **Keep contours coarse** (`maxDepth ≈ 6`). A deep contour of a dense lattice is
  exponential (~4–8× per level) and has OOM'd the machine. All tests run at `maxDepth=6`.
- **Golden counts are the marshaling gate.** `maxDepth=6`, default params:
  analytic `intersection(onion(gyroid(wavelength=0.5),thickness=0.12),box(min=[-1,-1,-1],max=[1,1,1]))`
  → 101,476 v / 163,740 t; mesh variant (thickness=0.1, unit cube) → 70,032 v / 120,612 t — the
  same counts whether the cube reaches DualC via `mesh(path="cube.obj")` or in-memory
  `mesh(id="cube")` (both gated in `MeshBufferTests`). A shifted count signals a
  struct-layout/marshaling regression.
- **Mesh sources reach DualC in RAM (no disk) — DualC v0.3.0.** To clip a Rhino mesh, marshal it
  to a `MeshBuffer`, reference it as `mesh(id="…")`, and build via
  `DualcField.FromJson(json, meshes)` (the `dualc_field_create_from_json_with_meshes` entry point);
  the buffers are **copied during the create call**, so they're pinned for the call only and need
  not outlive the field. (`mesh(path=…)` from disk still works for assets already on disk. The old
  temp-OBJ materializer was **removed** once the in-memory path landed — recoverable from git if a
  pre-0.3.0 DLL ever needs supporting.)
- **A `DualcField` is single-threaded** — create one handle per Grasshopper solve; never
  share across parallel solves. Always let `Contour` release the native mesh (it does so in a
  `finally`); dispose the field.
- **`maxDepth` is the only proxy/export lever**; a coarse proxy of a *lattice* is lossy (thin
  walls drop out). `.3mf` export is 1 unit = 1 mm — scale Rhino doc units to mm on export.

## The DualC dependency

`dualc_capi.dll` is **vendored** under `native/x64/` (provenance in
[`native/README.md`](../../native/README.md): DualC 0.3.0, commit `d6b2808` — re-vendored 2026-07-10 for
the strut-lattice parser; the ABI is unchanged from `e345bf3`, but `dualc_version()` still reports
`"dualc 0.3.0"`, so pin by commit, not version). The managed
`Boletus.Core` ↔ `.gha` link is a **project reference**. To refresh the native DLL, rebuild
it in the DualC repo and update the provenance file — procedure in `native/README.md`. The
trade-off (NuGet vs vendored vs project-ref) and the migration path to a local NuGet feed are
in [`docs/roadmap/02-dependency-strategy.md`](../../docs/roadmap/02-dependency-strategy.md).
