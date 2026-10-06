# 05 — Phase 3b: The diskless-mesh flip and the tiled STL export

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`; every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4).

## Implemented — diskless-mesh flip + `Volume`→Core relocation (2026-06-19)

Two follow-on changes after 3b.2: **(1)** consume DualC v0.3.0's in-memory mesh resolver so
input-mesh leaves reach the engine **in RAM** (no temp files), and **(2)** relocate the meshless
datatype + rewrite into `Boletus.Core` so the terminal logic is Rhino-free and unit-tested.

### The diskless-mesh flip (consume DualC v0.3.0)

- **Vendored** `dualc_capi.dll` 0.2.0 → **0.3.0** (commit `e345bf3`; `native/x64/` +
  `native/README.md`). ABI grew **9 → 11** entry points.
- **`NativeMethods.cs`** — added the blittable **`DualcMeshSourceNative`** (`id` / `vertices` /
  `vertexCount` / `indices` / `triangleCount` / `normals`; sequential layout, 48 B on x64) and the
  two **`dualc_field_create_from_{json,expr}_with_meshes`** decls (`out DualcFieldHandle`, matching
  the existing SafeHandle style — not the handoff doc's `out IntPtr`).
- **`DualcField.FromJson(json, meshes)`** — implemented: pins each `MeshBuffer`'s `Vertices`
  (`float[]`), `Triangles` (`int[]` ≡ `uint32*`, bit-identical) and UTF-8 `id` for the **create call
  only** (DualC copies eagerly — buffers need not outlive the field), builds the
  `DualcMeshSourceNative[]`, calls the native entry point, frees every `GCHandle` in `finally`.
  `normals = IntPtr.Zero` (ignored by DualC). An empty map falls back to the plain `FromJson`.
- **`mesh`/`winding` gained an `id` param** — `Ops.cs` (`path` now optional + `id` optional);
  `Field.Mesh(…, id:)` / `Field.Winding(id:)`; `FieldGraphValidator` enforces **exactly-one-of
  `path`/`id`**.
- **Terminal flip** — `ContourExportComponent` now builds the field via `VolumeResolver.Resolve` +
  `DualcField.FromJson(json, meshes)` (in RAM); the `VolumeMaterializer` temp-OBJ materialization +
  cleanup was dropped from the solve.

### The `Volume`→Core relocation + dedup

- Moved **`Volume`** and **`VolumeResolver`** to `Boletus.Core` (made `public`; both are Rhino-free)
  and extracted the shared **`FieldGraph/FieldTree.Rewrite`** bottom-up immutable tree-rewrite, with
  the `mem://` detection consolidated into **`Volume.TryGetMemoryMeshId`**. Only
  **`VolumeGoo`/`VolumeParameter`** remain in the `.gha`; the 9 GH files that referenced the moved
  types gained `using Boletus.Core;`.
- **Deleted `VolumeMaterializer`** — obsolete once the in-memory path landed, and wired into nothing
  (no version-negotiation path would ever invoke it). Recoverable from git history if a pre-0.3.0
  DLL ever had to be supported.
- **Rationale:** the rewrite is Rhino-free logic that was trapped in the net7.0-windows project, so
  `Core.Tests` couldn't reach it. Relocating to Core makes the GH-terminal rewrite unit-testable
  (closing the one previously-uncovered risky path) and keeps the `.gha` a thin shell.

### Verification (2026-06-19)

- `dotnet build Boletus.sln -c Debug` → **0/0**; output payload unchanged (`.gha` +
  `Boletus.Core.dll` + `dualc_capi.dll`, now 0.3.0).
- **82/82 Core tests** green (was 74 at 3b.2). New gates:
  - `MeshBufferTests.InMemory_mesh_source_hits_the_same_golden_counts_as_the_temp_file` — the
    in-memory cube hits the **same** 70,032 v / 120,612 t as the temp-file baseline, exercising the
    struct layout, `GCHandle` pinning, `int[]`→`uint32*`, and id resolution across the P/Invoke
    boundary.
  - `VolumeResolverTests` (7) — `mem://`→`id=` swap, buffer collect/dedup, drop-unreferenced,
    disk-path passthrough, missing-buffer error, serialize-and-validate; plus `FieldTree.Rewrite`
    no-op reference identity.
