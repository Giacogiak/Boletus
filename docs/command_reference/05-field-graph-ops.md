# Family 5: Field-graph ops

The `Field` builders of `Boletus.Core` and the DualC op token each one emits — everything a
canvas *can* say through Core, whether or not a component exposes it. This page pins the
**managed side**: token, arity, parameter keys with their types and whether the builder
requires them, and the component (if any) that emits the op. The **engine side** — what a
parameter means, its default, the primitive parameter order, the shorthand grammar — is
DualC's and is linked from each table:
[11/01 Input forms, op vocabulary & text shorthand](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md)
and [02 `dualc_primitive`](../../../DualC/docs/command_reference/02-dualc_primitive.md).
Every row is gated by a `--dump-json` round-trip
([design 07 § Serializer output equals `--dump-json`](../design/07-invariants-and-limits.md#serializer-output-equals---dump-json));
how the builders, the schema table and the serializer fit together is
[design 02](../design/02-field-graph-model.md).

Conventions: `[x,y,z]` is a 3-vector; *opt* means the builder argument is nullable and the
key is emitted only when supplied (DualC fills its default); **req** means the builder demands
it. Param key order is irrelevant on input (the serializer sorts keys); types and positional
order are load-bearing.

## Sources

**TPMS** (0 children; both params *opt*: `wavelength` scalar, `center` `[x,y,z]`) —
non-metric ([00 § The Normalize rule](00-shared-behaviour.md#metric-and-non-metric--the-normalize-rule)).

| Token | Builder | Component |
| --- | --- | --- |
| `gyroid` | `Field.Gyroid(wavelength?, center?)` | `TPMS` (0) |
| `schwarz-p` | `Field.SchwarzP(…)` | `TPMS` (1) |
| `diamond` | `Field.Diamond(…)` | `TPMS` (2) |
| `fischer-koch` | `Field.FischerKoch(…)` | `TPMS` (3) |
| `lidinoid` | `Field.Lidinoid(…)` | `TPMS` (4) |
| `neovius` | `Field.Neovius(…)` | `TPMS` (5) |

**Mesh sources** (0 children) — exactly one of `path` / `id` (text); `id` names a host buffer
handed over in RAM ([design 03](../design/03-volume-and-resolvers.md)). `mesh` is metric,
`winding` is not.

| Token | Params | Builder | Component |
| --- | --- | --- | --- |
| `mesh` | `path` / `id`, `sign` (text, *opt*), `normals` (text, *opt*) | `Field.Mesh(path?, sign?, normals?, id?)` | `Mesh → Volume` (Kinds 0, 1: `sign="parity"` / `"pseudonormal"`) |
| `winding` | `path` / `id` | `Field.Winding(path?, id?)` | `Mesh → Volume` (Kind 2, wrapped in `normalize`) |

**Strut lattices** (0 children; metric) — `wavelength`, `radius`, `nodeRadius` scalars and
`center` `[x,y,z]`, all *opt* ([DualC 11/02](../../../DualC/docs/command_reference/11-dualc_field/02-strut-lattices.md)).

| Token | Builder | Component |
| --- | --- | --- |
| `sc` / `bcc` / `fcc` / `octet` | `Field.Sc(wavelength?, radius?, nodeRadius?, center?)` / `Bcc` / `Fcc` / `Octet` | `Strut Lattice` (0 / 1 / 2 / 3) |

**Grouped-key primitives** (0 children; every key *opt*; metric). The meaning and default of
each key: [DualC 02 § Tier A](../../../DualC/docs/command_reference/02-dualc_primitive.md#tier-a--core-8).

| Token | Keys | Builder | Component |
| --- | --- | --- | --- |
| `sphere` | `center` `[x,y,z]`, `radius` | `Field.Sphere(radius?, center?)` | `Primitive` (1) |
| `box` | `min`, `max` | `Field.Box(min?, max?)` | `Primitive` (0) |
| `roundbox` | `min`, `max`, `radius` | `Field.RoundBox(min?, max?, radius?)` | `Primitive` (2) |
| `capsule` | `a`, `b`, `radius` | `Field.Capsule(a?, b?, radius?)` | `Segment Primitive` (0) |
| `cappedcylinder` | `a`, `b`, `radius` | `Field.CappedCylinder(a?, b?, radius?)` | `Segment Primitive` (1) |
| `torus` | `center`, `major`, `minor` | `Field.Torus(center?, major?, minor?)` | `Primitive` (3) |
| `ellipsoid` | `center`, `radii` | `Field.Ellipsoid(center?, radii?)` | `Primitive` (6) |

**Flat-`params` primitives** (0 children; one positional `params` array; every value *opt*;
metric, `triangle` / `quad` being open surfaces that need an `onion`). The builder names its
positions; the order and the defaults are DualC's
([02 § Tier B](../../../DualC/docs/command_reference/02-dualc_primitive.md#tier-b--common-10),
[§ Tier C](../../../DualC/docs/command_reference/02-dualc_primitive.md#tier-c--long-tail-12)),
and a transposition is silent to the round-trip gate, so the order is pinned by reading. The
`Axial Primitive` / `Segment Primitive` columns are the dropdown indices of
[`PrimitiveCatalog`](../../src/Boletus.Core/FieldGraph/PrimitiveCatalog.cs), whose slot → position
mapping and degree → radian conversion `PrimitiveCatalogTests` pins per shape.

| Token | Positions (count) | Builder | Component |
| --- | --- | --- | --- |
| `plane` | nx, ny, nz, offset (4) | `Field.Plane(nx, ny, nz, offset)` | `Primitive` (4) |
| `boxframe` | min xyz, max xyz, edge (7) | `Field.BoxFrame(…)` | `Primitive` (5) |
| `cone` | cx, cy, cz, angleRad, height (5) | `Field.Cone(…)` | `Axial Primitive` (0) |
| `cappedcone` | cx, cy, cz, height, radiusLow, radiusHigh (6) | `Field.CappedCone(…)` | `Axial Primitive` (1) |
| `roundcone` | a xyz, b xyz, radiusA, radiusB (8) | `Field.RoundCone(…)` | `Segment Primitive` (2) |
| `infinitecylinder` | p xyz, d xyz, radius (7) | `Field.InfiniteCylinder(…)` | `Segment Primitive` (4) |
| `hexprism` · `triprism` | cx, cy, cz, radius, halfLength (5) | `Field.HexPrism(…)` · `TriPrism(…)` | `Axial Primitive` (2 · 3) |
| `octahedron` | cx, cy, cz, size (4) | `Field.Octahedron(…)` | `Axial Primitive` (4) |
| `pyramid` | cx, cy, cz, height (4) | `Field.Pyramid(…)` | `Axial Primitive` (5) |
| `solidangle` | cx, cy, cz, angleRad, radius (5) | `Field.SolidAngle(…)` | `Axial Primitive` (6) |
| `cappedtorus` | cx, cy, cz, angleRad, major, minor (6) | `Field.CappedTorus(…)` | `Axial Primitive` (7) |
| `link` | cx, cy, cz, halfLength, major, minor (6) | `Field.Link(…)` | `Axial Primitive` (8) |
| `cutsphere` | cx, cy, cz, radius, cutHeight (5) | `Field.CutSphere(…)` | `Axial Primitive` (9) |
| `cuthollowsphere` | cx, cy, cz, radius, cutHeight, thickness (6) | `Field.CutHollowSphere(…)` | `Axial Primitive` (10) |
| `deathstar` | cx, cy, cz, radiusMain, radiusBite, distance (6) | `Field.DeathStar(…)` | `Axial Primitive` (11) |
| `vesica` | a xyz, b xyz, width (7) | `Field.Vesica(…)` | `Segment Primitive` (3) |
| `rhombus` | cx, cy, cz, la, lb, height, cornerRadius (7) | `Field.Rhombus(…)` | `Axial Primitive` (12) |
| `verticalcapsule` | cx, cy, cz, height, radius (5) | `Field.VerticalCapsule(…)` | `Axial Primitive` (13) |
| `roundedcylinder` | cx, cy, cz, radius, roundRadius, halfHeight (6) | `Field.RoundedCylinder(…)` | `Axial Primitive` (14) |
| `triangle` | a xyz, b xyz, c xyz (9) | `Field.Triangle(…)` | — (D-45) |
| `quad` | a xyz, b xyz, c xyz, d xyz (12) | `Field.Quad(…)` | — (D-45) |
| `infinitecone` | cx, cy, cz, angleRad (4) | `Field.InfiniteCone(…)` | `Axial Primitive` (15) |

`plane`, `infinitecylinder`, `infinitecone` and anything under `repeat` are unbounded —
bounds are a contour-time setting on the terminal, never a node.

## Booleans

Two children, except the three-child `mix`. Semantics and the `k` default:
[DualC 11/01 § Booleans](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md#booleans-two-children);
`mix`: [11/03 § Spatial morph](../../../DualC/docs/command_reference/11-dualc_field/03-graded-and-morph.md#spatial-morph-three-children-a-b-control).

| Token | Params | Builder | Component |
| --- | --- | --- | --- |
| `union` / `intersection` / `difference` / `xor` | — | `Field.Union(a, b)` / `Intersection` / `Difference` / `Xor` | `Boolean` (0–3); `Onion` / `Graded Onion` emit `intersection` for a `Boundary` |
| `smooth-union` / `smooth-intersection` / `smooth-difference` | `k` (scalar, *opt*) | `Field.SmoothUnion(a, b, k?)` / … | `Boolean` (4–6) |
| `mix` | `hi` (**req**), `lo` (*opt*) | `Field.Mix(a, b, control, hi, lo?)` | `Mix` |

## Decorators and domain operators

One child, except the two-child graded pair. Meanings and defaults:
[DualC 11/01 § Decorators](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md#decorators--placement-one-child),
[§ Domain operators](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md#domain-operators-one-child),
[11/03 § Graded shell & graded inflation](../../../DualC/docs/command_reference/11-dualc_field/03-graded-and-morph.md#graded-shell--graded-inflation-two-children-base-control).

| Token | Params | Builder | Component |
| --- | --- | --- | --- |
| `normalize` | — | `Field.Normalize(child)` | `Normalize`; `Mesh → Volume` (Kind 2) |
| `onion` | `thickness` (**req**) | `Field.Onion(child, thickness)` | `Onion` |
| `graded-onion` | `t1`, `t2`, `d1` (**req**), `d0` (*opt*) | `Field.GradedOnion(base, control, t1, t2, d1, d0?)` | `Graded Onion` |
| `graded-offset` | `t1`, `t2`, `d1` (**req**), `d0` (*opt*) | `Field.GradedOffset(base, control, t1, t2, d1, d0?)` | `Graded Offset` |
| `offset` | `r` (**req**) | `Field.Offset(child, r)` | `Offset` |
| `round` (alias of `offset`) | `r` (**req**) | `Field.Round(child, r)` | — |
| `scale` | `s` (*opt*) | `Field.Scale(child, s)` | — |
| `translate` | `by` `[x,y,z]` (**req**) | `Field.Translate(child, by)` | — |
| `rotate` | `axis` `[x,y,z]`, `degrees` (**req**) | `Field.Rotate(child, axis, degrees)` | — |
| `elongate` | `h` `[x,y,z]` (**req**) | `Field.Elongate(child, h)` | — ([D-23](../decisions/README.md)) |
| `transform` | `matrix` (16, row-major, **req**) | `Field.Transform(child, matrix)` | `Transform` |
| `twist` | `radiansPerUnit` (**req**), `axis` (text `"x"` / `"y"` / `"z"`, **req**) | `Field.Twist(child, radiansPerUnit, axis)` | `Twist` |
| `bend` | `curvature` (**req**), `axis` (text, **req**) | `Field.Bend(child, curvature, axis)` | `Bend` |
| `mirror` | `normal` `[x,y,z]` (**req**) | `Field.Mirror(child, normal)` | — ([D-23](../decisions/README.md)) |
| `repeat` | `period` `[x,y,z]` (**req**) | `Field.Repeat(child, period)` | — ([D-23](../decisions/README.md)) |
| `repeat-limited` | `period`, `count` `[x,y,z]` (**req**) | `Field.RepeatLimited(child, period, count)` | — |
| `displace` | `fn` (text `"sine"` / `"gyroid"` / `"bumps"`, **req**), `amplitude`, `frequency` (*opt*) | `Field.Displace(child, fn, amplitude?, frequency?)` | `Displace` |

The one per-op type quirk: `twist` / `bend` take `axis` as a **string**, unlike `rotate`'s
vector. `triangle` and `quad` are categorised metric so an `onion` over them — the only way
they gain thickness — raises no warning.

---

← Back to the [command reference](README.md) · the [docs index](../README.md)
