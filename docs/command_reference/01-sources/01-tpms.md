# TPMS

*Nickname `TPMS`.* A triply-periodic minimal-surface field — a space-filling lattice. The
output is **infinite and non-metric**: give it a region downstream (an `Onion` `Boundary` or
a `Boolean`) and run it through `Normalize` before any metric op
([00 § The Normalize rule](../00-shared-behaviour.md#metric-and-non-metric--the-normalize-rule)).
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

## Keep the TPMS raw, or Normalize it?

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

---

← Back to [Family 1: Sources](README.md) · the [command reference](../README.md)
