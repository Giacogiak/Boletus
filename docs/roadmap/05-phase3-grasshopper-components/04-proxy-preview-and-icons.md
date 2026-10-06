# 05 — Phase 3b: The capped proxy preview and the component icons

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`; every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4).

## Implemented 3b.3 (partial) — capped proxy preview (2026-06-20)

The second 3b.3 item is done: a dedicated **`Proxy preview`** terminal that draws a coarse,
depth-capped LOD of a Volume directly in the viewport — **viewport-only, no Mesh output** — so it can
never OOM Rhino.

- **Cap mechanism = a hard depth ceiling (decision — 2026-06-20).** `ProxyPreviewComponent` clamps the
  requested `Depth` to `MaxProxyDepth = 7` (`Math.Min(Math.Max(1, requested), 7)`) and posts a Remark
  when it clamps. A depth ceiling is the **only** mechanism that *prevents* OOM rather than detecting
  it too late: `DualcField.Contour` is **atomic** (no mid-run abort, no pre-count), so a pure
  vertex/triangle ceiling could only be checked *after* building the mesh that would already have blown
  up. At a capped depth, leaf-cell count is bounded ~`8^depth`, so memory is bounded for any field. An
  **adaptive triangle-budget** (contour coarse-first, step depth up while under budget, never past the
  ceiling) is noted as a later refinement; the **ROI sub-box** is deferred (orthogonal, and lattice
  fidelity is the raymarcher's job — [07 §5](../07-upstream-coordination/02-viewer-and-uniform-push.md#5-dualc_field_view-viewer-binary--gates-the-raymarch-preview-phase)).
- **Viewport-only, no output (decision — 2026-06-20).** The component has **no** Mesh output; it draws
  via `DrawViewportMeshes`. That is its whole reason to exist distinct from `Contour/Export`, which
  already hands back a bakeable proxy mesh in preview mode (exposing a Mesh output here too would
  double-draw against GH's default preview). Clean split: **`Contour/Export` = bakeable/export
  terminal; `Proxy preview` = capped, disposable viewport aid.**
- **Contour once, cache, then only draw.** `DrawViewportMeshes` fires on every pan/zoom/rotate, so the
  component contours **once in `SolveInstance`** and caches the `Rhino.Geometry.Mesh` + its
  `BoundingBox`; the draw override only draws the cached mesh, and `ClippingBox` returns the cached
  bbox so Zoom-Extents frames it. A failed/empty solve clears the cache (no stale draw). It carries
  over `Contour/Export`'s unbounded-field handling (Min/Max + the friendly `DualcStatus.Bounds`
  message) and the `VolumeResolver` + `FromJson(json, meshes)` in-RAM mesh path.
- **Reuse, not copy.** `ToRhinoMesh` moved out of `ContourExportComponent` into a shared
  `RhinoMeshConvert` helper; both terminals call it.

### Files changed (3b.3 capped proxy preview)

| File | Change |
|---|---|
| `src/Boletus.Grasshopper/ProxyPreviewComponent.cs` | **new** — the component (depth clamp + contour-once-and-cache + `DrawViewportMeshes`/`ClippingBox`, no output). |
| `src/Boletus.Grasshopper/RhinoMeshConvert.cs` | **new** — shared `internal static ToRhinoMesh(DualcMeshData)`. |
| `src/Boletus.Grasshopper/ContourExportComponent.cs` | calls `RhinoMeshConvert.ToRhinoMesh`; private copy removed. |
| `docs/roadmap/05`, `roadmap.md`, `README.md`, `STRUCTURE.md`, `CLAUDE.md` | recorded the increment. |

### Verification (3b.3 proxy) — what is and isn't proven

- **Automated:** `dotnet build Boletus.sln -c Debug` → **0/0**; output payload unchanged (`.gha` +
  `Boletus.Core.dll` + `dualc_capi.dll`). Suite still **83/83**. No `Boletus.Core` source changed: the
  proxy reuses the same `Contour`/marshaling path as `Contour/Export`, already golden-gated; the new
  code (a one-line depth clamp + viewport drawing) is GH-only and so compile-checked here only.
- **Compile-checked only (pending a manual Rhino smoke test — GH can't run headless):** the viewport
  behavior. Smoke test: `TPMS → Normalize → Onion(thickness, boundary = Primitive Box) → Proxy
  preview` draws a coarse shaded lattice with **no** bakeable Mesh output; pan/zoom stays smooth (no
  re-contour — proves the cache); dialing `Depth` above 7 **clamps** (Remark) and never OOMs; a bare
  `TPMS → Proxy preview` with no Min/Max raises the friendly unbounded message; Zoom-Extents frames the
  proxy (proves `ClippingBox`).

## Implemented 3b.3 (partial) — component icons (2026-06-20)

The third 3b.3 item is done: every component, the `Volume` wire param, and the plugin's assembly
entry now show a glyph on the palette instead of Grasshopper's default box.

- **Procedurally drawn (decision — 2026-06-20).** A new `BoletusIcons` helper draws each **24×24**
  glyph with `System.Drawing` GDI+ primitives — **no image files, no embedded resources, no
  generator, no build step**. The whole icon set is reviewable C# in the diff and reproducible from
  source; `System.Drawing` is already available (`<UseWindowsForms>` in the csproj). The alternative
  (a generator baking embedded PNGs) was rejected as machinery with no visual gain over procedural
  glyphs. The getters route through the helper, so swapping to designed/embedded PNGs later needs no
  component change. These are deliberately **simple flat geometric glyphs**, not illustrated art.
- **Fresh bitmap per access (not cached).** Every `BoletusIcons.*` accessor renders and returns a
  **new** `Bitmap`. GH caches the icon per component itself, so the getter is called rarely; handing
  one shared instance to all 12 owners would let GH dispose one owner's copy out from under the
  others (the resx pattern returns a fresh clone for the same reason). Twelve one-off 24×24 renders
  are free.
- **House style + legibility.** One warm Boletus accent (`#C8743C`) + neutral dark grey (`#3C3C3C`)
  on a transparent ground, ~2px padding, strokes ≥ ~1.4px, shapes kept near the pixel grid so the
  glyphs stay readable at palette size.
