# Primitive

*Nickname `Prim`.* An analytic primitive — the usual clip body for a lattice, a control field
for a graded decorator, or an operand of a `Boolean`. One component with a type dropdown; the
four geometry inputs are **reinterpreted per type**. Primitives are metric. The bounded
types auto-fit at the terminal; **Plane is infinite** and says so. The shapes that do not
fit two points and two numbers are the two companion components below
([D-25](../../decisions/01-settled.md)).

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
builders in Core and no component ([D-45](../../decisions/README.md)).

---

← Back to [Family 1: Sources](README.md) · the [command reference](../README.md)
