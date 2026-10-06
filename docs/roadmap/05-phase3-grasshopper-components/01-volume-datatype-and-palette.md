# 05 — Phase 3b: The plan — the `Volume` datatype, the component model, the MVP palette, the Rhino glue

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`; every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4).
*(2026-09-21, roadmap 09 Phase 5, the first semantic lint: from § Component model on, the page is the plan as
drafted on 2026-06-18 with its DONE marks; the component model and the Rhino glue as they hold
are [design 04](../../design/04-grasshopper-plugin.md) and [design 06](../../design/06-conventions.md),
and the palette as it is driven is the [command reference](../../command_reference/README.md).)*

## Project

The `.gha` project as it is — `net7.0-windows`, the project reference to `Boletus.Core`, the
compile-only `Grasshopper` metapackage — is [design 04 § The project](../../design/04-grasshopper-plugin.md#the-project);
the build-environment decisions that fixed it are
[§ Resolved decisions](02-thin-slice-and-volume-palette.md#resolved-decisions-build-environment-pinned-2026-06-18).

## The Volume datatype (the plugin's exchange currency)

The `Volume` — a meshless field tree plus the raw triangles of input-mesh leaves, nothing
baked between components, the boundary clip an explicit `intersection` — is described where
it lives today: [design README § The `Volume` contract](../../design/README.md#the-volume-contract)
and [design 03](../../design/03-volume-and-resolvers.md) (the diskless `mem://` → `id=`
path). The ground truth it mirrors — DualC's volume *is* the immutable field expression tree,
sampled once — is DualC's
`D:\DualC\docs\roadmap\02-implicit-sdf-foundation.md`. The decision is D-13 in the
[decisions index](../../decisions/01-settled.md). *(2026-09-21, roadmap 09 Phase 4: the
present-tense description that stood under these two headings — the project shape, the
`Volume` shape, the two senses of "mesh", the diskless handoff — moved to the design pages;
the headings stay as anchors.)*

## Component model (confirmed decisions)

- **Consolidated multi-mode** components (one per family with a type/op dropdown) — **except**
  `Onion`, `Graded-onion`, and `Normalize`, which are **standalone** (they gate metric
  correctness and the shell workflow; the user requires them discrete).
- **MVP first** — the value pillars (TPMS lattice → clip/boolean → preview → export).
- **Minimal TPMS, clip inside the thickening.** `TPMS` is just type + wavelength (no point —
  a bare point is not an intuitive "limiting space"). The boundary is supplied **on the
  `Onion`/`Graded-onion` component** (optional input) and applied **after the thickness**:
  `intersection(onion(field), boundary)`. This keeps the correct *onion-before-clip* order
  visible on the canvas (no deferred/implicit effect) — shelling before the clip gives clean cut
  faces; clipping first would shell the cut faces. Non-onion clipping (e.g. a TPMS network solid)
  uses the generic `Boolean(intersection)`.

### MVP palette

**Sources (→ Volume)**
- `TPMS` — family dropdown (6) + `wavelength`. Outputs the infinite, non-metric lattice.
- `Primitive` — type dropdown; `box`/`sphere`/`roundbox`/`torus`/`plane` (the usual clip solids),
  grow later.
- `Mesh → Volume` — Rhino mesh → in-memory `MeshBuffer` + `mesh(id=…)`/`winding(id=…)` node;
  `Kind` selects mesh-parity / mesh-pseudonormal / winding (open/soup). No disk write here.
- `Strut Lattice` — crystal dropdown (`sc`/`bcc`/`fcc`/`octet`) + `Center`/`Wavelength`/`Radius`/
  optional `NodeRadius`. The solid-beam counterpart to `TPMS`; already **metric** (true SDF — no
  Normalize) and **infinite** (posts an infinite-extent remark). **DONE (strut lattices, 2026-07-10).**

**Decorators / warps (Volume-in → Volume-out)**
- **`Onion`** (standalone) — `thickness` + **optional boundary Volume** (clip after thickness).
- **`Graded-onion`** (standalone) — base + control Volumes + `t1`/`t2`/`d1`/`d0` + optional boundary.
- **`Normalize`** (standalone) — the metric-correctness gate; canonical chain TPMS → Normalize → Onion.
- `Transform` — a Rhino `Transform` → the `transform` op (matrix convention flagged to verify).
- `Offset` (standalone) — one scalar `Distance` → the `offset` op (level-set shift, world units).
- `Twist` / `Bend` (standalone) — scalar (`Rate`/`Curvature`) + axis dropdown → the `twist`/`bend` ops
  (axis int 0/1/2 → `"x"/"y"/"z"` via `AxisName`).