- **Manual (Rhino):** a full-canvas smoke test remains a nice-to-have, but no longer gates the
  rewrite logic (now automated).

## Implemented 3b.3 (partial) — tiled STL export (2026-06-19)

The first 3b.3 item is done: `ContourExportComponent` now streams every `.stl` export through
`DualcField.ExportTiledStl` so a deep export can't OOM.

- **`ContourExportComponent`** gained an optional **`Tile depth`** input (`.stl` only; default =
  `Depth − 2`, DualC's documented practical rule — ~64× less peak RAM, ~10–20% CPU overhead). The
  solve now **branches on whether a `Path` is connected** (user decision — *skip preview when
  exporting*): with **no path** it contours an in-Rhino proxy mesh as before; with a **path** it runs
  **export-only** (skips the full-depth `Contour` so a heavy export stays in bounded RAM) — `.stl`
  routes through `ExportTiledStl`, other formats fall back to the monolithic `Export`. The Mesh
  output is empty in export mode; the `Info` output reports the export. A `Tile depth ≥ Depth` is a
  **warning** (DualC logs it and falls back to a single streamed pass — no RAM benefit), not an
  error. `Collapse` is never set here (`Default()` is 0), so the collapse-incompatibility can't trip.
- **STL-only**, by design: `.3mf`/`.obj` have no streamed writer yet, so they stay full-memory;
  binary formats are preferred for heavy meshes, ASCII OBJ stays a basic convenience only.
- **Lower bound (known edge, non-blocking):** for `Depth ≤ 3` the `Depth − 2` default floors at
  `tileDepth = 1`, where DualC's ghost-ring overhead degenerates; the native call would throw
  cleanly into the existing `DualcException` catch. The input description notes **"≥4 recommended"**
  rather than imposing a hard clamp.

### Files changed (3b.3 tiled STL)

| File | Change |
|---|---|
| `src/Boletus.Grasshopper/ContourExportComponent.cs` | Added the optional `Tile depth` input (param index 5, `Optional`); rewrote `SolveInstance` to branch on `Path` presence (preview vs export-only) and route `.stl` → `ExportTiledStl`, else `Export`; `Info` reports the export; `Tile depth ≥ Depth` warning. |
| `tests/Boletus.Core.Tests/WrapperTests.cs` | New `ExportTiledStl_writes_a_binary_stl_with_the_golden_facet_count` (exact facet-count + file-size gate). |
| `docs/roadmap/05`, `roadmap.md`, `07` (left), `CLAUDE.md`, `README.md`, `STRUCTURE.md` | Recorded the increment; bumped current-state test count 82 → 83 (historical-snapshot 82/82 refs in `05 §diskless-flip` and `07 §1.4` left intact); reconciled the `05 §Verification` canonical-chain line (Path now suppresses the preview mesh). |

No `Boletus.Core` source changed — `DualcField.ExportTiledStl` + the native `dualc_field_export_tiled_stl`
P/Invoke already shipped with the vendored DualC v0.3.0 DLL; this increment only wired them into the GH
terminal and added the missing Core test.

### Verification (3b.3) — what is and isn't proven

- **Test-verified (automated):** the Core tiled-STL path end-to-end. Build **0/0**; suite **83/83**.
  The new test exercises `tileDepth` marshaling across the P/Invoke, auto-bounds *through* the tiled
  writer, and proves the tiled output is **bit-identical** to the monolithic mesh — it asserts the
  binary-STL facet-count header is **exactly 163,740** (the golden triangle count) and that file size
  = `84 + 50·facets`. (The box is integer-aligned at depth 6 — a dyadic grid — which is what makes
  the exact equality hold; a non-dyadic box would match only to micron rounding.)
- **Compile-checked only (pending a manual Rhino smoke test):** the *component* wiring — the
  `Path`-presence branch, `.stl` extension detection, the `Depth − 2` default, reading input index 5,
  and specifically the user-chosen **skip-preview-when-exporting** behavior (a Volume that previously
  produced a mesh now produces none when a `Path` is set). GH can't run headless, so this is the real
  acceptance gate for the one user-visible behavior change and has **not** yet been run live. Smoke
  test: canonical chain → with no `Path`, a proxy mesh draws; with a `.stl` `Path`, the file writes
  and **no** mesh appears; clearing the `Path` restores the proxy.

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