- **Glyph catalog.** Sources — TPMS = interleaved sine waves; Primitive = isometric cube;
  Mesh→Volume = triangulated patch → field blob. Decorators — Onion = concentric rings; Graded
  Onion = rings thickening outward; Normalize = equal radial ticks; Transform = dashed + warped box.
  Booleans — Boolean = overlapping circles with the intersection filled. Terminals — Contour/Export
  = iso-curve over a grid + export arrow; Proxy preview = coarse facet ball. Param — Volume = field
  blob badged "V". Assembly — a boletus mushroom (ribbon tab / plugin manager).

### Files changed (3b.3 component icons)

| File | Change |
|---|---|
| `src/Boletus.Grasshopper/BoletusIcons.cs` | **new** — `internal static` GDI+ glyph helper (12 glyphs, fresh-bitmap-per-call, house style). |
| 10 component files + `VolumeParameter.cs` + `BoletusInfo.cs` | `Icon => null` stub → `BoletusIcons.<glyph>`. |
| `docs/roadmap/05`, `roadmap.md`, `README.md`, `STRUCTURE.md`, `CLAUDE.md` | recorded the increment. |

### Verification (3b.3 icons) — what is and isn't proven

- **Automated:** `dotnet build Boletus.sln -c Debug` → **0/0**; `.gha` output payload unchanged
  (icons compile into the assembly — no new shipped files). Suite still **83/83** (no Core touched).
- **Render-checked (offline):** the 12 glyphs were rendered to a contact sheet by a throwaway
  net-windows program linking the real `BoletusIcons.cs` — all 12 draw with **no runtime exception**
  (exercises `FillPie`, `AdjustableArrowCap`, `Region.Intersect`, `DrawCurve`, font drawing) and are
  legible at size. (The throwaway project was discarded; it is not in the repo.)
- **Pending manual Rhino smoke:** confirm each glyph appears on the **Boletus** tab across
  Sources / Decorators / Booleans / Terminals, the `Volume` param shows its glyph as the I/O type,
  and the mushroom shows in the GH ribbon tab / plugin manager.

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
