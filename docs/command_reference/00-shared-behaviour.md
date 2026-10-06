# Family 0: Shared behaviour

What every Boletus component does the same way: the wire type, the metric / Normalize rule,
how a terminal takes bounds, what each message level means, and the troubleshooting table
of every string the canvas can show.

## The `Volume` wire

Every wire between Boletus components carries a **`Volume`** (parameter name `Volume`,
nickname `V`, description "A DualC implicit volume (field graph)."): a lightweight
**field graph** — a symbolic recipe for an implicit field — plus the raw triangles of any
mesh a `Mesh → Volume` component imported. It is **not** baked geometry: connecting a
`Boolean`, an `Onion` or a `Transform` is tree surgery on that recipe, **nothing is meshed
and no disk is touched until a terminal** (`Write to File`, `Proxy preview`, `Live
Preview`) consumes the graph. A `Volume` shows on the canvas as `Volume [<op>]` (the root
op token) and is computed on every solve, never stored in the definition; the parameter is
not placed from the palette. The contract and its rationale:
[design README § The `Volume` contract](../design/README.md#the-volume-contract).

## Metric and non-metric — the Normalize rule

A field is **metric** when its value is true distance (gradient ≈ 1), so a thickness of
`0.1` means 0.1 world units. `Onion`, `Graded Onion`'s base, `Graded Offset`'s base and the
three **smooth** booleans assume a metric input. Every source emits a metric `Volume` by
default — `Mesh → Volume` (all three Kinds; the Winding kind is normalized inside the
component), the three `Primitive` components and `Strut Lattice` — except a **raw `TPMS`**, which is left
non-metric because whether to normalize it is the user's call
([design 06 § Metric-by-default sources and the Normalize rule](../design/06-conventions.md#metric-by-default-sources-and-the-normalize-rule)).
So the one place a `Normalize` belongs is between a raw `TPMS` and a metric-assuming
component, immediately after the source:

```
TPMS  →  Normalize  →  Onion  →  Write to File   (or Proxy preview / Live Preview)
```

A `Normalize` is **not** needed after `Mesh → Volume` of any Kind, after any `Primitive` or a
`Strut Lattice`, before the four **hard** booleans (they read only the field's sign), or
after an `Onion` (the shell is already metric). Nor is it always wanted on a TPMS — for hard
CSG, bare-surface extraction or a `Graded Onion` *control* input, the raw field is the right
one ([01 § Keep the TPMS raw, or Normalize it?](01-sources.md#keep-the-tpms-raw-or-normalize-it)).

Skipping a `Normalize` where one is needed **still contours**: the terminal posts the
validator's warning (quoted in [§ Troubleshooting](#troubleshooting)) and the wall thickness
or blend radius is simply not in world units. A correctness hint, never a hard error.

## Onion before clip

A boundary clip is not a separate component: it is the optional `Boundary` input of `Onion`
and `Graded Onion`, applied **after** the thickness — `intersection(onion(field), boundary)` —
so the cut faces come out clean instead of being shelled too
([design 06 § Onion before clip](../design/06-conventions.md#onion-before-clip)). To crop a
lattice *without* hollowing it, use a `Boolean` set to Intersection.

## Bounds — `Min` / `Max` on a terminal

A terminal auto-fits the bounds of a finite field. A bare `TPMS`, a `Strut Lattice`, a
`Plane` primitive or anything built on one is **unbounded**, and the engine refuses to
contour it without bounds
([design 07 § Unbounded fields](../design/07-invariants-and-limits.md#unbounded-fields)):
either give it a finite extent on the canvas (an `Onion` `Boundary`, a `Boolean`
Intersection with a solid) or connect **both** `Min` and `Max` on the terminal. A single
corner is ignored with a warning. Bounds are a contour-time setting, never part of the
`Volume`.

## Depth

`Depth` is the octree depth of the dual contour; the cost of a lattice is exponential in it,
so contours are kept **coarse** — `Write to File` defaults to 6 and warns above 8,
`Proxy preview` defaults to 5 and clamps at its ceiling
([design 07 § The coarse-depth rule](../design/07-invariants-and-limits.md#the-coarse-depth-rule)).
A feature thinner than about two to three cells fragments or vanishes — DualC's
[resolution rule](../../../DualC/docs/design/10-invariants-and-tolerances.md#the-resolution-rule-a-feature-is--23-cells-or-it-does-not-exist).

## Message levels

Grasshopper's three runtime-message levels carry one meaning each
([design 06 § Message levels](../design/06-conventions.md#message-levels)):

- **Remark** (grey) — an expected note to read once; the graph is fine.
- **Warning** (orange) — actionable and non-fatal; the graph still runs.
- **Error** (red) — the component refuses to run.

Every decorator and boolean also posts a Warning naming the missing input when a required
`Volume` is not connected (`No volume connected.`, `No base volume connected.`, `No control
volume connected.`, `No A volume connected.`, `No B volume connected.`); a terminal posts
`No volume connected.`. An out-of-range dropdown index — only reachable by wiring a number
into the dropdown — posts an Error naming the range.

## Units

The numbers on the canvas are the millimetres in the exported file; Boletus performs no unit
conversion and a `.3mf` declares 1 unit = 1 mm ([design 06 § Units](../design/06-conventions.md#units)).

## Troubleshooting

The strings, quoted from the source (`…` marks a value filled in at run time), with the
component that posts each.

| Message you see | Level | Posted by | What it means | Fix |
| --- | --- | --- | --- | --- |
| `/root/…: op '…' assumes a metric (signed-distance) input, but '…' is non-metric — wrap it in 'normalize' for a metric wall thickness (DualC still contours it as-is)` | Warning | any terminal (the validator) | A raw `TPMS` feeds an `Onion`, a `Graded Onion` base, a `Graded Offset` base or a smooth `Boolean`. It still contours; the thickness or blend is not in world units. | Insert a `Normalize` between the `TPMS` and that component. |
| `Field is unbounded — connect a boundary (e.g. clip via Onion/Boolean) or set Min/Max.` | Error | `Proxy preview` | No finite extent and no bounds. | A boundary clip, or both `Min` and `Max`. |
| `Field is unbounded — connect a boundary (Onion/Boolean) or set Min/Max.` | Error | `Write to File` | Same, at export time. | Same. |
| `Provide both Min and Max to set bounds; ignoring the single corner (auto-bounds).` | Warning | every terminal | Only one bounds corner is connected. | Connect both, or neither. |
| `Plane is infinite — clip it or set bounds at Contour.` | Remark | `Primitive` (Type = Plane) | A plane has no finite extent; expected. | Use it as a clip or control, or set bounds. |
| `InfiniteCylinder is infinite — clip it or set bounds at Contour.` · `InfiniteCone is infinite — …` | Remark | `Segment Primitive` (Type = InfiniteCylinder) · `Axial Primitive` (Type = InfiniteCone) | Same; expected. | Same. |
| `Strut lattice is infinite — clip it with a Boolean/Onion boundary, or set Min/Max at the terminal.` | Remark | `Strut Lattice` | Always fires; expected. | Clip it, or set bounds at the terminal. |
| `Mix blends field VALUES, not shapes: watertight only when A and B are the same crystal family differing in Radius; a cross-family blend leaves a mid-gap by design.` | Remark | `Mix` | Always fires; a cross-family morph shows a gap. | Expected if intentional; otherwise a smooth `Boolean`, or `Graded Offset` for a same-family radius grade. |
| `k is ignored for hard booleans - choose a Smooth variant (op 4-6) to blend.` | Warning | `Boolean` | `k` is connected but the operation is hard (0–3). | Pick a Smooth operation, or disconnect `k`. |
| `Depth … on a lattice can be very expensive (exponential per level).` | Warning | `Write to File` | `Depth` > 8. | Lower it; 6 is the default. |
| `Proxy depth capped at 7 (preview is a coarse LOD; use the raymarch side-car for fidelity).` | Remark | `Proxy preview` | A depth above the ceiling was asked for. | Expected; `Write to File` for a finer mesh, `Live Preview` for fidelity. |
| `No surface in the sampled region — the volume crosses zero nowhere inside the bounds, so there is nothing to draw. Check Min/Max against the volume's position, or the volume's size.` | Warning | `Proxy preview` | The engine found no surface and returned its one placeholder triangle; nothing is drawn. Needs a DualC library at ABI 0.4.0 or later. | Move or widen `Min`/`Max`, or check the volume's size and position. |
| `Proxy mesh is open (… boundary, … non-manifold edges) — expected where Min/Max cut through the surface; otherwise the volume itself is open.` | Remark | `Proxy preview` | The drawn proxy has boundary edges. | Expected under a bounds cut; otherwise close the volume (a clip, an `Onion`). |
| `No surface in the sampled region — … holds only a placeholder triangle. Check Min/Max against the volume's position, or the volume's size.` | Warning | `Write to File` | A monolithic write completed, but the file holds the engine's one placeholder triangle. | As for the proxy; then re-export. |
| `BUSY — a write is running with older inputs. Click Write to queue a restart with the new inputs, or wait for it to finish.` | Warning | `Write to File` | The definition changed while a write was running. | Wait, or click `Write ▶` to queue one restart. |
| `A file path is required.` · `Directory does not exist: …` | Error | `Write to File` | Empty `Path`, or its parent folder is missing. | Give a path in an existing folder. |
| `Unknown format … (expected 0 = STL, 1 = 3MF).` · `Unknown mode … (expected 0 = Tiled, 1 = Monolithic).` | Error | `Write to File` | A number outside the selector's range. | 0 or 1. |
| `Path extension '…' overridden by the Format selector → '…'.` | Warning | `Write to File` | The typed extension disagrees with `Format`. | Expected; `Format` decides. |
| `3MF has no tiled writer; writing monolithically.` | Warning | `Write to File` | `Format` = 3MF with `Mode` = Tiled. | Expected; 3MF is always monolithic. |
| `Tile depth … ≥ Depth …: tiling falls back to a single pass (no RAM benefit).` | Warning | `Write to File` | `Tile depth` is not below `Depth`. | Leave it empty (Depth − 2) or lower it. |
| `Failed to serialize volume: …` | Error | `Write to File`, `Live Preview` | The graph could not be resolved (a missing in-memory mesh buffer). | Reconnect the `Mesh → Volume` source. |
| `DualC error (…): …` | Error | `Proxy preview` | A native status other than the bounds one. | Read the code and the message. |
| `Off — connect a volume and set On to launch the viewer.` | Remark | `Live Preview` | `On` is false. | Expected. |
| `Side-car viewer running — updates live as you edit the graph. Orbit: left-drag · dolly: scroll · Esc quit.` | Remark | `Live Preview` | The window is up. | — |
| `dualc_field_view.exe not found next to the .gha — the viewer is built, never committed: run scripts/build_native.py or install the CI .yak.` | Error | `Live Preview` | The viewer exe is not beside the `.gha`. | Build it ([`native/README.md`](../../native/README.md)) or install the `.yak` CI produces, which carries it. |
| `Viewer exited immediately: …` | Error | `Live Preview` | The viewer died on launch — its stderr follows, or `the field may be unbounded — set Min/Max (a bare TPMS/plane/repeat has no finite bounds).` | Set `Min`/`Max`, or fix the graph. |
| `Failed to write graph file: …` · `Failed to launch viewer: …` | Error | `Live Preview` | A file or process error. | Read the message. |
| `No mesh connected.` · `Mesh has no vertices/faces.` | Warning · Error | `Mesh → Volume` | No or empty mesh. | Connect a mesh. |
| `/root/…: op '…' takes … child(ren), got …` and the other schema errors | Error | any terminal (the validator) | A graph DualC itself would reject. | Not reachable from the palette; report it. |
| A smooth `Boolean` looks identical to a hard one | — | — | A hard op is selected, or `k` is tiny next to the model. | Select a Smooth op; scale `k` to the model; raise `Depth` if the blend is still faint. |

---

← Back to the [command reference](README.md) · the [docs index](../README.md)
