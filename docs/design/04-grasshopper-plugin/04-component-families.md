# The component families

All components sit on the **Boletus** tab in four sub-panels. A terminal creates one
`DualcField` per contour or write and disposes it in a `finally` — `Proxy preview` inside
`SolveInstance`, `Write to File` on its worker thread, where the whole create → export →
dispose lifetime stays
([01 § The single-threaded rule](../01-native-interop.md#the-single-threaded-rule)); a
non-terminal never touches the engine at all.

- **Sources** (→ `Volume`): `TPMS` (family dropdown + wavelength, an infinite non-metric
  lattice), `Primitive` (type dropdown — box, sphere, roundbox, torus, plane, boxframe,
  ellipsoid — over a fixed `A / B / N0 / N1` input model; plane posts an infinite-extent
  remark), `Segment Primitive` and `Axial Primitive` (the other analytic primitives, grouped
  by parameter shape — two points + two numbers, one point + four numbers — over
  `PrimitiveCatalog` in Core: a fixed slot row whose names, tooltips and defaults follow the
  type dropdown, relabelled after each solve so a saved canvas never loses a wire; an empty
  number takes the shape's engine default, an angle slot is entered in degrees and converted;
  the two unbounded shapes post the remark), `Mesh → Volume`
  (a Rhino mesh → `MeshBuffer` + a `mesh` / `winding` leaf, its `Kind` choosing parity,
  pseudonormal or winding; always metric, [06](../06-conventions.md#metric-by-default-sources-and-the-normalize-rule)),
  `Strut Lattice` (crystal dropdown `sc` / `bcc` / `fcc` / `octet`, center, wavelength,
  radius, optional node radius; metric, infinite, posts a remark), `Porous Shell` (DualC's
  `porous` over a volume input and an optional control: four dropdowns — cells, mode,
  distribution, ramp — and the numeric keys, each left out of the graph at DualC's default;
  the control, `spacing2`, `d0`, `d1` and the ramp are emitted only when the spacing is
  graded; a metric, zero-thick field that the existing decorators thicken and a `Boolean`
  clips — the component adds neither).
- **Decorators** (`Volume` in → `Volume` out), all standalone: `Onion` (thickness + optional
  boundary, clipped after the shell), `Graded Onion` (base + control + `t1` / `t2` / `d1` /
  `d0` + optional boundary), `Normalize`, `Transform` (a Rhino `Transform` → the `transform`
  matrix), `Offset`, `Twist` and `Bend` (scalar + axis dropdown), `Displace` (function
  dropdown + optional amplitude / frequency, absent values deferring to DualC's defaults),
  `Graded Offset` (the solid-radius companion of `Onion`) and `Mix` (three volumes + `hi` /
  optional `lo`; posts the value-blend remark).
- **Booleans**: one `Boolean` component, an operation dropdown over the four hard and three
  smooth ops with `k` for the smooth ones (`k` supplied to a hard op warns).
- **Terminals**: `Write to File`, `Proxy preview`, `Live Preview` — the only components that
  hand a graph to DualC or to the viewer.

Dropdowns follow one pattern: a `Param_Integer` with `AddNamedValue` for every option and
the full option set repeated in the input description; the int → token maps (`AxisName.cs`
for `0/1/2 → "x"/"y"/"z"`, `0/1/2 → "sine"/"gyroid"/"bumps"` for `Displace`) use the exact
strings the vocabulary tests round-trip, and an out-of-range index posts an error. A GUID is
never reused across an input reorder ([06](../06-conventions.md#the-guid-rule)).

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
