# 05 — Phase 3b: Grasshopper components (MVP)

**DONE** (2026-06-18 … 2026-10-05): the thin slice, the `Volume` palette, the
diskless-mesh flip, the tiled STL export, the capped proxy preview, the component icons, the
domain-warp decorators, the example graph, the async `Write to File` exporter, the
strut-lattice components, the broader `Primitive` set — increment 3b.3 closed
([08](08-broader-primitive-set.md)) — and `Write to File` Phase C, the cooperative cancel and
the real percentage ([06](06-write-to-file.md), D-30 settled), then the contour diagnostics —
`Proxy preview` and `Write to File` telling an empty contour from real geometry
([09](09-contour-diagnostics.md), D-37 settled). What the block leaves is a
list of manual Rhino smoke tests, every increment's, pending on a Windows machine
([roadmap index § Next up](../README.md#next-up)).

The record of the `Boletus.Grasshopper` `.gha`: the GH node palette over the
[field-graph serializer](../04-phase3-field-graph-serializer.md) and the
[wrapper](../03-phase2-core-wrapper.md), plus the Rhino glue — the plan as it was made, then
one dated entry per increment as it landed; page 01 is the plan as drafted on 2026-06-18, its
sections framed by a dated note that names the pages owning the present tense. How the plugin
*is* today — the component
families, the proxy cap, the writer's threading model, the viewer's process model — is
[design 04](../../design/04-grasshopper-plugin.md); how each component is driven is the
[command reference](../../command_reference/README.md). Split on 2026-09-21 (roadmap 09
Phase 4) from one page into this folder; every heading is verbatim and every increment keeps
its anchor on the child that holds it.

## The pages of this topic

| Page | Sections |
| --- | --- |
| [01 The plan — the `Volume` datatype, the component model, the MVP palette, the Rhino glue](01-volume-datatype-and-palette.md) | [Project](01-volume-datatype-and-palette.md#project) · [The Volume datatype](01-volume-datatype-and-palette.md#the-volume-datatype-the-plugins-exchange-currency) · [Component model](01-volume-datatype-and-palette.md#component-model-confirmed-decisions) · [MVP palette](01-volume-datatype-and-palette.md#mvp-palette) · [Rhino glue](01-volume-datatype-and-palette.md#rhino-glue-bake-these-in-from-the-start) · [Verification](01-volume-datatype-and-palette.md#verification) · [Open questions](01-volume-datatype-and-palette.md#open-questions) |
| [02 Increments 3b.1 and 3b.2](02-thin-slice-and-volume-palette.md) | [3b.1 — thin vertical slice (2026-06-18)](02-thin-slice-and-volume-palette.md#implemented-3b1--thin-vertical-slice-2026-06-18) · [Resolved decisions (build environment; its local-only clause reversed by D-46)](02-thin-slice-and-volume-palette.md#resolved-decisions-build-environment-pinned-2026-06-18) · [3b.2 — Volume datatype + boundary-driven palette (2026-06-18)](02-thin-slice-and-volume-palette.md#implemented-3b2--volume-datatype--boundary-driven-palette-2026-06-18) |
| [03 The diskless-mesh flip and the tiled STL export](03-diskless-mesh-and-tiled-stl.md) | [diskless-mesh flip + Volume→Core relocation (2026-06-19)](03-diskless-mesh-and-tiled-stl.md#implemented--diskless-mesh-flip--volumecore-relocation-2026-06-19) · [3b.3 — tiled STL export (2026-06-19)](03-diskless-mesh-and-tiled-stl.md#implemented-3b3-partial--tiled-stl-export-2026-06-19) |
| [04 The capped proxy preview and the component icons](04-proxy-preview-and-icons.md) | [3b.3 — capped proxy preview (2026-06-20)](04-proxy-preview-and-icons.md#implemented-3b3-partial--capped-proxy-preview-2026-06-20) · [3b.3 — component icons (2026-06-20)](04-proxy-preview-and-icons.md#implemented-3b3-partial--component-icons-2026-06-20) |
| [05 The domain-warp decorators and the example graph](05-domain-warps-and-example-graph.md) | [3b.3 — domain-warp decorators (2026-06-25)](05-domain-warps-and-example-graph.md#implemented-3b3-partial--domain-warp-decorators-2026-06-25) · [3b.3 — example graph (2026-07-03)](05-domain-warps-and-example-graph.md#implemented-3b3-partial--example-graph-2026-07-03) |
| [06 The async, manual `Write to File` exporter](06-write-to-file.md) | [async, manual Write to File exporter (2026-07-05)](06-write-to-file.md#implemented--async-manual-write-to-file-exporter-2026-07-05) — why, the phased plan, the architecture, the constraints, the verification; Phase C unblocked (2026-10-02) · [Phase C — cooperative cancel and a real percentage](06-write-to-file.md#phase-c--cooperative-cancel-and-a-real-percentage) |
| [07 The strut-lattice components and the remaining increment](07-strut-lattice-components.md) | [strut lattices — Strut Lattice / Graded Offset / Mix (2026-07-10)](07-strut-lattice-components.md#implemented--strut-lattices-strut-lattice--graded-offset--mix-2026-07-10) · [Next increment (3b.3 — remaining)](07-strut-lattice-components.md#next-increment-3b3--remaining-planned) — closed by 08 |
| [08 The broader `Primitive` set](08-broader-primitive-set.md) | [broader Primitive set — Primitive + Segment Primitive + Axial Primitive (2026-10-03)](08-broader-primitive-set.md#implemented--the-broader-primitive-set) · [The fork (D-25) and what stays out (D-45)](08-broader-primitive-set.md#the-fork-d-25-and-what-stays-out-d-45) · [Verification](08-broader-primitive-set.md#verification-broader-primitive-set) |
| [09 The contour diagnostics — an empty-contour warning on the terminals](09-contour-diagnostics.md) | [contour diagnostics — DualcDiagnostics bound, Proxy preview + Write to File (2026-10-05)](09-contour-diagnostics.md#implemented--the-contour-diagnostics) · [Design choices and the rejected probes](09-contour-diagnostics.md#design-choices-and-the-rejected-probes) · [Verification](09-contour-diagnostics.md#verification-contour-diagnostics) |

The decisions taken in these pages — the `Volume` currency (D-13), the diskless path (D-14),
tiled STL (D-15), the proxy cap (D-18), procedural icons (D-20), `Write to File` (D-28), the
primitive input model (D-25), the diagnostics binding (D-37) and
the dropped or deferred alternatives — are rows of the [decisions index](../../decisions/README.md).

---

← Back to the [Roadmap index](../README.md) · prev: [04 — Field-graph serializer](../04-phase3-field-graph-serializer.md) · next: [06 — Distribution & packaging](../06-phase4-distribution-and-packaging.md)
