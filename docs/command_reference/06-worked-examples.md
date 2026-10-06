# Family 6: Worked examples

Three canvases end to end — the canonical sheet lattice, the four-step printable part and a
strut-lattice infill — plus the demo definition under `examples/`. Each names the components
by their page (families [1](01-sources.md)–[4](04-terminals.md)) and the graph it emits.

## W1 — a clipped sheet lattice

The canonical chain:

1. **`TPMS`** — Family = Gyroid, Wavelength = 0.5 → an infinite gyroid field.
2. **`Normalize`** — wire the TPMS in → the field is metric, so the thickness will be in
   world units.
3. **`Primitive`** — Type = Box, A = (−1, −1, −1), B = (1, 1, 1) → the clip region.
4. **`Onion`** — Normalize → `Volume`, Thickness = 0.12, the Box → `Boundary` → a hollow
   gyroid sheet clipped to the box with clean cut faces:
   `intersection(onion(normalize(gyroid(wavelength=0.5)), thickness=0.12), box(…))`.
5. A terminal — **`Proxy preview`** (Depth 5) for a coarse look in the viewport, nothing
   to bake; **`Live Preview`** for true lattice fidelity; **`Write to File`** with Path =
   `C:\tmp\lattice`, Format = STL, Mode = Tiled, then `Write ▶` — a tiled STL streams in the
   background while the canvas stays live.

The un-normalized graph `intersection(onion(gyroid(wavelength=0.5), thickness=0.12), box(…))`
at Depth 6 is the analytic golden-count graph
([design 07 § The golden contour counts](../design/07-invariants-and-limits.md#the-golden-contour-counts));
the normalized chain's counts differ because `Normalize` rescales the field.

**Variant — a network solid (no shell).** Replace step 4 with a **`Boolean`** set to
Intersection (A = the TPMS, B = the Box): the lattice stays solid and is merely cropped. No
`Normalize` is needed for a hard boolean.

## W2 — a printable part (isolate → thicken → skin → union)

The four-step manufacturing workflow: a TPMS lattice **and** its enclosing volume become one
watertight, printable part, composed entirely at the field level so the whole thing is a
single contour pass with no intermediate lattice mesh. The verbs, orientation and field
expressions follow DualC's canonical workflow
([11/04](../../../DualC/docs/command_reference/11-dualc_field/04-workflow-open-surface.md)),
here on the unit cube `[−0.5, 0.5]³`, Gyroid λ = 0.3, wall thickness 0.03.

| Step | Verb | Components (wiring) | Emits |
| --- | --- | --- | --- |
| **isolate** | pick out the membrane inside the volume | `TPMS` (Gyroid, λ = 0.3) → `Normalize` → `Onion` (Boundary = `Primitive` Box −0.5 … 0.5) | `intersection(onion(normalize(gyroid)), box)` |
| **thicken** | give the membrane a wall | the `Onion`'s Thickness = 0.03 (`Graded Onion` to vary the wall across the part) | the `onion(…, 0.03)` of the row before |
| **skin** | shell the enclosing volume's own surface | a **second** `Onion` on a `Primitive` Box −0.5 … 0.5 — no `Normalize` (a box is metric), no `Boundary` | `onion(box, 0.03)` |
| **union** | bond lattice and skin | `Boolean` Union: A = the lattice shell, B = the skin | `union(shell, skin)` |

The full graph:

```
union(
  intersection( onion(normalize(gyroid(wavelength=0.3)), thickness=0.03),
                box(min=[-0.5,-0.5,-0.5], max=[0.5,0.5,0.5]) ),   // isolate + thicken
  onion( box(min=[-0.5,-0.5,-0.5], max=[0.5,0.5,0.5]), thickness=0.03 )  // skin
)
```

Wire the union into `Write to File` or `Proxy preview` as in W1. The plain union bonds the
lattice to the skin's **inner** wall through their overlap — the strut ends meet the shell,
no box faces appear.

**Refinement — seat the lattice strictly inside the inner wall.** To stop the lattice at the
skin's inner face instead of overlapping it, clip the shell to the inner cavity before the
union: an **`Offset`** with Distance = −0.03 on the boundary Box, fed as the `Onion`'s
`Boundary` — `intersection(shell, offset(box, -t))` — then union with the skin.

**Validated.** The exact composition is pinned by
`tests/Boletus.Core.Tests/FieldGraphExampleTests.cs`, which round-trips it byte for byte
through DualC's `--dump-json` and contours it end to end — the automated half; the canvas
itself cannot run headless.

## W3 — a strut-lattice infill (clip → grade)

A strut lattice is the solid-beam alternative to a TPMS sheet and needs no `Normalize`:

1. **`Strut Lattice`** — Type = octet, Wavelength = 0.5, Radius = 0.05 → an infinite, metric
   octet truss (its infinite-extent remark is expected; the next step clips it).
2. **`Primitive`** — Type = Box, A = (−1, −1, −1), B = (1, 1, 1).
3. **`Boolean`** — Operation = Intersection, A = the Strut Lattice, B = the Box → the truss
   cropped to the box, `intersection(box(…), octet(…))`, a network solid.
4. A terminal — `Proxy preview` for a look, `Write to File` (STL, Tiled, `Write ▶`) for the
   mesh. Raise `Depth` until a cell is smaller than about the radius, or thin struts drop
   out; `Live Preview` shows the fidelity the proxy cannot.

**Tapered variant (stress-aligned).** NodeRadius = 0.1 with Radius = 0.02 → fat joints,
pinched spans: stiffer per gram and more printable. Leave NodeRadius empty for uniform struts.

**Graded variant (dense core → light shell).** Wrap the lattice in **`Graded Offset`** before
the Boolean — Base = the Strut Lattice, Control = a `Primitive` Sphere with N0 = 0 (its
value is the radial distance), t1 = 0, t2 = 0.06, d0 = 0, d1 = 1 → the struts thicken with
distance from the centre; then Intersection with the Box as before. `Graded Offset`, not
`Onion`, grades a *solid* strut.

**Morph variant (same-family radius).** Two same-family `Strut Lattice`s (both bcc, Radius
0.03 and 0.09) into **`Mix`** A and B with a `Primitive` Plane as Control — watertight because
the struts coincide. A cross-family `Mix` (bcc ↔ fcc) leaves a mid-gap by design
([02 § Mix](02-decorators.md#mix)); for a same-family radius grade `Graded Offset` is the
cheaper equivalent.

The `--expr` forms of these graphs, round-tripped by the Core tests:
[roadmap 08 § Minimal `--expr` exemplars](../roadmap/08-strut-lattices.md#minimal---expr-exemplars-the-core-round-trip-test-cases).

## W4 — the demo canvas

[`examples/demo.gh`](../../examples/demo.gh) wires much of the palette together on one
canvas — `TPMS` / `Normalize`, `Primitive`, `Mesh → Volume`, `Onion` / `Graded Onion`, the
domain warps, `Boolean` and `Proxy preview`; how to open it (the `.gha` must be installed
first) is [`examples/README.md`](../../examples/README.md).

---

← Back to the [command reference](README.md) · the [docs index](../README.md)
