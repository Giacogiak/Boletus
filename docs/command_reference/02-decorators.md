# Family 2: Decorators

`Volume` in, `Volume` out — the ten standalone components under **Boletus › Decorators**
that reshape a field: the metric gate, the two shell makers, a placement, five domain
operations and the two graded / morph decorators (the tokens:
[05 § Decorators and domain operators](05-field-graph-ops.md#decorators-and-domain-operators)).
Every one of them is pure tree surgery on the wire; nothing is meshed.

## Normalize

*Nickname `Norm`.* Rescales a non-metric field toward unit gradient so that downstream
thicknesses and blend radii are world units — the metric-correctness gate
([00 § The Normalize rule](00-shared-behaviour.md#metric-and-non-metric--the-normalize-rule)).
No parameters.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to normalize. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The normalized volume. |

Where: immediately after a raw `TPMS` and before the `Onion`, `Graded Onion` base, `Graded
Offset` base, smooth `Boolean`, `Offset` or `Displace` that consumes it. The canonical chain
is `TPMS → Normalize → Onion`. Not after `Mesh → Volume`, `Primitive` or `Strut Lattice`,
and not always wanted on a TPMS ([01 § Keep the TPMS raw](01-sources.md#keep-the-tpms-raw-or-normalize-it)).

## Onion

*Nickname `Onion`.* Hollows a solid field into a **shell** (wall ≈ 2 × Thickness), with an
optional boundary clip applied **after** the thickness — the lattice-shell maker
([00 § Onion before clip](00-shared-behaviour.md#onion-before-clip)).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to hollow; should be metric (`Normalize` a raw TPMS first). |
| Thickness (`T`) | Number | 0.1 | Wall **half**-thickness in world units. |
| Boundary (`B`) | Volume | *(optional)* | Clip solid; the shell is intersected with it after the thickness. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The shelled (and optionally clipped) volume. |

Emits `onion(field, thickness)`, or `intersection(onion(field, thickness), boundary)` with a
boundary. Where: after `Normalize` on a TPMS, or directly on a metric solid; a `Primitive`
box into `Boundary` crops the lattice with clean faces.

## Graded Onion

*Nickname `GOnion`.* A shell whose **wall thickness varies** across space, driven by a control
field: `t1` where the control value is at or under `d0`, `t2` where it is at or over `d1`,
a linear ramp between. Optional boundary clip after the thickness.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Base (`V`) | Volume | — | The volume to shell; should be metric. |
| Control (`C`) | Volume | — | A field whose value drives the thickness ramp; any field. |
| t1 (`t1`) | Number | 0.05 | Thickness where control ≤ `d0`. |
| t2 (`t2`) | Number | 0.2 | Thickness where control ≥ `d1`. |
| d1 (`d1`) | Number | 1.0 | Control value where the ramp ends. |
| d0 (`d0`) | Number | *(optional; 0)* | Control value where the ramp starts. |
| Boundary (`B`) | Volume | *(optional)* | Clip solid; applied after the thickness. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The graded shell volume. |

Emits `graded-onion(base, control; t1, t2, d1[, d0])`, clipped like `Onion`. Only the base
must be metric; the control is read as-is (a raw TPMS is a fine control). The engine's
semantics of the ramp:
[DualC 11/03 § Graded shell](../../../DualC/docs/command_reference/11-dualc_field/03-graded-and-morph.md#graded-shell--graded-inflation-two-children-base-control).

## Transform

*Nickname `Xform`; secondary exposure.* Applies a Rhino transform — translate, rotate,
scale, any affine — to a volume's domain.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to transform. |
| Transform (`X`) | Transform | — | A Rhino transform (from Move / Rotate / Orient …). |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The transformed volume. |

Emits `transform(field, matrix)` with the 4×4 passed **row-major**. Known convention to
verify: if a shape moves the wrong way, the op wants the inverse — feed it an inverted
Transform.

## Offset

*Nickname `Offset`.* Grows (+) or shrinks (−) a volume by sliding its level set a fixed
distance (`offset = field − Distance`): a pure level-set move, the surface translates along
its own normal and the gradient is unchanged — so the result stays metric when the input was.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to offset; should be metric for the distance to be world units. |
| Distance (`D`) | Number | 0.0 | Signed shift in world units: positive grows, negative shrinks. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The offset volume. |

Emits `offset(field, r)`. Where: a machining allowance before a terminal, an inset after a
`Mesh → Volume`, a uniform strut fattening.

## Twist

*Nickname `Twist`.* A domain warp: twists the volume helically about an axis, rotating the
plane perpendicular to the axis by `Rate` radians for every world unit travelled along it.
Distance from the axis is preserved. No metric input needed.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to twist. |
| Rate (`R`) | Number | 0.0 | Radians of twist per world unit along the axis (0 = none; π ≈ a half-turn per unit). |
| Axis (`A`) | Integer, dropdown | Z (2) | **X (0)**, **Y (1)**, **Z (2)**. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The twisted volume. |

Emits `twist(field, radiansPerUnit, axis="x"|"y"|"z")`. Messages: Error `Unknown axis index …
(expected 0..2).`. Reads best on a bounded shape; a body of revolution twisted about its own
axis shows nothing.

## Bend

*Nickname `Bend`.* A domain warp: bends the volume into a circular arc about an axis, the
angle growing at `Curvature` radians per world unit along the axis (`Curvature = 1 /
radius`). Distance from the origin within the bend plane is preserved. No metric input needed.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to bend. |
| Curvature (`C`) | Number | 0.0 | Radians per world unit along the axis (0 = straight; larger = tighter). |
| Axis (`A`) | Integer, dropdown | Z (2) | **X (0)**, **Y (1)**, **Z (2)**. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The bent volume. |

Emits `bend(field, curvature, axis)`. Messages: Error `Unknown axis index … (expected 0..2).`.
Like `Twist`, it reads best on a bounded shape (a `Primitive` box), where the arc is visible.

## Displace

*Nickname `Displace`.* A domain warp: adds an analytic surface bump to the field
(`field + Amplitude · fn(Frequency · p)`) — a sine, gyroid or bumps texture. Amplitude and
Frequency are optional; an omitted one takes the engine default.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to displace; should be metric for the amplitude to be world units. |
| Function (`F`) | Integer, dropdown | Sine (0) | **Sine (0)**, **Gyroid (1)**, **Bumps (2)**. |
| Amplitude (`A`) | Number | *(optional; the engine default)* | Peak surface displacement in world units; negative pushes inward. |
| Frequency (`Q`) | Number | *(optional; the engine default)* | Spatial frequency — higher is finer. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The displaced volume. |

Emits `displace(field, fn="sine"|"gyroid"|"bumps"[, amplitude][, frequency])`. Messages:
Error `Unknown bump function index … (expected 0..2).`. The three functions are normalized so
Amplitude is the true peak for all of them; the defaults:
[DualC 11/01 § Domain operators](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md#domain-operators-one-child).
A high Frequency needs a deeper contour to resolve. Where: a surface-finishing step, usually
near the end of a graph, on a metric solid.

## Graded Offset

*Nickname `GOffset`.* Inflates a **solid** field by an offset that ramps across space with a
control field: `t1` where the control is at or under `d0`, `t2` where it is at or over `d1`.
This is how a strut lattice's radius is graded — dense near a load path, sparse elsewhere —
and it is `Graded Offset`, not `Onion` / `Graded Onion`, because an onion would hollow the
struts into tubes. `t1 = t2` is a plain uniform offset (a handy sanity check).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Base (`V`) | Volume | — | The solid field to inflate (a `Strut Lattice`, typically); should be metric. |
| Control (`C`) | Volume | — | Field whose value drives the ramp: a `Primitive` Sphere with N0 = 0 is radial, a Plane is axial, a `Mesh → Volume` is distance-to-skin. |
| t1 (`t1`) | Number | 0.0 | Offset added where control ≤ `d0`. |
| t2 (`t2`) | Number | 0.05 | Offset added where control ≥ `d1`. |
| d1 (`d1`) | Number | 1.0 | Control value where the ramp ends. |
| d0 (`d0`) | Number | *(optional; 0)* | Control value where the ramp starts. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The graded (inflated) volume. |

Emits `graded-offset(base, control; t1, t2, d1[, d0])`. The result of a graded lattice is still
infinite: clip it downstream. The engine's definition:
[DualC 11/03 § Graded inflation](../../../DualC/docs/command_reference/11-dualc_field/03-graded-and-morph.md#graded-shell--graded-inflation-two-children-base-control).

## Mix

*Nickname `Mix`.* A value-lerp morph of two fields driven by a control: `value = lerp(A, B,
w)` with `w = clamp((control − lo) / (hi − lo))`. The tool for a **same-family radius morph** —
a thin `bcc` blending into a thick `bcc` across a plane.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| A (`A`) | Volume | — | Field at `w = 0` (control ≤ `lo`). |
| B (`B`) | Volume | — | Field at `w = 1` (control ≥ `hi`). |
| Control (`C`) | Volume | — | Scalar ramp field (a `Primitive` Plane is axial, a Sphere with N0 = 0 radial). |
| hi (`hi`) | Number | 1.0 | Control value where `w = 1` (fully B). |
| lo (`lo`) | Number | *(optional; 0)* | Control value where `w = 0` (fully A). |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The morphed volume. |

Emits `mix(A, B, control; hi[, lo])`. Messages: Remark `Mix blends field VALUES, not shapes:
watertight only when A and B are the same crystal family differing in Radius; a cross-family
blend leaves a mid-gap by design.` (always). **`Mix` blends distance values, not shapes**: it is
fluid and watertight only when A and B share their geometry (the same crystal, differing in
`Radius`); morphing two different crystals makes the struts taper to nothing at the mid-plane
and split into two bodies with a gap — inherent to interpolating disjoint fields, not a bug
([DualC design 10 § `mix` blends values, not shapes](../../../DualC/docs/design/10-invariants-and-tolerances.md#mix-blends-values-not-shapes)).
For a continuous solid across two families use a smooth `Boolean`; for a hard per-region swap
clip each crystal to a half-region and `Union`; for a same-family radius grade `Graded
Offset` is the cheaper equivalent.

## Recipes

| # | Goal | Wiring |
| --- | --- | --- |
| D1 | A metric gyroid sheet, clipped clean | `TPMS` → `Normalize` → `Onion` (T = 0.12, Boundary = `Primitive` Box) |
| D2 | A shell thick at the top, thin at the bottom | `Normalize`(TPMS) → `Graded Onion` Base; `Primitive` Plane (A = (0, 0, 1), N0 = 0) → Control; t1 = 0.05, t2 = 0.2, d0 = −1, d1 = 1 |
| D3 | Hollow struts (tubes) | `Strut Lattice` → `Onion` (T = 0.03) → `Boolean` Intersection with a clip |
| D4 | A radially graded strut radius | `Strut Lattice` → `Graded Offset` Base; `Primitive` Sphere (N0 = 0) → Control; t1 = 0, t2 = 0.06, d1 = 1 |
| D5 | Fatten a lattice toward the part's skin | the clip `Mesh → Volume` wired into both `Graded Offset` Control and the downstream `Boolean` — one solid, two roles |
| D6 | A same-family thin-to-thick morph | two `Strut Lattice` (both bcc, R = 0.03 and 0.09) → `Mix` A / B; `Primitive` Plane → Control; lo = −0.6, hi = 0.6 |
| D7 | Seat a lattice inside a skin's inner wall | `Offset` (D = −t) on the boundary Box → the lattice `Onion`'s Boundary — [06 W2](06-worked-examples.md#w2--a-printable-part-isolate--thicken--skin--union) |
| D8 | A twisted block | `Primitive` Box → `Twist` (R = 1.0, Axis = Z) |
| D9 | A textured surface | `Normalize`(TPMS) or any metric solid → `Displace` (Sine, A = 0.05, Q = 6) |
| D10 | Reposition a volume | any `Volume` → `Transform` (X from a Move / Rotate component) |

---

← Back to the [command reference](README.md) · the [docs index](../README.md)
