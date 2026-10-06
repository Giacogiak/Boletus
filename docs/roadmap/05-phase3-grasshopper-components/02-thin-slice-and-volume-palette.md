# 05 — Phase 3b: Increments 3b.1 and 3b.2 — the thin vertical slice and the `Volume` palette

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`; every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4).

## Implemented 3b.1 — thin vertical slice (2026-06-18)

The first increment is a **thin vertical slice**, not the whole palette: scaffold the
project + native-DLL resolver + the `Field` data type, plus **one source (TPMS)** and the
**Contour/Export terminal**. That single path deliberately exercises *every* architectural
risk at once — building a `.gha`, loading it in Rhino 8, the native-DLL resolver, and the
serializer → native → `Rhino.Geometry.Mesh` marshaling — at minimum component count. The
full MVP palette becomes a clean second increment (see § Next increment below).

### Resolved decisions (build environment, pinned 2026-06-18)

- **Target framework = `net7.0-windows`.** Rhino **8.32**'s .NET Core host runs on the
  **.NET 7** runtime; net8/9 assemblies won't load into Rhino. (The doc's earlier
  net7-windows *assumption* is now confirmed correct, for this reason.)
- **Online NuGet restore is allowed** (constraint clarified by the user 2026-06-18: the
  only hard rule is that the **native code never leaves the machine / is never published**;
  fetching managed packages from nuget.org is fine). The first build restored the
  `Grasshopper` metapackage + the net7/WindowsDesktop reference packs online. This
  supersedes the older "NuGet is offline" framing in [00](../00-references-and-environment.md)
  and `CLAUDE.md`. *(2026-10-06: the "never leaves the machine" half is reversed by D-46 —
  the repository is public and CI builds the native side from the DualC submodule; the rule
  that holds is "no native binary is ever committed", [10](../10-public-delivery/README.md).)*
- **Rhino/GH referenced via the `Grasshopper` 8.0.x NuGet metapackage**, compile-only
  (`IncludeAssets=compile;build`, `ExcludeAssets=runtime`, `Private=false`) — the real
  RhinoCommon/Grasshopper assemblies load from the running Rhino process and are never
  copied into the `.gha` output. McNeel ships these packages with **net48-only** reference
  assemblies, so a net7 project consumes them via `AssetTargetFallback`, which emits
  **NU1701**. That is expected for Rhino 8 plugin builds (the net48 public surface is
  source-compatible with the net7 RhinoCommon Rhino loads at runtime); it is suppressed
  via `<NoWarn>NU1701</NoWarn>` with a comment. *This is the one thing not verifiable
  without launching Rhino — confirm on first load.*
- Installed toolchain facts (verified): .NET SDK 9.0.307; WindowsDesktop/NETCore **ref
  packs present for net6/net8/net9** (no net7 in the local cache initially — restored
  online); net48 reference assemblies present (on disk + cache).

### What was built — `src/Boletus.Grasshopper/` (net7.0-windows, x64, output `.gha`)

| File | Role |
|---|---|
| `Boletus.Grasshopper.csproj` | net7.0-windows, x64, `<TargetExt>.gha</TargetExt>`, `<UseWindowsForms>`, `<EnableDynamicLoading>`; `Grasshopper` 8.0.x metapackage (compile-only, `Private=false`); `<NoWarn>…;NU1701</NoWarn>`; project ref to `Boletus.Core`; same `<None>` copy of `native/x64/dualc_capi.dll` Core uses. |
| `BoletusInfo.cs` | `GH_AssemblyInfo` — plugin name/description/GUID/version/author (shown in the GH plugin manager). |
| `BoletusPriority.cs` | `GH_AssemblyPriority` — at `PriorityLoad()` installs `NativeLibrary.SetDllImportResolver(typeof(DualcField).Assembly, …)`. **Registered against the Core assembly**, because that is where the `[DllImport("dualc_capi.dll")]` declarations live (a resolver only fires for P/Invokes in the assembly it is attached to). The resolver loads `dualc_capi.dll` from the `.gha`'s own directory — needed because the OS loader searches Rhino.exe's dir, not the `.gha`'s. |
| `FieldGoo.cs` | `GH_Goo<FieldNode>` — the payload on Field wires. `Duplicate()` shares the (immutable) node; `ToString()` → `Field [<op>]`. |
| `FieldParameter.cs` | `GH_Param<FieldGoo>` — the Field wire param (not persistent; `Exposure = hidden`). |
| `TpmsComponent.cs` | Source. Inputs: Family (`Param_Integer` with 6 named values 0..5 → Gyroid/SchwarzP/Diamond/FischerKoch/Lidinoid/Neovius), Wavelength (optional), Center (optional point). Output: Field. `switch` on family → the matching `Field.*` builder. |
| `ContourExportComponent.cs` | Terminal. Inputs: Field, Depth (default **6** — deep lattice contours are exponential), Min/Max bounds (default ±1), optional Path. Outputs: Mesh, Info (`"{v} v / {t} t"`). Runs `FieldGraphValidator.Validate` (pushes warnings, aborts on error) → `node.ToJson()` → `DualcField.FromJson` → `Contour` with `HasBounds=true` → builds a `Rhino.Geometry.Mesh` (vertices from `Positions` triples, faces from `Indices` triples, normals from `Normals` or `ComputeNormals()`) → optional `Export(path)`. Field created **inside** `SolveInstance` and disposed in `finally` (single-threaded handle; GH may solve in parallel). |

`Boletus.sln` updated: `Boletus.Grasshopper` added under the `src` solution folder, all six
config rows mapped to **x64**.

### Verification

**Automatable — done:**
- `dotnet build Boletus.sln -c Debug` → **0 warnings, 0 errors**; emits
  `Boletus.Grasshopper.gha`.
- Output payload is exactly `Boletus.Grasshopper.gha` + `Boletus.Core.dll` +
  `dualc_capi.dll` — **no** RhinoCommon/Grasshopper runtime DLLs (proves the compile-only,
  non-copy-local reference worked).
- The existing **70/70 `Boletus.Core` tests still pass** (no regression).

**Manual — pending (Rhino can't be driven headlessly):** copy the `.gha` + `dualc_capi.dll`
into `%APPDATA%\Grasshopper\Libraries\` (unblock the `.gha` first); open Rhino 8 →
Grasshopper; the **Boletus** tab shows TPMS + Contour/Export; drop **TPMS (Gyroid) →
Contour/Export**; a **mesh renders** in the viewport and an **STL writes**. Success for
this slice = *mesh renders + STL writes* — **not** a golden-count match: this slice has no
Onion/Box/Intersection, so it cannot compose the `intersection(onion(gyroid),box)` golden
graph; the golden counts are already gated at the Core level by the 70 tests and don't need
re-proving in GH until the Onion/Boolean components exist.

(Note: in 3b.1 the wire type was named `Field` and `TpmsComponent` took a `center` point; 3b.2
renamed the type to `Volume` and dropped the point — see below.)

## Implemented 3b.2 — Volume datatype + boundary-driven palette (2026-06-18)

The rest of the MVP palette, restructured around the user's direction: a runtime **Volume**
datatype (meshless until Contour/Export), a minimal `TPMS`, and the boundary clip applied inside
the thickening components. All builds clean (0/0); the suite is **74/74** (70 + 4 new).

### What was built / changed

- **`Boletus.Core`:** added the Rhino-free **`MeshBuffer`** (`Vertices`/`Triangles`/`Normals` +
  `ContentHash()` + `WriteObj()`) and a forward-looking **`DualcField.FromJson(json, meshes)`** seam
  (a stub at 3b.2, not yet wired to a native call — implemented in the 2026-06-19 flip below).
- **GH datatype:** renamed `FieldGoo`/`FieldParameter` → **`VolumeGoo`/`VolumeParameter`**, wrapping
  the **`Volume`** type (`Core` `FieldNode` + in-memory `Meshes`). At 3b.2, `Volume` plus a temp-OBJ
  **`VolumeMaterializer`** (swap `mem://<id>` leaves → temp OBJs at the terminal, delete after) lived
  in the `.gha`; both were relocated/replaced in the 2026-06-19 flip below.
