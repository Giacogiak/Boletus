# Strut Lattice

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
fragment ([00 § Depth](../00-shared-behaviour.md#depth)). The crystals, their strut counts, the
engine defaults and the taper: [DualC 11/02](../../../../DualC/docs/command_reference/11-dualc_field/02-strut-lattices.md#strut-lattices-wireframe-crystals).
To hollow the struts into tubes wrap the lattice in `Onion`; to grade the strut radius across
space use `Graded Offset`, never `Onion` ([02 § Graded Offset](../02-decorators.md#graded-offset)).

---

← Back to [Family 1: Sources](README.md) · the [command reference](../README.md)
