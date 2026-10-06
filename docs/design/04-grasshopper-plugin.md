# The Grasshopper plugin — `Boletus.Grasshopper`

The `.gha`: the component palette, the `Volume` wire type, the Rhino glue and the two pieces
of machinery that are not pure tree surgery — the background file writer and the external
viewer process. Everything with logic in it lives in `Boletus.Core`
([README § The layering](README.md#the-layering)); this project wires Grasshopper inputs to
the `Field` builders and the resolvers, and owns only what needs Rhino, a thread or a
process. What each component's inputs mean, their defaults and the exact messages they post
is the command reference's, not this page's.

## The project

`src/Boletus.Grasshopper/Boletus.Grasshopper.csproj`: **`net7.0-windows`**, x64, output
extension `.gha`, `UseWindowsForms` (the icons and the writer's ticker need it),
`EnableDynamicLoading`. Rhino 8's .NET Core host runs on the .NET 7 runtime, so a net8/net9
assembly does not load — the reason for the TFM. RhinoCommon and Grasshopper are referenced
through the **`Grasshopper` NuGet metapackage, compile-only** (`Private=false`, runtime assets
excluded): the real assemblies come from the running Rhino process and are never copied to
the output, so the shipped payload is exactly `Boletus.Grasshopper.gha`, `Boletus.Core.dll`,
`dualc_capi.dll` and `dualc_field_view.exe`. The metapackage carries net48 reference
assemblies, consumed by a net7 project via `AssetTargetFallback`, which emits **NU1701** —
expected for a Rhino 8 plugin build and suppressed in the csproj. The two native binaries —
built from the DualC submodule into `native/x64/` by `scripts/build_native.py`, never
committed — are copied beside the `.gha` by `Exists`-conditioned `<None>` items mirroring the
one in `Boletus.Core.csproj`. CI's Windows job packages those four files with
`yak/manifest.yml` into one `.yak`.

## The native-DLL resolver

`BoletusPriority.cs` is a `GH_AssemblyPriority`: `PriorityLoad()` runs once at Grasshopper
startup, before any component solves, and installs
`NativeLibrary.SetDllImportResolver(typeof(DualcField).Assembly, …)`. Two facts make it
necessary and shape it. The Windows loader searches `Rhino.exe`'s directory and the system
path when a `[DllImport("dualc_capi")]` fires — **not** the `.gha`'s folder — so the DLL
copied beside the `.gha` is not reliably found without help. And a resolver fires only for
P/Invokes declared in the assembly it is attached to, so it is registered against the **Core**
assembly, where `NativeMethods` lives, not against the `.gha`. The resolver intercepts
`dualc_capi` / `dualc_capi.dll` only, loads it from the `.gha`'s own directory with
`NativeLibrary.TryLoad`, and falls through (`IntPtr.Zero`) for any other library name.
`Boletus.Core` cannot host this itself: netstandard2.0 has no `NativeLibrary`.

## The wire type — `VolumeGoo` / `VolumeParameter`

`VolumeGoo.cs` is the `GH_Goo<Volume>` payload on every wire; `Duplicate()` shares the
immutable volume. `VolumeParameter.cs` is the `GH_Param<VolumeGoo>` (name `Volume`, nickname
`V`, `Exposure = hidden`: it is computed, never placed from the palette or persisted on the
canvas). Both are thin wrappers over the Rhino-free type of [03](03-volume-and-resolvers.md).

## The component families

All components sit on the **Boletus** tab in four sub-panels. A terminal creates one
`DualcField` per contour or write and disposes it in a `finally` — `Proxy preview` inside
`SolveInstance`, `Write to File` on its worker thread, where the whole create → export →
dispose lifetime stays
([01 § The single-threaded rule](01-native-interop.md#the-single-threaded-rule)); a
non-terminal never touches the engine at all.

- **Sources** (→ `Volume`): `TPMS` (family dropdown + wavelength, an infinite non-metric
  lattice), `Primitive` (type dropdown — box, sphere, roundbox, torus, plane, boxframe,
  ellipsoid — over a fixed `A / B / N0 / N1` input model; plane posts an infinite-extent
  remark), `Segment Primitive` and `Axial Primitive` (the other analytic primitives, grouped
  by parameter shape — two points + two numbers, one point + four numbers — over
  `PrimitiveCatalog` in Core: a fixed slot row whose names, tooltips and defaults follow the
  type dropdown, relabelled after each solve so a saved canvas never loses a wire; an empty
  number takes the shape's engine default, an angle slot is entered in degrees and converted;
  the two unbounded shapes post the remark), `Mesh → Volume`
  (a Rhino mesh → `MeshBuffer` + a `mesh` / `winding` leaf, its `Kind` choosing parity,
  pseudonormal or winding; always metric, [06](06-conventions.md#metric-by-default-sources-and-the-normalize-rule)),
  `Strut Lattice` (crystal dropdown `sc` / `bcc` / `fcc` / `octet`, center, wavelength,
  radius, optional node radius; metric, infinite, posts a remark).
- **Decorators** (`Volume` in → `Volume` out), all standalone: `Onion` (thickness + optional
  boundary, clipped after the shell), `Graded Onion` (base + control + `t1` / `t2` / `d1` /
  `d0` + optional boundary), `Normalize`, `Transform` (a Rhino `Transform` → the `transform`
  matrix), `Offset`, `Twist` and `Bend` (scalar + axis dropdown), `Displace` (function
  dropdown + optional amplitude / frequency, absent values deferring to DualC's defaults),
  `Graded Offset` (the solid-radius companion of `Onion`) and `Mix` (three volumes + `hi` /
  optional `lo`; posts the value-blend remark).
- **Booleans**: one `Boolean` component, an operation dropdown over the four hard and three
  smooth ops with `k` for the smooth ones (`k` supplied to a hard op warns).
- **Terminals**: `Write to File`, `Proxy preview`, `Live Preview` — the only components that
  hand a graph to DualC or to the viewer.

Dropdowns follow one pattern: a `Param_Integer` with `AddNamedValue` for every option and
the full option set repeated in the input description; the int → token maps (`AxisName.cs`
for `0/1/2 → "x"/"y"/"z"`, `0/1/2 → "sine"/"gyroid"/"bumps"` for `Displace`) use the exact
strings the vocabulary tests round-trip, and an out-of-range index posts an error. A GUID is
never reused across an input reorder ([06](06-conventions.md#the-guid-rule)).

## `Proxy preview` — the capped viewport LOD

`ProxyPreviewComponent.cs` draws a coarse contour of a volume directly in the viewport,
**viewport-only, no Mesh output**: the requested depth is clamped to `MaxProxyDepth`
([07](07-invariants-and-limits.md#maxproxydepth)) with a remark when it clamps, and the
component contours **once in `SolveInstance`** through `VolumeResolver` +
`FromJson(json, meshes)`, caches the `Rhino.Geometry.Mesh` and its bounding box, and only
draws the cache from `DrawViewportMeshes` (which fires on every pan, zoom and rotate);
`ClippingBox` returns the cached box so Zoom Extents frames it, and a failed or empty solve
clears the cache. On a library that `SupportsDiagnostics` the contour goes through the
diagnostics twin ([01](01-native-interop.md#the-public-surface--dualcfield)): an **empty
contour** — the engine's one-triangle placeholder for a field with no surface inside the
sampled region — posts a warning and draws nothing, and a proxy that is **not watertight**
(boundary edges, as where Min / Max cut through the surface, or non-manifold ones) posts a
remark carrying both edge counts; on an
older DLL the plain contour runs and nothing is said. Min / Max bounds are needed only for an unbounded field, with a friendly
`DualcStatus.Bounds` message like the writer's — two near-identical strings, both quoted on
the command reference. `RhinoMeshConvert.ToRhinoMesh`
(vertices from the `Positions` triples, faces from the `Indices` triples, normals from
`Normals` or `ComputeNormals()`) is the one Rhino-side copy, shared by whoever needs a Rhino
mesh. Lattice *fidelity* is not this component's job — a coarse contour of a dense lattice
drops thin walls; that is what the side-car is for.

## `Write to File` — the threading model

`WriteToFileComponent.cs` is **export-only** (outputs: a status string and the written path;
no mesh) and **never writes on auto-solve**. The deterministic half — path, extension,
strategy, tile depth — is `ExportPlan` in Core ([05](05-export-planning.md)); the component
owns the Grasshopper glue, the threading and the native calls — the `DualcField.Export` /
`ExportTiledStl` overloads with a progress sink and a cancellation token when the library
supports them (`DualcField.SupportsProgress`), the plain ones otherwise
([01 § Cancel and progress](01-native-interop.md#cancel-and-progress--the-050-twins)). The
two monolithic paths also take the engine's `DualcDiagnostics` when the library has them —
through the 0.5.0 twin on the cancellable path, through the 0.4.0 twin otherwise when
`DualcField.SupportsDiagnostics`: a completed write whose contour was empty — the file
holds the engine's one placeholder triangle — carries a warning into the next solve's `Done`
state, beside the status string; the tiled writer has no diagnostics in the ABI and reports
nothing, and the warning exists only on success, so cancel and restart never see it.

- **State machine.** One instance holds `RunState` — `Idle`, `Running`, `Done`, `Failed`,
  `Cancelled` — behind a lock, with the input hash captured when the running write
  launched and, on the cancellable path, the running write's `CancellationTokenSource`.
- **`SolveInstance` never launches work.** On every solve it reads the inputs, runs the
  validator, plans, resolves the graph (`VolumeResolver` → `ToJson`) and hashes it. If
  `Running` and the hash differs from the launch snapshot it posts **BUSY** and returns; if
  the button set `_runRequested` and nothing is running, it snapshots, flips to `Running`
  and launches; otherwise it publishes the last result. On a library without the cancel
  ABI it also posts a Remark saying so, on every solve.
- **The button.** `WriteToFileAttributes : GH_ComponentAttributes` reserves a strip under
  the capsule, draws a `GH_Capsule` and hit-tests a left click into `RequestRun()`.
  Momentary, not a toggle. Idle it reads `Write ▶` and a click sets `_runRequested` and
  expires the solution; while `Running` it reads `Cancel ■` (`Cancelling…` once clicked) and
  a click cancels the token source — the registration inside Core forwards that into the
  native token from the UI thread — and only redraws, because the completion callback
  schedules the solve. On the older library it reads `Writing…` and a click sets
  `_restartRequested`.
- **The worker.** A **dedicated long-running thread** (`Task.Factory.StartNew` with
  `TaskCreationOptions.LongRunning`, not a pool thread — a minutes-long blocking call would
  starve the pool) runs the whole create → export → dispose lifetime. DualC delivers progress
  on that thread (the one that made the P/Invoke), and the component's sink turns each
  report into one string in a volatile field — `tile i/T (p%)`, `sampling p%`,
  `contouring p%`, `writing file` — with no marshaling. On completion the worker marshals
  back with `RhinoApp.InvokeOnUiThread` → `OnPingDocument().ScheduleSolution(…,
  ExpireSolution(false))`, so the next solve flips `Running` → `Done` / `Failed` /
  `Cancelled` and publishes the result; an `OperationCanceledException` is the `Cancelled`
  outcome, with nothing on disk.
- **The ticker.** A UI-thread `System.Windows.Forms.Timer` updates the component `Message`
  once a second — `Writing… <progress> · Ns`, or `Writing… Ns` before the first report, or
  `Cancelling… …` after a cancel click — and redraws the canvas: a label redraw, no re-solve.
- **Single flight and cancel.** Never two concurrent native writes on one component
  ([07](07-invariants-and-limits.md#single-flight-writes-and-the-cancel-constraint)). A
  cancel is a cancel: the write ends `Cancelled` and the user clicks `Write ▶` again for a
  fresh one with the current inputs — no automatic relaunch, so a click meant to stop a
  runaway write never starts another. On the older library "abort" is the **queued
  restart**: a click while `Running` sets `_restartRequested` and the completion callback
  relaunches with the then-current inputs; a restart whose inputs equal the running snapshot
  is dropped.
- **Lifecycle.** `RemovedFromDocument` sets `_removed`, stops the ticker and cancels the
  running write, which leaves nothing at the path; on the older library the write finishes
  harmlessly. Either way its callback no-ops.

No unit test covers the thread, button or Grasshopper path — Rhino cannot run headless — so
the acceptance gate is a manual Rhino smoke test, the same precedent as `Live Preview`.

## `Live Preview` — the process model

`LivePreviewComponent.cs` launches **`dualc_field_view.exe`**, built beside the `.gha`, as a separate
child process on a file: the true-fidelity preview, because the viewer sphere-traces the exact
field with RAM bounded by the window rather than the lattice. The exe is found beside the
`.gha` with the same assembly-directory idiom the DLL resolver uses. Each solve serializes
the volume — through `MeshMaterializingResolver` into a **per-instance temp directory**
(`%TEMP%\Boletus\LivePreview\<InstanceGuid>`, so two components never collide) — and writes
`graph.json` **atomically** (a `.tmp` then `File.Move` with overwrite, so the watcher never
sees a half-written file), skipping the write when the JSON is unchanged. The viewer polls
the file's mtime and reloads on change — DualC's own feature, recorded in its
[12/03 § Disk file-watch](../../../DualC/docs/roadmap/12-field-graph-and-app/03-raymarch-app.md#disk-file-watch-auto-reload)
— so every canvas change reaches the window with **no keypress and no IPC**; every change
pays a shader recompile, and the per-parameter uniform push that would avoid it is a deferred
optimisation ([decisions](../decisions/README.md)). Optional Min / Max become `--bounds`. The
viewer keeps its **own** orbit/dolly camera; Rhino's camera is not bound. `On` toggles the
window; `RemovedFromDocument` kills the process and deletes the temp directory, best-effort,
while `AppDomain.ProcessExit` only kills the process. No unit test covers the process or GL path.

## The icons

`BoletusIcons.cs` draws every glyph **procedurally** with `System.Drawing` GDI+ primitives —
no image files, no embedded resources, no generator, no build step; the whole icon set is
reviewable C# and reproducible from source. Twenty-two `Bitmap` accessors: one per component
(`Write to File` draws the `ContourExport` glyph), one for the `Volume` parameter and one for
the assembly (the boletus mushroom on the ribbon tab and in the plugin manager). Each
accessor renders a **fresh 24×24 `Bitmap`** rather than a cached instance: Grasshopper
caches per owner and may dispose what it was handed, so a shared bitmap would be disposed out
from under its other owners (the resx pattern returns a fresh clone for the same reason).
House style: one warm accent (`#C8743C`, with two darker shades of it for depth) and a neutral
dark grey (`#3C3C3C`) on a transparent
ground, ~2 px padding, strokes ≥ ~1.4 px, shapes near the pixel grid — simple flat glyphs,
legible at palette size.

---

← Back to the [design index](README.md) · the [docs index](../README.md)
