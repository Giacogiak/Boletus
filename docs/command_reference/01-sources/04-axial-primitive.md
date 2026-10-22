# Axial Primitive

*Nickname `AxPrim`.* A primitive placed by **one point in the engine's own frame** — the
cones, prisms, the octahedron and pyramid, the partial and cut shapes. Each stands on its
own axis (Y for most, Z for the prisms, a capped torus ring in XY:
[DualC's `primitives.h`](../../../../DualC/include/dualc/primitives.h)), so **orient it with
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
[DualC 02 § Tier B / C](../../../../DualC/docs/command_reference/02-dualc_primitive.md#tier-b--common-10).

---

← Back to [Family 1: Sources](README.md) · the [command reference](../README.md)
