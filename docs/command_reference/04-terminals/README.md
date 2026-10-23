# Family 4: Terminals

The three components under **Boletus › Terminals** are the only ones that hand a graph to
DualC or to a viewer — where the field graph is finally evaluated. All three auto-fit
bounds on a finite field and take `Min` / `Max` only for an unbounded one
([00 § Bounds](../00-shared-behaviour.md#bounds--min--max-on-a-terminal)), and all three run
the validator first and post its findings ([00 § Troubleshooting](../00-shared-behaviour.md#troubleshooting)).
One page per component; the family's recipes are below.

| File | Component | What it does |
| --- | --- | --- |
| [01-write-to-file.md](01-write-to-file.md) | `Write to File` | The on-demand, background STL / 3MF exporter with its `Write ▶` / `Cancel ■` button |
| [02-proxy-preview.md](02-proxy-preview.md) | `Proxy preview` | A coarse, depth-capped proxy drawn in the viewport — preview only |
| [03-live-preview.md](03-live-preview.md) | `Live Preview` | An external GPU raymarch window on the exact field: DualCEditor in exploration mode, or DualC's `dualc_field_view` |

## Recipes

| # | Goal | Wiring |
| --- | --- | --- |
| T1 | A tiled STL of a heavy lattice | `Write to File`: Path = `C:\tmp\lattice`, Format 0, Mode 0, Depth 6 → click `Write ▶`; keep working while it runs |
| T2 | A 3MF for a slicer | Format 1 (Mode is ignored: monolithic); the file declares millimetres |
| T3 | A finer export at bounded RAM | Depth 8, Mode 0, Tile depth 5 |
| T4 | Export an unbounded lattice | connect `Min` and `Max` — or clip it on the canvas first |
| T5 | A quick look while wiring | `Proxy preview`, Depth 5 (the default) |
| T6 | The densest lattice the machine can show | `Live Preview`, `On` = true; `Min` / `Max` if the field is bare |
| T7 | Re-export after a change | `Info` reads `Inputs changed since the last write …` — click `Write ▶` again |
| T8 | Cut a lattice open and look inside while sliders move | `Live Preview`, Viewer = Editor, `On` = true; in the window `X` / `Y` / `Z` on the section toolbar, `−` / `+` to move the cut; the cut survives every solve |

---

← Back to the [command reference](../README.md) · the [docs index](../../README.md)
