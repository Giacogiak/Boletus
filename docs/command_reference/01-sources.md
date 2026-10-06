# Family 1: Sources

The six components that make a `Volume` from scratch — a TPMS surface, an analytic
primitive (three components, split by the shape of their parameters), a Rhino mesh, a strut
lattice — each emitting one source node of the field graph
(the tokens: [05 § Sources](05-field-graph-ops.md#sources)). All six sit under **Boletus ›
Sources**.

## TPMS

*Nickname `TPMS`.* A triply-periodic minimal-surface field — a space-filling lattice. The
output is **infinite and non-metric**: give it a region downstream (an `Onion` `Boundary` or
a `Boolean`) and run it through `Normalize` before any metric op
([00 § The Normalize rule](00-shared-behaviour.md#metric-and-non-metric--the-normalize-rule)).
It is deliberately minimal — no centre or box input — because a space-filling lattice has no
natural limiting space of its own.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Family (`F`) | Integer, dropdown | Gyroid (0) | TPMS family: **Gyroid (0)**, **Schwarz-P (1)**, **Diamond (2)**, **Fischer-Koch (3)**, **Lidinoid (4)**, **Neovius (5)**. |
| Wavelength (`W`) | Number | *(optional; the engine default when omitted)* | Cell wavelength — smaller is finer. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The TPMS field (infinite, non-metric). |

Messages: Error `Unknown TPMS family index … (expected 0..5).` (only from a wired number
outside the dropdown).

### Keep the TPMS raw, or Normalize it?

Unlike `Mesh → Volume`, which normalizes its Winding kind for you, the TPMS is left raw and
the choice is yours, because a TPMS has three roles and two of them are better off raw.
`Normalize` never moves the surface — the zero level-set is identical — it rescales the
off-surface values toward true distance at the cost of extra gradient evaluations per sample.

Keep it raw when you only do **hard CSG / clipping** (`Intersection(TPMS, solid)` for a
network solid, or any hard `Boolean` — they need only the field's sign, which the raw TPMS
has), when you want the **bare surface**, when the TPMS is the **control** input of a
`Graded Onion` or `Graded Offset` (its raw value *is* the signal that drives the ramp), or
when performance matters on a dense, deep contour. Normalize it when a world unit must mean
a world distance: an `Onion` wall, a `Graded Onion` base, a smooth `Boolean`'s `k`, an
`Offset` distance, a `Displace` amplitude.

## Primitive

*Nickname `Prim`.* An analytic primitive — the usual clip body for a lattice, a control field
for a graded decorator, or an operand of a `Boolean`. One component with a type dropdown; the
four geometry inputs are **reinterpreted per type**. Primitives are metric. The bounded
types auto-fit at the terminal; **Plane is infinite** and says so. The shapes that do not
fit two points and two numbers are the two companion components below
([D-25](../decisions/01-settled.md)).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Type (`T`) | Integer, dropdown | Box (0) | **Box (0)**, **Sphere (1)**, **RoundBox (2)**, **Torus (3)**, **Plane (4)**, **BoxFrame (5)**, **Ellipsoid (6)**. |
| A (`A`) | Point | (−1, −1, −1) | Box / RoundBox / BoxFrame **min** · Sphere / Torus / Ellipsoid **centre** · Plane **normal** (read as a vector). |
| B (`B`) | Point | (1, 1, 1) | Box / RoundBox / BoxFrame **max** · Ellipsoid **radii** (x, y, z); ignored by the other types. |
| N0 (`N0`) | Number | 1.0 | Sphere / RoundBox **radius** · Torus **major** radius · Plane **offset**. |
| N1 (`N1`) | Number | 0.25 | Torus **minor** radius · BoxFrame **edge** thickness; ignored by the other types. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The primitive volume. |

| Type | Uses | Ignores | Emits |
| --- | --- | --- | --- |
| Box | A (min), B (max) | N0, N1 | `box(min, max)` |
| Sphere | A (centre), N0 (radius) | B, N1 | `sphere(center, radius)` |
| RoundBox | A (min), B (max), N0 (corner radius) | N1 | `roundbox(min, max, radius)` |
| Torus | A (centre), N0 (major), N1 (minor) | B | `torus(center, major, minor)` |
| Plane | A (normal), N0 (offset) | B, N1 | `plane(nx, ny, nz, offset)` |
| BoxFrame | A (min), B (max), N1 (edge) | N0 | `boxframe(min, max, edge)` — the 12 struts of the box |
| Ellipsoid | A (centre), B (radii) | N0, N1 | `ellipsoid(center, radii)` |

Messages: Remark `Plane is infinite — clip it or set bounds at Contour.` (Type = Plane);
Error `Unknown primitive type ….`. `triangle` and `quad` — open surfaces, no inside — have
builders in Core and no component ([D-45](../decisions/README.md)).

## Segment Primitive

*Nickname `SegPrim`.* A primitive **spanned between two points** — a strut, a pin, a lens.
**The inputs rename themselves after the selected type** (the canvas shows `Radius`, not
`R0`); an unused slot says `Ignored by …`; an **empty number takes the type's default**, the
engine's own ([DualC 02](../../../DualC/docs/command_reference/02-dualc_primitive.md)). Metric;
bounded types auto-fit at the terminal.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Type (`T`) | Integer, dropdown | Capsule (0) | **Capsule (0)**, **CappedCylinder (1)**, **RoundCone (2)**, **Vesica (3)**, **InfiniteCylinder (4)**. |
| A (`A`) | Point | (0, −1, 0) | First end of the axis · InfiniteCylinder: a **point on** the axis. |
| B (`B`) | Point | (0, 1, 0) | Second end of the axis · InfiniteCylinder: the axis **direction** (read as a vector). |
| R0 (`R0`) | Number | *(optional; per type)* | Capsule / CappedCylinder / InfiniteCylinder **radius** (0.5) · RoundCone **radius at A** (0.6) · Vesica **half-width** (0.6). |
| R1 (`R1`) | Number | *(optional; per type)* | RoundCone **radius at B** (0.3); ignored by the other types. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The primitive volume. |

Messages: Remark `InfiniteCylinder is infinite — clip it or set bounds at Contour.` (Type =
InfiniteCylinder); Error `Unknown Segment Primitive type … (expected 0..4).`. The emitted
tokens: [05 § Sources](05-field-graph-ops.md#sources).

## Axial Primitive

*Nickname `AxPrim`.* A primitive placed by **one point in the engine's own frame** — the
cones, prisms, the octahedron and pyramid, the partial and cut shapes. Each stands on its
own axis (Y for most, Z for the prisms, a capped torus ring in XY:
[DualC's `primitives.h`](../../../DualC/include/dualc/primitives.h)), so **orient it with
`Transform`**. The number inputs rename themselves as in `Segment Primitive`; an empty one
takes the type's default; **angles are in degrees** (the component converts to radians).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Type (`T`) | Integer, dropdown | Cone (0) | The sixteen shapes of the table below, 0–15. |
| Center (`C`) | Point | origin | The centre — the **apex** for Cone / InfiniteCone (opening along −Y), the **base** for Pyramid / VerticalCapsule (rising up +Y). |
| P0 … P3 (`P0`–`P3`) | Number | *(optional; per type)* | The type's parameters, in the table's order; the rest are ignored. |

| Type | P0 (default) | P1 | P2 | P3 | Emits |
| --- | --- | --- | --- | --- | --- |
| Cone (0) | Angle, ° (29) | Height (2) | — | — | `cone` |
| CappedCone (1) | Height, half (1) | RadiusLow, −Y cap (1) | RadiusHigh, +Y cap (0.5) | — | `cappedcone` |
| HexPrism (2) | Radius, the apothem (1) | HalfLength, along Z (1) | — | — | `hexprism` |
| TriPrism (3) | Radius (1) | HalfLength, along Z (1) | — | — | `triprism` |
| Octahedron (4) | Size, centre to vertex (1) | — | — | — | `octahedron` |
| Pyramid (5) | Height, over a side-1 base (1.5) | — | — | — | `pyramid` |
| SolidAngle (6) | Angle, ° (40) | Radius (1.5) | — | — | `solidangle` |
| CappedTorus (7) | Angle, ° of the arc kept (57) | Major (1) | Minor (0.3) | — | `cappedtorus` |
| Link (8) | HalfLength, along Y (0.5) | Major (1) | Minor (0.3) | — | `link` |
| CutSphere (9) | Radius (1) | CutHeight, Y of the cut (0.3) | — | — | `cutsphere` |
| CutHollowSphere (10) | Radius (1) | CutHeight (−0.2) | Thickness (0.1) | — | `cuthollowsphere` |
| DeathStar (11) | RadiusMain (1) | RadiusBite (0.7) | Distance, of the bite along +X (0.9) | — | `deathstar` |
| Rhombus (12) | LengthA, half-diagonal X (1) | LengthB, half-diagonal Z (0.6) | Height, half, Y (0.3) | CornerRadius (0) | `rhombus` |
| VerticalCapsule (13) | Height, the run up +Y (1.5) | Radius (0.4) | — | — | `verticalcapsule` |
| RoundedCylinder (14) | Radius (1) | RoundRadius, the rim (0.2) | HalfHeight, Y (1) | — | `roundedcylinder` |
| InfiniteCone (15) | Angle, ° (29) | — | — | — | `infinitecone` |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The primitive volume. |

Messages: Remark `InfiniteCone is infinite — clip it or set bounds at Contour.` (Type =
InfiniteCone); Error `Unknown Axial Primitive type … (expected 0..15).`. The message bar
shows the type's name, or `(mixed)` for a list of types — the slots then keep their
generic names `P0`–`P3`. The geometry of each parameter:
[DualC 02 § Tier B / C](../../../DualC/docs/command_reference/02-dualc_primitive.md#tier-b--common-10).

## Mesh → Volume

*Nickname `Mesh`.* Turns a Rhino mesh into a `Volume` so it can be clipped, booleaned or
shelled. The triangles stay **in memory** inside the `Volume` (quads are split in two) and
reach DualC in RAM at the terminal — no temp file
([design 03](../design/03-volume-and-resolvers.md)). **The output is always metric**: the two
closed-mesh kinds are true signed-distance fields and the Winding kind is wrapped in
`normalize` inside the component, so a `Normalize` after `Mesh → Volume` is never wired.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Mesh (`M`) | Mesh | — | The Rhino mesh to convert. |
| Kind (`K`) | Integer, dropdown | Mesh (closed, parity) (0) | **Mesh (closed, parity) (0)** — a signed-distance field from a watertight mesh; three probe rays with a majority vote, the robust default. **Mesh (closed, pseudonormal) (1)** — the same result on clean input from one closest-point query; faster, no extra robustness on a broken mesh. **Winding (open / soup) (2)** — a winding-number field that tolerates open shells and triangle soup and seals holes. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The mesh volume (always metric). |

Messages: Warning `No mesh connected.`; Error `Mesh has no vertices/faces.`; Error
`Unknown kind ….`. What the three kinds are in the engine:
[DualC 11/01 § Sources](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md#sources).

## Strut Lattice

*Nickname `Strut`.* A periodic **strut** (wireframe-crystal) lattice — the solid-beam
counterpart of the sheet-like `TPMS`. Unlike a TPMS it is **already metric** (a true
signed-distance field, a union of exact capsules), so no `Normalize` follows it. Like a TPMS
it is **infinite**: clip it with a `Boolean` or an `Onion` `Boundary`, or set `Min` / `Max`
at the terminal; the component always posts the infinite-extent remark.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Type (`T`) | Integer, dropdown | bcc (1) | Crystal: **sc (0)** simple cubic, **bcc (1)** body-centred, **fcc (2)** face-centred, **octet (3)** octet truss. |
| Center (`Ctr`) | Point | *(optional; the origin)* | World position of a unit-cell centre — shifts the whole tiling. |
| Wavelength (`W`) | Number | *(optional; the engine default)* | Unit-cell side, the tiling period. |
| Radius (`R`) | Number | 0.1 | Strut **half**-thickness in world units (the beam is 2·R across). Keep R below half the wavelength for an open lattice. |
| NodeRadius (`NR`) | Number | *(optional; = Radius)* | Half-thickness at the strut end-nodes: **tapered struts** when it differs from Radius (two to four times fatter nodes is the usual look). Omit for uniform struts. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The strut-lattice field (infinite, metric). |

Messages: Remark `Strut lattice is infinite — clip it with a Boolean/Onion boundary, or set
Min/Max at the terminal.` (always); Error `Unknown crystal type index … (expected 0..3).`.
Raise the terminal `Depth` until a cell is smaller than about the radius, or thin struts
fragment ([00 § Depth](00-shared-behaviour.md#depth)). The crystals, their strut counts, the
engine defaults and the taper: [DualC 11/02](../../../DualC/docs/command_reference/11-dualc_field/02-strut-lattices.md#strut-lattices-wireframe-crystals).
To hollow the struts into tubes wrap the lattice in `Onion`; to grade the strut radius across
space use `Graded Offset`, never `Onion` ([02 § Graded Offset](02-decorators.md#graded-offset)).

## Recipes

| # | Goal | Wiring |
| --- | --- | --- |
| S1 | A gyroid sheet with a metric wall | `TPMS` (Gyroid, W = 0.5) → `Normalize` → `Onion` (T = 0.12, Boundary = `Primitive` Box) — [06 W1](06-worked-examples.md#w1--a-clipped-sheet-lattice) |
| S2 | A gyroid network solid, no shell | `TPMS` → `Boolean` (Intersection, B = `Primitive` Box); no `Normalize` needed |
| S3 | A hollow Rhino part | `Mesh → Volume` (Kind 0) → `Onion` (T); no `Normalize` |
| S4 | A hole-ridden scan sealed into a solid | `Mesh → Volume` (Kind = Winding (open / soup)) → any decorator; already metric |
| S5 | An octet truss cropped to a box | `Strut Lattice` (octet, W = 0.5, R = 0.05) → `Boolean` (Intersection, B = Box) |
| S6 | Tapered, stress-aligned struts | `Strut Lattice` with R = 0.02, NR = 0.1 |
| S7 | A plane as a graded-decorator control | `Primitive` (Plane, A = (1, 0, 0), N0 = 0) into `Control` of `Graded Offset` or `Mix` |
| S8 | A radial control (`\|p\|`) | `Primitive` (Sphere, A = origin, N0 = 0) into `Control` |
| S9 | One strut between two Rhino points | `Segment Primitive` (Capsule, A, B, R0 = 0.05) — a `Boolean` Union of several makes a hand-drawn lattice |
| S10 | A hexagonal boss, standing on Z | `Axial Primitive` (HexPrism, C, P0 = apothem, P1 = half-length) → `Transform` to orient |

---

← Back to the [command reference](README.md) · the [docs index](../README.md)
