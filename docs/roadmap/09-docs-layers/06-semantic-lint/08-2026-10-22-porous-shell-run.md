# Semantic-lint run 08 — after the porous shell

Run on 2026-10-22, the eighth run of [`/docs-semantic-lint`](README.md) on Boletus, owed by
the porous-shell session ([roadmap 11](../../11-porous-shell.md)): the pin at DualC `main`
with `porous`, the arity range and closed-set keys in `OpSchema`, the validator's porous
rules, `Field.Porous`, the `Porous Shell` component and its glyph, and four `design/` pages
written (`02`, `03`, and the `04` folder's families and icons children, `04` split the same
commit). Gate green before reading (`python3 scripts/check.py --docs`, 26 passed; the full
gate green with 196 tests).

| Commit | What | Date |
| --- | --- | --- |
| `4f4db6d` | last code change (`FieldGraph/`, `PorousShellComponent.cs`, `BoletusIcons.cs`, the tests) | 2026-10-22 |
| `4f4db6d` | last `docs/design/` change (`02`, `03`, `04/`) — the same commit | 2026-10-22 |
| DualC `4f02b51` | the pin — the gitlink of `external/DualC` | 2026-10-22 |

**Pages read:** every design page that names a file the commit changed — `02` (whole, against
`OpSchema.cs`, `Ops.cs`, `Field.cs`, `FieldGraphValidator.cs`), `03` § The porous source at the
terminals (against `Volume.Combine`, `ProxyPreviewComponent.cs`, `04/06`), `06` § metric by
default, `07` § The version trap, `01` § the entry-point count (the header's diff
`350cb3a..4f02b51` is six comment lines, so "all twenty" holds), `08` glossary (`Ops` /
`OpSchema`, standalone vs consolidated), `04/04` and `04/08` (23 `Bitmap` accessors counted) —
plus the new usage page `command_reference/01-sources/07-porous-shell.md`, whose message
strings were read against `PorousShellComponent.cs` and `FieldGraphValidator.cs`.

## Findings

| # | Class | Finding | Docs | Code / record | Disposition |
| --- | --- | --- | --- | --- | --- |
| 1 | (a) | "that it reaches the root through a thickening op" — `GivesThickness` reads the **direct parent** only; `onion(translate(porous(…)))` warns. | `design/02-field-graph-model.md:108` | `src/Boletus.Core/FieldGraph/FieldGraphValidator.cs:199` | fixed in this run's commit — the sentence says parent, and that a thickening op further up is not looked for |
| 2 | (b) | The files table's `OpSchema` / `ParamSpec` row named an exact child arity and no `Choices`. | `design/02-field-graph-model.md:22` | `src/Boletus.Core/FieldGraph/OpSchema.cs:49,74` | fixed in this run's commit |
| 3 | (b) | § Coverage — "the whole vocabulary `dualc_field --list` prints, in three families" — had no `porous`. | `design/02-field-graph-model.md:67-75` | `src/Boletus.Core/FieldGraph/Ops.cs:143` | fixed in this run's commit |
| 4 | (b) | The `ValidateOrThrow` error list omitted the two error kinds the session added: a text value outside its choices, a graded `porous` without control or `d1`. | `design/02-field-graph-model.md:117-119` | `FieldGraphValidator.cs:118,167` | fixed in this run's commit |
| 5 | (b) | The metric-by-default list of source components stopped at `Strut Lattice`. | `design/06-conventions.md:46` | `src/Boletus.Grasshopper/PorousShellComponent.cs`, `OpCategory.Porous` in `IsMetric` | fixed in this run's commit |
| 6 | (b) | The primitives' order cited DualC's retired `02-dualc_primitive.md`; the code comments were retargeted to `11-dualc_field/12-primitive-catalogue.md` in the same commit, the design page was not. | `design/02-field-graph-model.md:12` | `src/Boletus.Core/FieldGraph/Ops.cs:10` | fixed in this run's commit; the `command_reference/` links to the old page still resolve at the pin and stay |
| 7 | (a) | The usage page's thickness warning fired "without a thickness op above it" — the same direct-parent rule as 1. | `command_reference/01-sources/07-porous-shell.md:53-54` | `FieldGraphValidator.cs:199` | fixed in this run's commit |

**(a) — 2 findings** (1, 7), the session's own sentences, one rule described twice; fixed.
**(b) — 5 findings** (2–6): design pages the commit did not rewrite in full, behind the
code it changed; all fixed.
**(c) — 0.** The seed search over the new block 11 and the new usage page hits nothing; block
11's item bodies say what landed on a date, and the usage page's § Cost and the previews
states what the user sees and links design 03 for the mechanism.
**(d) — 0.** `design/04-grasshopper-plugin/03-wire-type.md` is linked only by its folder's
README, but it was born in this commit (the split); every other new page has an inbound link
beyond its index.
**(e) — 0.** D-08's trigger (a version bump on a vocabulary change) is not met: DualC added
`porous` without moving the version string, `0.5.2` having moved for a header comment; D-05
not met (the CI `.yak` still serves the owner's own machine); D-50 and D-51 born this session;
the rest unchanged.

---

← Back to the [semantic-lint runs](README.md) · the [Docs layers index](../README.md)
