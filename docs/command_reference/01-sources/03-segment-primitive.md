# Segment Primitive

*Nickname `SegPrim`.* A primitive **spanned between two points** — a strut, a pin, a lens.
**The inputs rename themselves after the selected type** (the canvas shows `Radius`, not
`R0`); an unused slot says `Ignored by …`; an **empty number takes the type's default**, the
engine's own ([DualC 02](../../../../DualC/docs/command_reference/02-dualc_primitive.md)). Metric;
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
tokens: [05 § Sources](../05-field-graph-ops.md#sources).

---

← Back to [Family 1: Sources](README.md) · the [command reference](../README.md)
