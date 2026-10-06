# Record — Phase 2 (entry points)

**DONE (2026-09-21).** Delivered: `AGENTS.md` (74 lines — what Boletus is, build/test/gate,
the five principles with the component-reference wording of principle 4 and the "a DualC fact
is linked, never restated" clause, the reading order with a seventh step into DualC's own
layers); `CLAUDE.md` = `@AGENTS.md`, its previous 189 lines snapshotted verbatim to
`docs/raw/2026-09-21-claude-md-before-phase-2.md` for Phase 3 to harvest; `docs/README.md`
(63 lines: the folder table, the fact-class → owner table with the DualC-fact row, the
conventions, the `rg` recipes); the root `README.md` rewritten without its status blockquote,
layout tree and roadmap table (each now one link), without restated counts, and with the
`file:///D:/DualC` URI turned into prose; `STRUCTURE.md` rewritten as one annotated tree
(every tracked file named, the thirteen missing ones added, `ContourExportComponent.cs`
removed, `docs/` one line per folder, no status, date or count anywhere; 12.7 KB);
the roadmap index `roadmap.md` → `docs/roadmap/README.md` by `git mv`, every in-repo link retargeted; and the
phantom-path sweep — `native/README.md` no longer promises `scripts/update-native.ps1` or a
startup version assert, and roadmap [02](../02-dependency-strategy.md) carries a dated
follow-up naming the four guardrails that were promised and never built (their decision rows
come in Phase 3).

**Evidence at close.** `check.py --fast`: 25 checks — **20 passed, 0 failed, 2 skipped**
(`decisions-index`, `design-no-history` await Phase 3); `root-entry`, `claude-md`,
`structure`, `indexes`, `footer`, `status-vocab` all green for the first time. Report-only
lists left for Phase 4: `dualc-links` 5 (the `.cs` comment citations of the pre-split
`11-dualc_field` page), `usage-in-record` 2 (the Write-to-File I/O tables in `05`),
`index-staleness` (the rename itself). `links`: 0 broken. The commit passed through the
pre-commit hook. `dotnet test`: unchanged (no `.cs` file touched this phase).

**Note (2026-09-21, same session).** `status-sync` reads "0 anchored rows checked, 25
unverifiable": it is vacuous, not green — no Status-cell row in either status table links to
a heading anchor, so the first-body-line comparison never runs. Phase 4 adds `§` anchors to
the index rows (DualC's own fix, its Phase 5 step 9) so the check has something to compare.
`--docs --strict` at this close: 21 passed, 2 failed (`usage-in-record`, `dualc-links` — both
Phase 4 work), 2 skipped (Phase 3); `doc-lag` and `drift-pending` clean.

**Deviations from the plan.** The three "reference"/"ongoing" status cells in the roadmap
index became legend words (`DONE (living reference)`, `DONE (decision record)`, `PARTIAL`)
because `status-vocab` reads every Status column; the plan had not listed them. The
`design/`, `decisions/` and `command_reference/` rows of `docs/README.md` are plain text
until their folders exist (a link to a missing README would fail `links`).

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
