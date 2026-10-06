# Family 3: Booleans

One component under **Boletus › Booleans** combines two volumes by constructive solid
geometry: the four hard operations and three smooth (filleted) variants (the tokens:
[05 § Booleans](05-field-graph-ops.md#booleans)).

## Boolean

*Nickname `Bool`.* Constructive solid geometry on two volumes. The **hard** operations (0–3)
work on any field, metric or not — they read only the sign. The **smooth** operations (4–6)
blend by a radius `k` and assume **metric** inputs: `Mesh → Volume` (every Kind), `Primitive`
and `Strut Lattice` need no `Normalize`; only a raw `TPMS` does
([00 § The Normalize rule](00-shared-behaviour.md#metric-and-non-metric--the-normalize-rule)).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Operation (`Op`) | Integer, dropdown | Union (0) | **Union (0)**, **Intersection (1)**, **Difference (2)**, **Xor (3)**, **Smooth Union (4)**, **Smooth Intersection (5)**, **Smooth Difference (6)**. `k` applies to 4–6 only. |
| A (`A`) | Volume | — | First volume. |
| B (`B`) | Volume | — | Second volume (subtracted from A by Difference). |
| k (`k`) | Number | *(optional; the engine default)* | Smooth blend radius in world units, smooth operations only. Scale it to the model — for a 50–100 mm part try single-digit to tens of mm; a `k` far below one contour cell (≈ model size ÷ 2^Depth) gives only a faint blend. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The combined volume. |

Emits `union` / `intersection` / `difference` / `xor` / `smooth-union` / `smooth-intersection` /
`smooth-difference` of A and B, with `k` on the smooth ones when supplied. Messages: Warning
`k is ignored for hard booleans - choose a Smooth variant (op 4-6) to blend.` (a `k` is
connected while a hard operation is selected — the value is dropped); Error `Unknown
operation ….`. The engine's `k` default and the smooth-blend semantics:
[DualC 11/01 § Booleans](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md#booleans-two-children).

Intersection is also the way to **clip a TPMS or strut lattice without hollowing it**:
`Intersection(lattice, solid)` is a *network solid*, where `Onion` would give a sheet shell.

## Recipes

| # | Goal | Wiring |
| --- | --- | --- |
| B1 | A network solid | `TPMS` → A; `Primitive` Box → B; Operation = Intersection — [06 W1](06-worked-examples.md#w1--a-clipped-sheet-lattice) (variant) |
| B2 | A strut infill in a box | `Strut Lattice` → A; `Primitive` Box → B; Intersection — [06 W3](06-worked-examples.md#w3--a-strut-lattice-infill-clip--grade) |
| B3 | Bond a lattice shell to a skin | the lattice `Onion` → A; the skin `Onion` → B; Union — [06 W2](06-worked-examples.md#w2--a-printable-part-isolate--thicken--skin--union) |
| B4 | A filleted junction of two metric solids | two `Primitive`s → A, B; Smooth Union; `k` = a few mm at part scale |
| B5 | A continuous body across two crystal families | two `Strut Lattice`s (bcc, fcc) → A, B; Smooth Union with `k`; then clip |
| B6 | A hard per-region crystal swap | clip each `Strut Lattice` to a half-box (Intersection), then Union the two |
| B7 | Cut a lattice out of a part | `Mesh → Volume` → A; the clipped lattice → B; Difference |

---

← Back to the [command reference](README.md) · the [docs index](../README.md)