- `Displace` (standalone) — fn dropdown (0/1/2 → `"sine"/"gyroid"/"bumps"`) + optional `Amplitude`/`Frequency`
  → the `displace` op (omitted optionals defer to DualC's engine defaults).
- **`Graded Offset`** (standalone) — base + control Volumes + `t1`/`t2`/`d1`/optional `d0` → the
  `graded-offset` op. Grades a **solid** strut radius across space (use it, not `Onion`, on struts).
  **DONE (strut lattices, 2026-07-10).**
- **`Mix`** (standalone) — three Volumes (A, B, control) + `hi`/optional `lo` → the `mix` value-lerp
  morph. Watertight only for a same-family radius morph (posts a value-blend remark).
  **DONE (strut lattices, 2026-07-10).**

**Booleans (→ Volume)**
- `Boolean` — op dropdown (`union`/`intersection`/`difference`/`xor` + 3 smooth); `k` for smooth.
  Two Volume inputs. Also the path to clip a TPMS without thickening.

**Terminals (invocation)**
- `Contour / Export` — Volume + `depth` + optional `Min`/`Max` + `path` + `Tile depth`. Hands any
  input-mesh leaves to DualC **in RAM** (`mesh(id=…)` + `*_with_meshes`, v0.3.0; **DONE** —
  `VolumeResolver` + `FromJson(json,meshes)`), builds the `DualcField`. **Auto-bounds** when the field
  is finite; Min/Max only needed for an unbounded field. **Branches on whether a `Path` is connected**
  (skip-preview-when-exporting): **no path** → dual-contours once → returns a `Rhino.Geometry.Mesh`;
  **path** → export-only (skips the proxy contour for bounded RAM), no mesh output that solve.
  - **STL export is always tiled (decision — 2026-06-19; DONE — see [§ Implemented 3b.3](03-diskless-mesh-and-tiled-stl.md#implemented-3b3-partial--tiled-stl-export-2026-06-19)).**
    Every `.stl` path routes through `DualcField.ExportTiledStl` (streams tiles to disk, peak RAM ≈
    one tile) rather than `Export`, so a high-depth export can't OOM. The `Tile depth` input is
    optional (default `Depth − 2`); `Tile depth ≥ Depth` warns and falls back to a single streamed
    pass (DualC); `Collapse` is never set here so its incompatibility can't trip. `.3mf`/`.obj` have
    no tiled writer yet, so they still materialize the full mesh — prefer STL for heavy/deep exports.
  - **Prefer binary formats (decision — 2026-06-19).** For heavy meshes default to / recommend the
    **binary** formats (`.stl`, `.3mf`); ASCII `.obj` bloats on large meshes. Keep only a **basic**
    OBJ read/write (the existing `MeshBuffer.WriteObj` + the file-path mesh source) — nice to have,
    **not a priority** to extend.
- `Proxy preview` — a coarse drawable **viewport LOD only**, and **always under a hard safety cap**:
  the proxy contour depth is **clamped to a hard ceiling** (`MaxProxyDepth = 7`) so it can never OOM
  Rhino, no matter how high the user dials depth. A coarse proxy of a *lattice* is inherently lossy
  (thin walls drop out); **true TPMS/lattice visualization is delegated exclusively to the externalized
  raymarch side-car** ([07 §5](../07-upstream-coordination/02-viewer-and-uniform-push.md#5-dualc_field_view-viewer-binary--gates-the-raymarch-preview-phase) / roadmap Phase 5), never to a Rhino mesh.
  **DONE (3b.3, 2026-06-20)** — viewport-only (no Mesh output), contours once and caches; see
  § Implemented 3b.3 (capped proxy preview).

## Rhino glue (bake these in from the start)

- **Native-DLL resolver at plugin startup.** The OS loader searches Rhino.exe's directory,
  **not** the `.gha`'s — a native DLL next to the `.gha` is *not* reliably found. In the
  plugin's load hook (`GH_AssemblyPriority` / priority load, net7.0) install
  `NativeLibrary.SetDllImportResolver` (or `SetDllDirectory`) pointing at the assembly's
  own folder so `dualc_capi.dll` resolves. (`Boletus.Core` is netstandard2.0 and can't
  reference `NativeLibrary` at compile time — the resolver lives in the net7.0 GH host.)
- **One `DualcField` handle per solve.** Grasshopper may solve in parallel; never share a
  handle. Create → contour/export → dispose within a component's solve.
- **Meshless and diskless until Contour/Export.** A `Mesh → Volume` keeps the imported geometry in
  memory (a `MeshBuffer` of raw triangles — *not* contoured); the mesh leaf carries a `mem://<id>`
  placeholder. Only the Contour/Export terminal hands it to DualC, **in RAM (DONE, DualC v0.3.0):**
  `VolumeResolver` rewrites the `mem://<id>` placeholder to `mesh(id=…)`, then
  `DualcField.FromJson(json, meshes)` pins each `MeshBuffer` for the create call only (DualC copies
  eagerly — buffers need **not** outlive the field) and calls the `*_with_meshes` entry point —
  **no disk**. *(If a pre-0.3.0 DLL ever had to be supported, the old approach — write each buffer
  to a temp OBJ, swap the placeholder for that path, build, delete in `finally` — is recoverable
  from git history; the `VolumeMaterializer` that did this was removed once the in-memory path
  landed. [07 § 1](../07-upstream-coordination/01-in-memory-mesh.md#1-in-memory-mesh-source-resolver--done-dualc-v030-upstream--the-boletus-side-flip-both-2026-06-19).)*
- **Unit scaling.** `.3mf` is 1 unit = 1 mm; scale Rhino-doc units → mm on export.
  *(2026-09-21, roadmap 09 Phase 4: no Rhino-unit scaling was ever implemented — the canvas numbers
  are the millimetres in the file, [design 06 § Units](../../design/06-conventions.md#units).)*
- **Mesh construction.** Build `Rhino.Geometry.Mesh` from `DualcMeshData` (vertices from
  `Positions` triples, faces from `Indices` triples, normals optional) — an unavoidable
  Rhino-side copy.

## Verification

- The `.gha` **loads in Rhino 8** and components appear on the canvas.
- The canonical chain **`TPMS → Normalize → Onion(thickness, boundary = Primitive Box) →
  Contour/Export`** — **with no `Path`** draws a clean sheet lattice clipped to the box (proxy mesh);
  **with a `.stl` `Path`** writes a tiled STL and (by the *skip-preview-when-exporting* rule)
  produces **no** Rhino mesh that solve. (Preview and export are separate solves.)
- The serialized graph from the canvas matches `--dump-json` for the same composition
  (reuses the [serializer](../04-phase3-field-graph-serializer.md) tests).
- The Onion-before-clip ordering and the in-memory `MeshBuffer` path are gated at the Core
  level by golden-count tests (see § Implemented 3b.2).

## Open questions

- ~~Exact Rhino 8 SR / target framework~~ — **RESOLVED: `net7.0-windows`** (Rhino **8.32**).
- ~~Mesh exchange / "Volume" type~~ — **RESOLVED:** a single in-memory `Volume` (field-graph +
  `MeshBuffer`s); meshless until Contour/Export. See § The Volume datatype.
- MVP `Primitive` subset beyond the proposed five.
- ~~Proxy-mesh strategy~~ — **RESOLVED (2026-06-19):** the Rhino proxy is a coarse LOD **kept under a
  hard safety cap** (it can never OOM) and is **not** the lattice-visualization surface; **TPMS/
  lattice visualization is exclusively the raymarch side-car** ([07 §5](../07-upstream-coordination/02-viewer-and-uniform-push.md#5-dualc_field_view-viewer-binary--gates-the-raymarch-preview-phase)).
  Cap mechanism **RESOLVED (2026-06-20): a hard depth ceiling** (`MaxProxyDepth = 7`, clamped) — the
  only mechanism that prevents OOM rather than detecting it too late (`Contour` is atomic). Adaptive
  triangle-budget noted as a later refinement; ROI sub-box deferred. See § Implemented 3b.3 (capped
  proxy preview).

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
