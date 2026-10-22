# Porous Shell in Boletus: re-pin DualC past the `porous` node, one Sources component, its tests and docs

## Context

DualC grew a field-graph source, `porous`, on its branch `feat/porous-shell` (DualC roadmap
block 23, items DualC #63–#68): seeds placed in any volume, one Delaunay tetrahedralization of
them, and the **unsigned distance to the cell complex** — the Voronoi walls or the Delaunay
faces (`mode=walls`, sealed cells), or the edges (`mode=struts`, open cells). The value is 0
on the complex, so the bare node contours empty; it is thickened by the existing `onion` /
`graded-onion` / `offset` / `graded-offset` and clipped by the volume — a natural sponge, a
foam, a bone. DualC's owner decided (DualC D-72, 2026-10-19) that **Boletus gets this plan
only**: nothing is implemented here until the owner picks it up.

This plan was drafted unattended on 2026-10-22 by DualC's plan-run, unit P07.1, from DualC's
own docs as P06 wrote them. Its sources — read them at the new pin, not at the old one (none of
them exists at `350cb3a`):

- DualC `docs/command_reference/11-dualc_field/16-porous-shell.md` — every key with the
  parser's default, what the value is, the ceilings, the four recipes, the advice, the cost
  guide, recipes V1–V9. **The one home of every engine fact below**; the Boletus docs link it.
- DualC `capi/README.md` § *The porous source from a host mesh* — the C call with an in-memory
  mesh, the JSON form, the create-time build, the errors, determinism.
- DualC `docs/design/12-field-graph.md` § *The porous source* — the invariants (build-time
  volume, 1-Lipschitz, the bin bound, determinism, no cancel).
- DualC `docs/command_reference/12-dualc_field_view/02-nodes-and-examples.md` — the preview
  (one live uniform, `smooth`; every other key rebuilds the cells on reload).
- The parser itself: `readPorousParams` in DualC `examples/field_graph.cpp`, and
  `dualc_field --list`.

Verified on the drafting day against Boletus `main` at `2423275` (pin `350cb3a`, ABI 0.5.1) and
DualC `feat/porous-shell` at `4bed5e3` (ABI 0.5.2). Line numbers move; re-read before trusting
one.

## What the drafting session measured or read

- **The ABI does not change for `porous`.** No entry point, struct or code is new; the node is
  graph vocabulary, so `DualcField.FromJson(json, meshes)` already takes it. DualC keeps the
  version at `dualc 0.5.2` for it (DualC 14/04, "vocabulary widening, no ABI version move").
  Between the current pin `350cb3a` and `4bed5e3` the header changed in two comment lines
  only: `numThreads` now bounds the contourer as well as the sampler (0.5.2, DualC #34; the
  mesh is byte-identical at every value), and the version string. Nothing in
  `NativeMethods.cs` moves.
- **No DualC citation breaks at the new tree.** `DUALC_ROOT=~/Documents/DualC-porous python3
  scripts/check.py --docs`: `dualc-links` 136 files scanned, 118 DualC paths cited, **0
  missing**; 27 checks, 26 passed + 1 report-only. The seven CLIs DualC retired on 2026-10-16
  (DualC #60) are not built by `build_native.py` (`dualc_capi`, `dualc_field`, `dualc_c_demo`,
  `dualc_field_view` all remain); the five code comments that cite `02-dualc_primitive.md`
  (`Ops.cs`, `OpSchema.cs`, `Field.cs`) point at a retired-tool page that still exists — retarget
  them to DualC `11-dualc_field/12-primitive-catalogue.md` while re-pinning.
- **The validator cannot express the node's arity.** `porous` takes **one or two** children
  (`volume[, control]`); `OpSchema.ChildCount` is exact and `FieldGraphValidator` Tier 1a
  compares with `!=`. The schema needs a range.
- **The serializer already fits.** Keys sorted ordinally, text values quoted, numbers in the
  shortest round-trip form with a forced `.0` — what DualC's `--dump-json` emits for the node
  (checked: `porous(sphere(radius=1),spacing=0.5,seed=9007199254740992,relax=2,…)` dumps
  `"relax": 2.0`, `"seed": 9.007199254740992e+15`). One byte difference to know: .NET writes
  `E+15` where DualC writes `e+15`, for a scalar ≥ 1e15. Both parse; the vocabulary tests
  compare DualC's dump of both sides, so they pass, but a hand-written golden string must not
  use a seed that large (or `FormatNumber` lowercases the exponent — one line, optional).
- **The build runs inside the create call and takes no token** (DualC design/12 § No cancel).
  In Boletus that means: `Write to File` creates the field on its long-running task, so the
  canvas stays live, but `Cancel ■` only takes effect once the cells are built; `Proxy preview`
  creates on the solve thread, so the canvas waits for the cells. At the recipes' spacings
  the build is under 13 ms; the seeds cost about 1/55,000 s each in an analytic volume and
  1/5,500 s in a mesh (DualC 11/16 § Cost) — 10,000 seeds in a Rhino mesh is about 2 s.
- **`relax` uses every hardware thread** whatever `DualcContourParams.NumThreads` says (the
  graph never sets the seed builder's thread count; the cells are identical at any count).
  DualC tracks the create-time thread count as DualC #62 (DEFERRED until a host asks).
- **A mesh volume referenced twice is one buffer.** `Volume.Combine` merges by content hash,
  so the sponge's `intersection(mesh(id), …porous(mesh(id), …)…)` hands DualC one
  `DualcMeshSource` with the `id` used twice — exactly what DualC's `cli_c_abi_porous` tests.
- **Live Preview materializes a mesh leaf to an OBJ** (`MeshMaterializingResolver`, vertices
  written with `float.ToString("R")`), and the viewer builds the cells from that file. Whether
  the OBJ path and the in-memory path give the same cells for a mesh with non-dyadic
  coordinates is **not measured** — the seeds are placed by the volume's value, so a vertex
  that parses to a different double could move a seed. Step 5 tests it.

## Decided by this plan (defaults, pending the owner's review)

Each is a choice this unattended session made where DualC's raw plan left it open; the owner
confirms or changes it before Step 1.

1. **One component, `Porous Shell`, in `Boletus › Sources`** — not a decorator: like `TPMS`
   it is a source whose output still needs a thickness. The thickness and the clip are the
   existing `Onion`, `Graded Onion`, `Offset`, `Graded Offset` and `Boolean` components,
   exactly DualC's composition (DualC D-69). No built-in thickness input.
2. **All twelve keys are inputs**, the raw plan's list plus `Jitter`, which only
   `Distribution = Jitter` reads (omitting it would leave a DualC key unreachable from the
   canvas).
3. **A key is emitted only when it differs from DualC's default** — an empty optional input
   and a dropdown at its default index emit nothing, so the canvas's JSON equals DualC's own
   samples (`porous-sponge.json` writes only `spacing`).
4. **The ranges stay DualC's.** The component checks what it must to build a well-formed node
   (spacing present, a control when `Spacing2` differs, a dropdown index in range); `relax`
   ≤ 50, the 2,000,000-seed ceiling, `smooth` < `B` for struts are DualC's located
   `DUALC_ERR_GRAPH`, surfaced verbatim by the terminals as every other graph error is.
5. **`Seed` is a Grasshopper Integer** (0 … 2³¹−1), a subset of DualC's 0 … 2⁵³; enough for a
   canvas, and it keeps the golden-string trap above out of reach.
6. **No uniform push for `smooth`.** The viewer has `smooth` as a live uniform, but Boletus's
   update channel is the file-watch reload (D-27), which rebuilds the cells; no new row.

## Order of work

Every step leaves the gate green (`python scripts/check.py`), on a branch, by PR.

1. Re-pin the submodule to the DualC commit that carries `porous`.
2. Core: the schema's arity range, the `porous` row in `Ops.cs`, the validator rules, the
   `Field.Porous` builder.
3. Core tests: vocabulary round-trips, validator cases, the sponge example graph, CLI parity.
4. The `Porous Shell` component, its icon and GUID.
5. The previews: Proxy Preview and Write to File as they are, Live Preview's mesh path tested.
6. Docs: the command reference split and page, the design rows, the roadmap block, the
   decisions, `STRUCTURE.md`.
7. The Rhino smoke test on Windows (the owner, on the CI `.yak`).

---

## Step 1 — the pin

- **The commit:** the merge of DualC `feat/porous-shell` into DualC `main` on GitHub. It does
  not exist yet: the branch is local (DualC's runs never push), it ends with DualC's P08 (the
  full gate, the GPU tier, the semantic lint) and is then the owner's PR. The submodule must
  pin a commit reachable on GitHub, so **do not pin a branch commit**; until the merge,
  develop against the local checkout: `python scripts/build_native.py --dualc
  ~/Documents/DualC-porous` and `DUALC_ROOT=~/Documents/DualC-porous` for the tests and the
  gate.
- **The move:** `git -C external/DualC fetch origin main && git -C external/DualC checkout
  <merge>`, `git add external/DualC`, rebuild (`python scripts/build_native.py`, which prints
  the provenance block and runs `dualc_c_demo … cancel`), full gate. Also run DualC's own
  `dualc_c_demo <tmp>.stl porous` from `build-native/capi/` once: it is the C-level proof that
  the DLL at the pin carries the node (in-memory cube, JSON == shorthand, watertight, cancel).
- **What moves with it:** ABI `0.5.1` → `0.5.2` (the `numThreads` comment; no signature);
  `native/README.md` says "the pin is the gitlink" and states no hash, so it does not change;
  the roadmap index's milestone row and 07's inventory of what the pin offers
  (`07-upstream-coordination/03-export-callback-and-strut-sync.md` § 9) get a dated append
  naming `porous` and 0.5.2.
- **The five `02-dualc_primitive.md` comments** retargeted to DualC
  `11-dualc_field/12-primitive-catalogue.md` (Step 6 runs `dualc-links` over them).

## Step 2 — Core

**`OpSchema.cs`** — an arity range:

- `MinChildren` / `MaxChildren` replace the exact `ChildCount` (keep `ChildCount` as an
  obsolete alias of `MaxChildren` only if a caller outside the validator reads it — today none
  does: `grep -rn ChildCount src tests`). The existing constructor sets both to its
  `childCount`; a second constructor takes `(token, minChildren, maxChildren, category,
  params)`.
- `ParamSpec` gains an optional `Choices` (`IReadOnlyList<string>?`) for a `Text` param whose
  value is one of a closed set.
- `OpCategory.Porous`: a new category, documented as "an unsigned 1-Lipschitz distance
  source; metric; empty until thickened".

**`Ops.cs`** — one row, keys and defaults exactly as `readPorousParams` reads them (the
defaults are DualC's and are written here only so the component can tell "at default" from
"set"; the docs link DualC 11/16 § Parameters):

| Key | `ParamSpec` | DualC default | Choices / range (DualC enforces) |
| --- | --- | --- | --- |
| `spacing` | `ScalarReq` | required | finite, > 0 |
| `cells` | `TextOpt`, choices | `voronoi` | `voronoi`, `delaunay` |
| `mode` | `TextOpt`, choices | `walls` | `walls`, `struts` |
| `dist` | `TextOpt`, choices | `poisson` | `poisson`, `jitter`, `random` |
| `spacing2` | `ScalarOpt` | = `spacing` | finite, > 0; ≠ `spacing` needs the control and `d1` |
| `d0` | `ScalarOpt` | `0` | — |
| `d1` | `ScalarOpt` | `0`; required when graded | — |
| `ramp` | `TextOpt`, choices | `linear` | `linear`, `smooth`, `gaussian` |
| `relax` | `ScalarOpt` | `0` | whole, 0–50 |
| `seed` | `ScalarOpt` | `0` | whole, 0–2⁵³ |
| `smooth` | `ScalarOpt` | `0` | ≥ 0; < `B` for `struts` |
| `jitter` | `ScalarOpt` | `1` | `dist=jitter` only; `0` is a regular grid |

```csharp
// ---- Porous source (children: volume[, control]) --------------------------------
// Unsigned distance to the cell complex of seeds placed in the volume (DualC
// 11-dualc_field/16-porous-shell.md). The volume is read only while DualC builds the
// node; the control, when present, grades the spacing from `spacing` to `spacing2`.
Add(new OpSchema("porous", 1, 2, OpCategory.Porous,
    ScalarReq("spacing"),
    TextChoice("cells", "voronoi", "delaunay"), TextChoice("mode", "walls", "struts"),
    TextChoice("dist", "poisson", "jitter", "random"),
    ScalarOpt("spacing2"), ScalarOpt("d0"), ScalarOpt("d1"),
    TextChoice("ramp", "linear", "smooth", "gaussian"),
    ScalarOpt("relax"), ScalarOpt("seed"), ScalarOpt("smooth"), ScalarOpt("jitter")));
```

**`FieldGraphValidator.cs`:**

- Tier 1a: `Children.Count` in `[MinChildren, MaxChildren]`; the message becomes "takes 1–2
  child(ren)" for a range, unchanged for an exact count (existing tests pin the old wording —
  keep it for exact counts).
- Tier 1c: a `Text` value outside `Choices` is an Error naming the choices.
- Tier 1d (beside the mesh/winding rule): `porous` with `spacing2` present and ≠ `spacing` and
  no second child → Error "'porous' spacing2 differs from spacing: give a control field as the
  second child" (DualC's own wording); graded and no `d1` → Error; a second child but not graded
  → Warning "the control is ignored unless Spacing2 differs from Spacing".
- Tier 2: the volume (child 0) non-metric (a raw TPMS, a `winding`) → Warning: the seeds are
  placed below a pad measured in distance units, so a non-metric volume misplaces the boundary
  pores — `normalize` it.
- `IsMetric`: `OpCategory.Porous` → `true` (1-Lipschitz, DualC design/12), so `onion`,
  `offset`, the graded ops and the smooth booleans take it without a warning.
- A warning when a `porous` node's parent is not `onion`, `graded-onion`, `offset`, `round` or
  `graded-offset` (or it is the root): "the porous field has no thickness — its contour is
  empty; wrap it in Onion / Offset (or their graded forms)". The terminals already warn on an
  empty contour (D-37's diagnostics); this one names the cause before the contour runs.

**`Field.cs`** — one builder, every optional key nullable and emitted only when supplied:

```csharp
public static FieldNode Porous(FieldNode volume, double spacing, FieldNode? control = null,
    string? cells = null, string? mode = null, string? dist = null, double? spacing2 = null,
    double? d0 = null, double? d1 = null, string? ramp = null, int? relax = null,
    long? seed = null, double? smooth = null, double? jitter = null)
```

The component (Step 4) passes `null` for every input at DualC's default (decision 3).

## Step 3 — Core tests

All CLI-backed tests skip when `dualc_field` is absent, like their neighbours; each new
contour stays at depth ≤ 6 (DualC measured the porous library test at 0.45 GB peak; depth 7 is
1.6–3.2 GB and does not belong in `dotnet test`).

- **`FieldGraphVocabularyTests`** — `porous` round-trips through `--dump-json`, ours vs the
  shorthand, byte for byte: (a) the minimal `porous(sphere(radius=1),spacing=0.4)`; (b) every
  key set to a non-default (`cells=delaunay, mode=struts, dist=jitter, jitter=0.5,
  spacing2=0.6, d0=0.1, d1=1, ramp=gaussian, relax=2, seed=7, smooth=0.01`) with a control
  `sphere(radius=0)`; (c) the sponge sample's graph (below).
- **`FieldGraphValidatorTests`** — the arity range (0 and 3 children fail, 1 and 2 pass), an
  unknown choice, graded without a control, graded without `d1`, a control without grading
  (warning), a non-metric volume (warning), a bare `porous` root (warning), `onion(porous(…))`
  clean.
- **`FieldGraphExampleTests`** — the sponge as the canvas composes it: `Porous Shell(Volume =
  Sphere r 1, Spacing 0.4)` → `Graded Onion(Base = it, Control = Sphere r 0, t1 0.04, t2 0.08,
  d1 1)` → `Boolean(smooth intersection, k 0.1)` with the sphere. Assert the JSON equals the
  `--dump-json` of DualC's `porous-sponge.json` (shipped in `external/DualC/examples/samples/`,
  found the way `FieldGraphTests` finds the samples), then contour it at depth 6 through the
  wrapper and assert non-empty and `DualcDiagnostics` with no issue.
- **`CliParityTests`** — a second fact: DualC's own watertight test graph,
  `smooth-intersection(sphere(radius=1),onion(porous(sphere(radius=1),spacing=0.4,seed=3),thickness=0.06),k=0.1)`,
  exported at depth 6 through the wrapper and through `dualc_field`, byte-identical (DualC's
  library test: 40,086 vertices / 80,116 triangles at depth 6, ~1 s, 0.45 GB).
- **An in-memory mesh case** — `porous(mesh(id))` over `tests/Boletus.Core.Tests/cube.obj`
  loaded into a `MeshBuffer`, used twice (volume and clip), contoured twice at depth 6:
  the two meshes identical (determinism), non-empty, no diagnostics issue.
- **Cancel** — `ProgressAndCancelTests`: a token requested during the export of a porous
  field throws `OperationCanceledException` and leaves no file and no `.part` (DualC's
  `cli_c_abi_porous` proves the engine side; this proves the binding).
- `check_data.json` `tests_expected_min`: raise the floor to the new count.

## Step 4 — the `Porous Shell` component

`src/Boletus.Grasshopper/PorousShellComponent.cs`, `"Porous Shell", "Porous"`, category
`"Boletus"`, subcategory `"Sources"`, GUID `4F1B2A30-0000-4000-8000-000000000110` (the next
free in the `…0000000NN0` sequence: the largest today is `…100`), icon
`BoletusIcons.PorousShell` (a procedural glyph, D-20's style: three or four Voronoi cells).

| # | Input | Nick | Type | Default | Notes |
| --- | --- | --- | --- | --- | --- |
| 0 | Volume | V | `Volume` | — | required; read only while DualC builds the cells |
| 1 | Cells | Cl | Integer, named values | 0 Voronoi | 1 Delaunay |
| 2 | Mode | M | Integer, named values | 0 Walls | 1 Struts |
| 3 | Distribution | D | Integer, named values | 0 Poisson | 1 Jitter, 2 Random |
| 4 | Spacing | S | Number | — | required, > 0 |
| 5 | Spacing2 | S2 | Number | empty (= Spacing) | optional; ≠ Spacing needs Control and d1 |
| 6 | Control | C | `Volume` | — | optional; grades the spacing |
| 7 | d0 | d0 | Number | empty (0) | optional |
| 8 | d1 | d1 | Number | empty | optional; required when graded |
| 9 | Ramp | R | Integer, named values | 0 Linear | 1 Smooth, 2 Gaussian |
| 10 | Relax | Rx | Integer | empty (0) | optional; Lloyd iterations |
| 11 | Seed | Sd | Integer | empty (0) | optional |
| 12 | Smooth | Sm | Number | empty (0) | optional; rounds the cells' corners |
| 13 | Jitter | J | Number | empty (1) | optional; Distribution = Jitter only |

Output: `Volume` (V), "The porous field: zero on the cell walls or struts. Give it a thickness
(Onion, Graded Onion, Offset, Graded Offset), then clip it with the volume."

`SolveInstance`: read the inputs; Error and return on a missing Volume or Spacing, a dropdown
index out of range, or `Spacing2` ≠ `Spacing` without a Control or `d1` (the validator's
wording); Warning when Control is connected but `Spacing2` is empty or equal; build
`Field.Porous(…)` with `null` for every input at DualC's default; output
`Volume.Combine(node, volume, control)`. The input order follows the raw plan's list
(Volume, Cells, Mode, Distribution, Spacing, Spacing2, Control, d0/d1, Ramp, Relax, Seed,
Smooth) with `Jitter` appended last, so the plan's order and the canvas's agree.

## Step 5 — the previews

- **Proxy preview** — unchanged code. The create (and so the cell build) runs on the solve
  thread: at the recipes' spacings it is invisible (< 13 ms build, a few hundred seeds); at
  10,000+ seeds in a Rhino mesh the canvas waits seconds. The command-reference page says so
  with DualC's cost numbers linked, not copied. A cancellable build is D-50.
- **Write to File** — unchanged code. The create runs on the write task; `Cancel ■` takes
  effect at the first checkpoint after the cells are built. The page says so.
- **Live preview** — unchanged code. Every input change rewrites the graph file, the viewer's
  file-watch reloads it and rebuilds the cells (`smooth` is the viewer's live uniform, but only
  from its own keyboard; D-27 covers a push from the canvas). The viewer previews the
  export's own cells (DualC uploads the built field's arrays as data textures) or, past the
  GPU's texture limits, a baked grid it reports on stderr.
- **The mesh path, tested:** a Core test materializes a `MeshBuffer` with non-dyadic
  coordinates (a coarse sphere mesh built in the test, or `bracket.obj` from DualC's demo
  data if present) through `MeshMaterializingResolver`, builds `porous(mesh(path=<obj>))` and
  `porous(mesh(id))` over the same buffer, contours both at depth 5–6 and compares. **Equal:**
  the preview of a mesh volume shows the export's cells; record it in design 03. **Different:**
  the preview of a porous mesh volume is a different (equally valid) sample of the same
  foam; record that in design 03 and the command-reference page, and open D-52.

## Step 6 — docs (same session as the code, the gate green at the end)

- **Command reference.** `01-sources.md` is at 14,258 of 15,360 bytes; a `Porous Shell`
  section does not fit. Split it into the same-numbered folder `01-sources/` (`README.md`
  with the family intro and an index row per child, one child per component as today's
  headings, `Porous Shell` the new child), headings verbatim, every inbound link retargeted;
  run `scripts/docs_loss_audit.py` before the commit. The new page: the inputs table above
  with the real defaults, every dropdown's full option set, the exact remark and warning
  strings, what the output is, the advice (round the rim on a curved volume with a smooth
  intersection; Delaunay walls want `Smooth` ≈ 2× the wall thickness; a skin anchors clipped
  struts; a wall must span 2–3 cells at the export depth) **linked** to DualC 11/16 § The
  recipes and § Cost, never restated; recipes R-P1 … (the sponge, a uniform foam in a Rhino
  mesh, an open-cell bone with Offset, a graded foam).
- `05-field-graph-ops.md`: a `porous` row in § Sources (token, 1–2 children, the keys, the
  builder, `Porous Shell`), linking DualC 11/16.
- `06-worked-examples.md`: **W5 — a sponge** (Porous Shell → Graded Onion → Boolean smooth
  intersection), pinned by the Step 3 example test.
- `examples/README.md` and `demo.gh`: the sponge added to the canvas — needs Rhino; the
  owner's, in Step 7.
- **Design.** `02-field-graph-model.md` § The model: the arity range and `Choices`;
  § Metric and non-metric: `Porous` is metric. `04-grasshopper-plugin.md` is at 15,358 bytes:
  the new glyph row trips it — split it into `04-grasshopper-plugin/` first, the same way.
  `03-volume-and-resolvers.md`: the result of Step 5's mesh test.
- **Roadmap.** A new block **11** (`11-porous-shell.md`, or a folder if it outgrows a page),
  items from **#35**: #35 the re-pin, #36 Core vocabulary and tests, #37 the component and its
  page, #38 the Rhino smoke test. The index: block row, § Current focus, a milestone row,
  § Next up. Block 07 § 9: `porous` moves from "offered" to "bound".
- **Decisions** (next free **D-49**; the rows this plan opens, written when the work lands):

  | ID | Decision | Status | Trigger / note |
  | --- | --- | --- | --- |
  | D-49 | `porous` reaches the canvas as one `Porous Shell` Sources component; the thickness and the clip are the existing components (DualC D-69's composition); a key is emitted only off DualC's default | DONE when Step 4 lands → `01-settled.md` | — |
  | D-50 | A cancellable porous build: the create call takes no token, so `Cancel ■` waits for the cells and `Proxy preview` blocks the solve while they build | DEFERRED | a canvas whose porous build blocks a solve or a cancel for more than ~2 s; then ask DualC (07, a new §) for a create twin with a token — an ABI minor on DualC's side |
  | D-51 | A bounded thread count for create-time work (`relax`, `bake=N`): today every hardware thread, whatever `NumThreads` | DEFERRED | a canvas that needs the build bounded (a shared machine, Rhino's UI starved); then answer DualC #62's trigger, which waits on exactly this ask |
  | D-52 | Live preview of a porous **mesh** volume shows other cells than the export | opened only if Step 5's test finds a difference; DEFERRED, trigger: a user comparing the preview's cells with the printed part | — |

- `STRUCTURE.md`: the component, the split folders, the new test files if any.

## Step 7 — the Rhino smoke test (the owner, Windows, Rhino 8)

On the `.yak` of the first green CI run after Steps 1–6:

1. `Porous Shell` loads in `Boletus › Sources` with its icon; the four dropdowns show their
   named values; an unset `Spacing` is an Error on the component.
2. The sponge on a Rhino sphere mesh (`Mesh → Volume` → Porous Shell (Spacing ≈ a fifth of
   the diameter) → Graded Onion → Boolean smooth intersection with the same volume):
   `Proxy preview` at depth 6 draws closed cells with open pores at the rim.
3. `Live preview` of the same: the viewer opens on the Windows GPU (the porous GLSL was
   verified on Mesa llvmpipe only, DualC 23/08 — the first real-GPU run), `tab` to `smooth`
   and nudge; then change `Seed` on the canvas: the window reloads with new cells.
4. `Write to File` at depth 7 to `.3mf` (DualC's advice for watertightness), then a second
   write with `Cancel ■` pressed during it: the label reaches Cancelled, no file, no `.part`.
5. Record the timings and the outcome in block 11 #38.

## Verification

- `python scripts/check.py` (full) and `python scripts/check.py --docs --strict` green on
  Linux; CI green on Ubuntu and Windows with the `.yak` artifact.
- `dotnet test`: the new vocabulary, validator, example, parity, in-memory and cancel facts
  pass, none skipped on a machine with the native build; the floor raised.
- `/docs-semantic-lint` run (the session edits `src/` and `docs/design/`), recorded under
  09/06.
- Step 7 recorded, or listed as pending in block 11 and § Next up.

## Deliberately not done

- **Anything in DualC.** The node, its ABI path and its docs are DualC's; Boletus links them.
  The two upstream asks this plan can produce (a create twin with a token, D-50; a create-time
  thread count, D-51) are written as asks in block 07 only when their trigger fires.
- **A built-in thickness or clip on the component** (decision 1).
- **A uniform-push channel for `smooth`** (D-27 stands).