- **Components** (all `Boletus`/Sources·Decorators·Booleans·Terminals):
  - `TpmsComponent` (rewritten) — family + wavelength only → Volume (infinite field).
  - `PrimitiveComponent` — box/sphere/roundbox/torus/plane (the clip solids).
  - `MeshToVolumeComponent` — Rhino mesh → in-memory `MeshBuffer` + `mesh`/`winding` node.
  - `OnionComponent` — thickness + **optional boundary** → `intersection(onion(child), boundary)`.
  - `GradedOnionComponent` — base + control + `t1`/`t2`/`d1`/`d0` + optional boundary.
  - `NormalizeComponent`, `TransformComponent` (Rhino `Transform`; matrix convention flagged),
    `BooleanComponent` (7 ops + `k`).
  - `ContourExportComponent` (rewritten) — Volume in; materializes mesh leaves; **auto-bounds**
    (Min/Max only for unbounded fields); friendly `DUALC_ERR_BOUNDS` message.

### Verification (3b.2)

- `dotnet build Boletus.sln -c Debug` → **0/0**; output payload unchanged (`.gha` +
  `Boletus.Core.dll` + `dualc_capi.dll`).
- **74/74 Core tests** green. New `MeshBufferTests`: (a) a cube `MeshBuffer.WriteObj` drives the
  **mesh-path golden counts** (70,032 v / 120,612 t) — proving it is byte-equivalent to the
  `cube.obj` fixture; (b) `intersection(onion(gyroid,0.12), box)` — the exact graph the Onion
  component emits with a boundary — hits the **analytic golden counts** (101,476 v / 163,740 t),
  proving the onion-before-clip ordering; (c) `ContentHash` stable/distinct; (d) OBJ writer format.
- **Manual (Rhino):** the canonical chain (Verification above) — pending a user smoke test.

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
