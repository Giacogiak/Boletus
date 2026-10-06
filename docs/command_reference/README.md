# Boletus — Command reference

The usage contract of the Grasshopper palette: one page per component family, each
component with its inputs table (the real defaults, read from the source), its outputs, every
dropdown's full option set, the exact remark, warning and error strings it posts, and
letter-numbered recipes. What every component does the same way — the `Volume` wire, the
Normalize rule, bounds, the message levels, the troubleshooting table — is
[00-shared-behaviour.md](00-shared-behaviour.md). *Why* a component behaves as it does is the
design layer ([`docs/design/`](../design/README.md)); how it came to be is the roadmap
([`docs/roadmap/`](../roadmap/README.md)); an engine fact — an op token, a parameter default,
an invariant — is DualC's and is linked, never restated.

Once the `.gha` is loaded — from the `.yak` CI builds or the four-file folder the root
`README.md` § Build & test describes — every component sits on the **Boletus** tab in four sub-panels —
**Sources**, **Decorators**, **Booleans**, **Terminals** — and a typical canvas reads left to
right: one or more Sources → Decorators / Booleans → a Terminal.

## The families

| # | Family | Components | Page |
| --- | --- | --- | --- |
| 0 | Shared behaviour | the `Volume` wire, the Normalize rule, bounds, messages, troubleshooting | [00-shared-behaviour.md](00-shared-behaviour.md) |
| 1 | Sources | `TPMS`, `Primitive`, `Segment Primitive`, `Axial Primitive`, `Mesh → Volume`, `Strut Lattice` | [01-sources.md](01-sources.md) |
| 2 | Decorators | `Normalize`, `Onion`, `Graded Onion`, `Transform`, `Offset`, `Twist`, `Bend`, `Displace`, `Graded Offset`, `Mix` | [02-decorators.md](02-decorators.md) |
| 3 | Booleans | `Boolean` | [03-booleans.md](03-booleans.md) |
| 4 | Terminals | `Write to File`, `Proxy preview`, `Live Preview` | [04-terminals.md](04-terminals.md) |
| 5 | Field-graph ops | the `Field` builder ↔ DualC op-token table (what a canvas can emit through Core) | [05-field-graph-ops.md](05-field-graph-ops.md) |
| 6 | Worked examples | a clipped sheet lattice, a printable part, a strut-lattice infill, `examples/demo.gh` | [06-worked-examples.md](06-worked-examples.md) |

## Where a task is documented

| Task | Components | Where |
| --- | --- | --- |
| Make a sheet lattice with a real wall thickness | `TPMS → Normalize → Onion(Boundary)` | [01 § TPMS](01-sources.md#tpms) · [02 § Onion](02-decorators.md#onion) · [06 W1](06-worked-examples.md#w1--a-clipped-sheet-lattice) |
| Crop a lattice without hollowing it (a network solid) | `Boolean` (Intersection) | [03 § Boolean](03-booleans.md#boolean) |
| Bring a Rhino mesh into the graph as a clip body or operand | `Mesh → Volume` | [01 § Mesh → Volume](01-sources.md#mesh--volume) |
| An analytic solid — a clip box, a strut between two points, a cone or prism | `Primitive`, `Segment Primitive`, `Axial Primitive` | [01 § Primitive](01-sources.md#primitive) · [§ Segment](01-sources.md#segment-primitive) · [§ Axial](01-sources.md#axial-primitive) |
| A strut (beam) lattice, tapered or graded | `Strut Lattice`, `Graded Offset`, `Mix` | [01 § Strut Lattice](01-sources.md#strut-lattice) · [02 § Graded Offset](02-decorators.md#graded-offset) · [06 W3](06-worked-examples.md#w3--a-strut-lattice-infill-clip--grade) |
| Vary a shell's wall thickness across the part | `Graded Onion` | [02 § Graded Onion](02-decorators.md#graded-onion) |
| Twist, bend, inflate or texture a volume | `Twist`, `Bend`, `Offset`, `Displace` | [02](02-decorators.md) |
| Export a printable STL or 3MF | `Write to File` | [04 § Write to File](04-terminals.md#write-to-file) |
| A quick coarse look in the viewport | `Proxy preview` | [04 § Proxy preview](04-terminals.md#proxy-preview) |
| An exact, GPU-raymarched preview of a dense lattice | `Live Preview` | [04 § Live Preview](04-terminals.md#live-preview) |
| Turn a lattice and its enclosing volume into one printable part | the four-step workflow | [06 W2](06-worked-examples.md#w2--a-printable-part-isolate--thicken--skin--union) |
| Read a warning or error the canvas shows | — | [00 § Troubleshooting](00-shared-behaviour.md#troubleshooting) |
| What a component emits as DualC JSON, and what the engine's defaults are | — | [05](05-field-graph-ops.md) |

## How these pages are kept true

A component, input or output created, changed or removed updates its page in the same
session, with the defaults read from the `pManager.Add*Parameter` calls in
`src/Boletus.Grasshopper/*Component.cs` and the message strings quoted from the source
([`AGENTS.md`](../../AGENTS.md), principle 4). Every dropdown's option set appears in full on
its page and in the input's own hover description
([design 06 § Every dropdown documents its full option set](../design/06-conventions.md#every-dropdown-documents-its-full-option-set)).
The gate that would cross-check the tables against the source is a deferred decision
([D-39](../decisions/README.md)).

---

← Back to the [docs index](../README.md).
