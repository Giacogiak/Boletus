# 11 — Porous shell: DualC's `porous` source on the canvas

**PARTIAL: DONE (2026-10-22, #35–#37); #38 PLANNED.** The record of binding DualC's `porous`
source — seeds placed in any volume, the unsigned distance to their Voronoi or Delaunay cell
complex, thickened by the existing decorators into a sponge, a foam or a bone (DualC roadmap
23, DualC #63–#68). DualC's owner decided that Boletus gets a plan only (DualC D-72); DualC's
plan-run wrote it unattended on 2026-10-22 ([raw/2026-10-22](../raw/2026-10-22-porous-shell-plan.md)),
and the owner confirmed its six defaults the same day — one `Porous Shell` source, no built-in
thickness or clip, every key an input, a key emitted only off DualC's default, the ranges
DualC's, `Seed` a Grasshopper Integer — before this session implemented it. Headings are
frozen at ID + title; status, date and evidence live on the body lines. The usage contract is
[cmdref 01 § Porous Shell](../command_reference/01-sources/07-porous-shell.md); the mechanism
[design 02 § Metric and non-metric](../design/02-field-graph-model.md#metric-and-non-metric)
and [design 03 § The porous source at the terminals](../design/03-volume-and-resolvers.md#the-porous-source-at-the-terminals);
the decisions [D-49](../decisions/01-settled.md), [D-50 and D-51](../decisions/README.md).

## #35 Re-pin: DualC `main` with the porous source

**DONE — 2026-10-22.** The submodule moved from `350cb3a` (ABI 0.5.1) to the merge of DualC's
`feat/porous-shell` into DualC `main` on GitHub (PR #3) plus its CI fixes — the first pin on
DualC's `main` rather than a side branch, which also closes the snapshot's re-pin item. Read
at the new tree against the plan's base (`4bed5e3`): the parser, `porous.h` and the complex
differ only in moved comments, so the plan's key table holds; `dualc_c.h` has no new entry
point (`porous` is graph vocabulary, `FromJson(json, meshes)` takes it), and the version
string reads `0.5.2`. `NativeMethods.cs` did not move.

- `scripts/build_native.py` rebuilt the library and `dualc_field` at the pin (75 `dualc_`
  exports); its own `dualc_c_demo … cancel` passed.
- DualC's `dualc_c_demo <out>.stl porous` passed — `porous(mesh(id))` over the in-memory cube
  watertight, JSON == shorthand byte for byte, the cancel cases, the two negative graphs
  rejected with DualC's wording. It must run **from `build-native/capi/`**, where `cube.obj`
  sits; from any other directory it stops with `could not load cube.obj into buffers`.
- The five code comments citing DualC's retired `02-dualc_primitive.md` (`Ops.cs`,
  `OpSchema.cs`, `Field.cs`) now cite `11-dualc_field/12-primitive-catalogue.md`; the docs
  links to `02-dualc_primitive.md` still resolve (the page exists at the pin) and stay.
- [07 § 9](07-upstream-coordination/03-export-callback-and-strut-sync.md#9-dualc-at-the-pin--what-2fcd19f-offers-that-boletus-does-not-use)
  is the inventory of the `2fcd19f` pin and predates `porous`; it is not appended (its page is
  at the cap) — this item is the record of what the new pin offers and Boletus now binds.

## #36 Core: the porous vocabulary and its tests

**DONE — 2026-10-22.** `OpSchema` carries an arity range (`MinChildren` / `MaxChildren`; the
exact constructor sets both, so every other row is unchanged and the exact-count message keeps
its wording), `ParamSpec.Choices` for a closed-set text key, and `OpCategory.Porous` (metric).
`Ops.cs` has the `porous` row with the twelve keys as `readPorousParams` reads them;
`Field.Porous(volume, spacing, control?, …)` emits each optional key only when supplied. The
validator rejects a name outside its choices, a graded spacing without a control or without
`d1` (DualC's own wording for the first), and warns on a control that does nothing, a
non-metric volume, and a `porous` node with no thickening parent (onion, offset, round, the
base of a graded op) unless it is read as a control.

Tests (all at depth ≤ 6; the gate floor 180 → 196): two vocabulary round-trips against
`--dump-json` (the minimal node; every key off its default with a control); eight validator
facts; and `PorousShellTests` — the sponge as the canvas composes it equals DualC's
`porous-sponge.json` after `--dump-json` and contours non-empty; DualC's watertight foam
graph exported through the wrapper is byte-identical to `dualc_field`'s; an icosahedron mesh
used as volume and clip is one buffer and contours identically twice; a cancel in the contour
stage of a porous export leaves no file and no `.part`; and the Live Preview's OBJ path
builds the same triangles as the in-memory path.

**The OBJ path, measured** (the plan's open question). An icosahedron of circumradius 1 (its
coordinates irrational, written as float text and read back as doubles), depth 6, each graph
contoured through `VolumeResolver` (in memory) and through `MeshMaterializingResolver` (OBJ):

| Graph over the mesh `M` | Triangles (both paths) | Indices equal | Coordinates moved > 1e-5 | Max shift |
| --- | --- | --- | --- | --- |
| `onion(M, 0.04)` — no porous node | 60,456 | yes | 249 of 90,696 | 0.022 |
| `M ∩ onion(normalize(gyroid(0.4)), 0.04)` | 140,532 | yes | 105 of 208,800 | 0.0054 |
| `M ∩ onion(porous(M, 0.3), 0.04)` | 100,660 | yes | 67 of 151,200 | 0.0083 |
| the same, `dist=jitter, jitter=0` | 80,844 | yes | 90 of 121,464 | 0.0035 |
| `M ∩ onion(porous(sphere(1), 0.3), 0.04)` — seeds in an analytic volume | 99,676 | yes | 68 of 149,832 | 0.0085 |

Same triangles everywhere, so the same cells; the shifts are as large without a porous node,
and as large when the seeds are placed in an analytic sphere, so they are the mesh path's own
baseline at the icosahedron's creases, not moved seeds. D-52 (a preview showing other cells
than the export) was **not opened**; the test pins topology equality.

**Two engine limits left as they are.** The create call builds the cells with no cancel token
(D-50) and on every hardware thread whatever `NumThreads` says (D-51); at the recipes'
spacings the build is milliseconds, so both are DEFERRED, each with the upstream ask its
trigger would send.

## #37 The `Porous Shell` component, its icon and its pages

**DONE — 2026-10-22** (compiled on Linux; the Rhino run is #38). `PorousShellComponent.cs`,
`Boletus › Sources`, GUID `…000000000110`, fourteen inputs in the plan's order with `Jitter`
last; `BoletusIcons.PorousShell`, Voronoi walls with degree-3 junctions clipped to a disk
(previewed at 10× and at palette size before it was written). Three choices the plan left
open, made in the code: the control, `spacing2`, `d0`, `d1` and the ramp are emitted **only
when the spacing is graded**, and `jitter` only with `Distribution` = Jitter, so an unused
input never reaches the graph (the validator's control-without-grading warning remains for
hand-built graphs); an unconnected `Volume` is a Warning, the house rule for every decorator,
not the plan's Error; and an empty `Spacing` is Grasshopper's own *failed to collect data* on
a required input, not a component message. Docs: `01-sources.md` (14,258 bytes) split into
`command_reference/01-sources/` — one page per component, headings verbatim, every inbound link
retargeted — with the `Porous Shell` page and recipes S11–S14; `05` the `porous` row; `06` W5,
the sponge; `design/04-grasshopper-plugin.md` (15,358 bytes) split the same way into
`design/04-grasshopper-plugin/` before its families and icons children took the new component.
`scripts/docs_loss_audit.py --base 2423275 --head 4f4db6d` (a scratch triage, not committed):
no heading, identifier or sentence of the two split pages lost — one sentence partial, the
family's "All six sit under" now "seven"; the roadmap index's four residuals are the snapshot
updated on purpose (the `2fcd19f` pin paragraph and the re-pin item, both closed by #35; the
2026-10-05 milestone row, out of the D-43 window).

## #38 The Rhino smoke test

**PLANNED.** On the `.yak` of the first green CI run with #35–#37, on Windows with Rhino 8 —
the plan's [Step 7](../raw/2026-10-22-porous-shell-plan.md#step-7--the-rhino-smoke-test-the-owner-windows-rhino-8):
the component loads with its icon and its four dropdowns; the sponge on a Rhino sphere mesh in
`Proxy preview` at depth 6; `Live Preview` of it on a real GPU (DualC verified the porous GLSL
on Mesa llvmpipe only), a `Seed` change reloading the window; `Write to File` at depth 7 to
`.3mf`, then a cancelled second write leaving no file. The timings and the outcome are
appended here.

---

← Back to the [Roadmap index](README.md).
