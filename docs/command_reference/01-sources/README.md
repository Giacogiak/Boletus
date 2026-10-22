# Family 1: Sources

The seven components that make a `Volume` from scratch — a TPMS surface, an analytic
primitive (three components, split by the shape of their parameters), a Rhino mesh, a strut
lattice, a porous foam — each emitting one source node of the field graph
(the tokens: [05 § Sources](../05-field-graph-ops.md#sources)). All seven sit under **Boletus ›
Sources**. One page per component; the family's recipes are below.

| File | Component | What it makes |
| --- | --- | --- |
| [01-tpms.md](01-tpms.md) | `TPMS` | A triply-periodic minimal surface — infinite, non-metric; when to `Normalize` it |
| [02-primitive.md](02-primitive.md) | `Primitive` | Box, Sphere, RoundBox, Torus, Plane, BoxFrame, Ellipsoid from two points and two numbers |
| [03-segment-primitive.md](03-segment-primitive.md) | `Segment Primitive` | A primitive spanned between two points — a strut, a pin, a lens |
| [04-axial-primitive.md](04-axial-primitive.md) | `Axial Primitive` | Sixteen shapes placed by one point in the engine's frame |
| [05-mesh-to-volume.md](05-mesh-to-volume.md) | `Mesh → Volume` | A Rhino mesh as a metric `Volume`, in memory |
| [06-strut-lattice.md](06-strut-lattice.md) | `Strut Lattice` | A periodic strut (wireframe-crystal) lattice — infinite, metric |
| [07-porous-shell.md](07-porous-shell.md) | `Porous Shell` | A sponge, foam or bone in any volume — zero-thick until an `Onion` / `Offset` gives it walls |

## Recipes

| # | Goal | Wiring |
| --- | --- | --- |
| S1 | A gyroid sheet with a metric wall | `TPMS` (Gyroid, W = 0.5) → `Normalize` → `Onion` (T = 0.12, Boundary = `Primitive` Box) — [06 W1](../06-worked-examples.md#w1--a-clipped-sheet-lattice) |
| S2 | A gyroid network solid, no shell | `TPMS` → `Boolean` (Intersection, B = `Primitive` Box); no `Normalize` needed |
| S3 | A hollow Rhino part | `Mesh → Volume` (Kind 0) → `Onion` (T); no `Normalize` |
| S4 | A hole-ridden scan sealed into a solid | `Mesh → Volume` (Kind = Winding (open / soup)) → any decorator; already metric |
| S5 | An octet truss cropped to a box | `Strut Lattice` (octet, W = 0.5, R = 0.05) → `Boolean` (Intersection, B = Box) |
| S6 | Tapered, stress-aligned struts | `Strut Lattice` with R = 0.02, NR = 0.1 |
| S7 | A plane as a graded-decorator control | `Primitive` (Plane, A = (1, 0, 0), N0 = 0) into `Control` of `Graded Offset` or `Mix` |
| S8 | A radial control (`\|p\|`) | `Primitive` (Sphere, A = origin, N0 = 0) into `Control` |
| S9 | One strut between two Rhino points | `Segment Primitive` (Capsule, A, B, R0 = 0.05) — a `Boolean` Union of several makes a hand-drawn lattice |
| S10 | A hexagonal boss, standing on Z | `Axial Primitive` (HexPrism, C, P0 = apothem, P1 = half-length) → `Transform` to orient |
| S11 | A sponge: closed cells, walls thickening outwards, the rim rounded | `Porous Shell` (Volume = `Primitive` Sphere, Spacing = 0.4) → `Graded Onion` (Control = `Primitive` Sphere N0 = 0, t1 = 0.04, t2 = 0.08, d1 = 1) → `Boolean` (Smooth Intersection, A = the sphere, k = 0.1) — [06 W5](../06-worked-examples.md#w5--a-sponge-porous-shell--graded-onion--smooth-intersection) |
| S12 | A uniform foam inside a Rhino part | `Mesh → Volume` → `Porous Shell` (Spacing ≈ a tenth of the part) → `Onion` (T, Boundary = the same `Mesh → Volume` output); the one mesh reaches DualC once |
| S13 | An open-cell bone | `Porous Shell` (Cells = Delaunay, Mode = Struts) → `Offset` (Distance = the strut radius) → `Boolean` (Intersection with the volume); a skin (`Onion` of the volume, `Boolean` Union) anchors the clipped stubs |
| S14 | A graded foam: small cells at the centre, large at the rim | `Porous Shell` (Spacing = 0.2, Spacing2 = 0.4, Control = `Primitive` Sphere N0 = 0, d1 = 1) → `Onion` (T = 0.04) → `Boolean` (Smooth Intersection with the volume, k = 0.1) |

---

← Back to the [command reference](../README.md) · the [docs index](../../README.md)
