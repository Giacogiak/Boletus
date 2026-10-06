# Boletus docs — what lives where

One folder per class of fact. Each folder's README indexes every file below it; start there.

| Folder | Holds | Genre |
| --- | --- | --- |
| [`design/`](design/README.md) | **How it is**: the layering, the `Volume` contract, the resolvers, the terminals, conventions, invariants, glossary. Rewritten freely, dated nowhere. | compiled, mutable |
| [`roadmap/`](roadmap/README.md) | **How it got here**: one numbered block per topic, tracked items, evidence, rejected approaches. Entries are appended with a date, never rewritten. | append-only record |
| [`command_reference/`](command_reference/README.md) | **How to use it**: one page per component family — inputs tables with real defaults, every dropdown's options, exact remark strings, recipes. | usage contract |
| [`decisions/`](decisions/README.md) | **What was decided**: ID, decision, status with trigger, date, where recorded. | index |
| [`raw/`](raw/README.md) | **Immutable inputs**: plans as drafted, imported text, snapshots, run dumps. Never edited; a correction is a new dated file. | exempt from every contract |

## One home per fact

| Fact class | Owner | Everywhere else |
| --- | --- | --- |
| a component's input, default, dropdown option, output, exact remark or warning, a recipe | its `command_reference/` page | link |
| a mechanism, an invariant, a limit, a unit or naming convention | [`design/`](design/README.md) | link |
| rationale, a measurement, a verification, a rejected approach | the topic's `roadmap/` block, dated | link |
| a decision (DONE by decision, DROPPED, DEFERRED + trigger) | a [`decisions/`](decisions/README.md) row + the roadmap anchor it cites | link |
| the current status of anything | [`roadmap/README.md`](roadmap/README.md) § Current focus / § Next up | link |
| a DualC fact — an op token, an ABI signature, an engine invariant, a CLI flag | DualC's own layer (`external/DualC/docs/README.md`, the submodule at the pin, says which) | link, never a copy |
| a plan, an imported text, a snapshot, a run log | `raw/`, as a new dated file | link |
| a new file, script or build target | `STRUCTURE.md` + its folder's `README.md` | one row each |

Restating a count, a version, a commit hash or a date outside its owner is the drift the gate hunts.

## Conventions

- **Numbers are IDs.** `NN-slug` files and folders, `#N` items, letter-ID recipes: assigned at
  birth, never renumbered, never reused. Headings are never rewritten (anchors are links).
  Boletus's items are `#N`; a DualC item or decision is written `DualC #N` / `DualC D-NN`.
- **A README at every level.** An index row per file; an index README is a rewritable snapshot,
  a topic file is a record that only takes dated appends.
- **Footer.** Every non-index page under `design/`, `roadmap/`, `decisions/` and
  `command_reference/` ends with `← Back to …`.
- **Size cap.** `scripts/check_data.json` `size_caps` (lines, words, bytes; whichever trips
  first) applies to every page here except under `raw/` (`frozen_prefixes`). A page that trips
  it is split into a same-numbered folder (`NN-slug/README.md` + children) — never frozen, never
  appended past the cap. Files already over when the gate landed sit in `size_baseline`, a
  ceiling that only shrinks.
- **Status words** are the legend of [`roadmap/README.md`](roadmap/README.md) and nothing else;
  never in a heading. What each check enforces: [`scripts/check.py --list`](../scripts/check.py)
  and the Phase-1 record [09/02](roadmap/09-docs-layers/02-record-phase-1.md).
- **Debt is written, never guessed.** What git cannot re-derive is marked
  `**Reconstructed (<date>) from commit <hash>**` or `DRIFT-PENDING: <owed>` (`--strict` fails on it).

## How to find things

Index + `rg`; there is deliberately no search index to keep in sync.

```bash
rg -n '#2[1-8]' docs/                             # a tracked item, wherever it is cited
rg -n 'D-0' docs/decisions docs/roadmap           # a decision by its ID, index row then record
rg -n 'DRIFT-PENDING|Reconstructed' docs          # documentation debt the record admits to
rg -n 'mem://' src docs                           # a symbol or convention, code first
rg -n -i 'normalize' docs/command_reference       # a component or input, on every page that documents it
```

The layered layout is the work of [roadmap 09](roadmap/09-docs-layers/README.md).
