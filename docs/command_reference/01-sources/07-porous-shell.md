# Porous Shell

*Nickname `Porous`.* A natural sponge, foam or bone in any volume: DualC's `porous` source.
Seeds are placed in the `Volume`, tetrahedralized once, and the output is the **unsigned
distance to their cell complex** — the Voronoi walls or the Delaunay faces (`Mode` = Walls),
or the edges (`Mode` = Struts). Like `TPMS` it is a source whose output still needs a
thickness: the value is 0 on the walls, so **on its own it contours empty**. Give it a
thickness with `Onion`, `Graded Onion`, `Offset` or `Graded Offset`, then clip it with the
volume (an `Onion` `Boundary` or a `Boolean`) — DualC's own composition
([D-49](../../decisions/01-settled.md)). The output is **metric** (1-Lipschitz), so no
`Normalize` follows it. What each key does, its range and the measured costs are DualC's:
[DualC 11/16](../../../../DualC/docs/command_reference/11-dualc_field/16-porous-shell.md).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | The volume to fill. Read **only while the cells are built**; a metric volume places the boundary pores right (`Normalize` a raw TPMS or Winding first). |
| Cells (`Cl`) | Integer, dropdown | Voronoi (0) | **Voronoi (0)** — the cells of the seeds (foam, bone); **Delaunay (1)** — the tetrahedra's faces and edges. |
| Mode (`M`) | Integer, dropdown | Walls (0) | **Walls (0)** — closed cells, each interior cell a sealed cavity; **Struts (1)** — open cells, the edges only. |
| Distribution (`D`) | Integer, dropdown | Poisson (0) | **Poisson (0)** — blue noise, minimum distance `Spacing`; **Jitter (1)** — a grid of pitch `Spacing`, each point moved by up to `Jitter`; **Random (2)** — white noise. |
| Spacing (`S`) | Number | — *(required)* | Mean distance between seeds where the control is ≤ `d0`; > 0. Smaller means more, smaller cells, and the cost grows with the seed count. |
| Spacing2 (`S2`) | Number | *(optional; = Spacing)* | Seed distance where the control is ≥ `d1`. A value that differs from `Spacing` grades the cells and needs `Control` and `d1`. |
| Control (`C`) | Volume | *(optional)* | The field that grades the spacing — a `Primitive` Sphere with N0 = 0 is the distance from a point. Ignored unless `Spacing2` differs from `Spacing`. |
| d0 (`d0`) | Number | *(optional; 0)* | Control value where the spacing is `Spacing`. |
| d1 (`d1`) | Number | *(optional; required when graded)* | Control value where the spacing is `Spacing2`. |
| Ramp (`R`) | Integer, dropdown | Linear (0) | Grading curve between `d0` and `d1`: **Linear (0)**, **Smooth (1)** (smoothstep), **Gaussian (2)**. |
| Relax (`Rx`) | Integer | *(optional; 0)* | Lloyd iterations, 0–50: more regular cells, slower to build. |
| Seed (`Sd`) | Integer | *(optional; 0)* | The random stream, ≥ 0; the same graph gives the same cells on any OS. |
| Smooth (`Sm`) | Number | *(optional; 0)* | Radius rounding the cells' corners, ≥ 0. Delaunay walls want about twice the wall thickness. |
| Jitter (`J`) | Number | *(optional; 1)* | Seed displacement in grid pitches (0 = a regular grid). Read only when `Distribution` = Jitter. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The porous field: zero on the cell walls or struts. Give it a thickness (Onion, Graded Onion, Offset, Graded Offset), then clip it with the volume. |

**What reaches the graph.** A key at DualC's default is left out, so the canvas's JSON reads
like DualC's samples: a dropdown at index 0, an empty optional input, or a value equal to the
default emits nothing. `Control`, `Spacing2`, `d0`, `d1` and `Ramp` are emitted only when
the spacing is graded (`Spacing2` ≠ `Spacing`); `Jitter` only when `Distribution` = Jitter.
The token and its keys: [05 § Sources](../05-field-graph-ops.md#sources).

## Messages

On the component: Warning `No volume connected.`; Error `Spacing must be > 0.`; Error
`Spacing2 differs from Spacing: connect a Control field.`; Error `Spacing2 differs from
Spacing: set d1 (the control value where the spacing is Spacing2).`; Warning `The Control is
ignored unless Spacing2 differs from Spacing.`; Remark `Jitter is read only when Distribution
= Jitter.`; Error `Unknown Cells index … (expected 0..1).` (and the same for `Mode` 0..1,
`Distribution` 0..2, `Ramp` 0..2 — only from a wired number outside the dropdown). An empty
`Volume` or `Spacing` stops the solve with Grasshopper's own *failed to collect data*.

At a terminal, from the graph check
([00 § Message levels](../00-shared-behaviour.md#message-levels)): Warning `…: the porous field has no thickness —
its contour is empty; wrap it in onion / offset (or graded-onion / graded-offset)` when the
node's direct parent is not one of those (a `Boolean` or a `Transform` straight after it
warns; a control input does not); Warning `…:
'porous' places its seeds by the volume's distance, but '…' is non-metric — wrap it in
'normalize' so the boundary pores are placed right`. A key out of DualC's range (`Relax`
above 50, more than 2,000,000 seeds, `Smooth` too large for struts) is DualC's located
error, shown by the terminal like every other engine error.

## Cost and the previews

The cells are built **when the terminal creates the field**, and that call takes no cancel
token ([design 03 § The porous source at the terminals](../../design/03-volume-and-resolvers.md#the-porous-source-at-the-terminals)):

- **`Proxy preview`** builds them on the solve: invisible at the recipes' spacings (a few
  hundred seeds), seconds for tens of thousands of seeds in a Rhino mesh.
- **`Write to File`** builds them on its task, so the canvas stays live; `Cancel ■` takes
  effect once the cells are built, then cancels as usual ([D-50](../../decisions/README.md)).
- **`Live Preview`** rebuilds the cells on every input change (the viewer reloads the graph
  file); a mesh volume reaches it as an OBJ and builds the same cells as the export.

How the seed count and the build time scale with `Spacing`, and what each depth costs:
[DualC 11/16 § Cost](../../../../DualC/docs/command_reference/11-dualc_field/16-porous-shell.md#cost).
Rounding the rim, Delaunay's `Smooth`, anchoring clipped struts with a skin, the 2–3 cells a
wall must span, and writing `.3mf` for watertightness:
[DualC 11/16 § The recipes](../../../../DualC/docs/command_reference/11-dualc_field/16-porous-shell.md#the-recipes).
The recipes on the canvas are [S11–S14](README.md#recipes); the sponge end to end is
[06 W5](../06-worked-examples.md#w5--a-sponge-porous-shell--graded-onion--smooth-intersection).

---

← Back to [Family 1: Sources](README.md) · the [command reference](../README.md)
