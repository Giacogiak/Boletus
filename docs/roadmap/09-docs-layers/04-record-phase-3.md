# Record — Phase 3 (`design/` + `decisions/`)

**DONE (2026-09-21).** Delivered: `docs/design/` — the index README (the layering, the
`Volume` contract, the page table, "Things we do not do") and the eight pages the plan
mapped, `01-native-interop` through `08-glossary`, harvested from the roadmap topic intros
(00, 01, 03, 04, 05, 07, 08) and the `CLAUDE.md` snapshot under `raw/`, every present-tense
claim checked against `src/`, `tests/` and `native/README.md` rather than against the old
pages; `docs/decisions/` — `README.md` (the open and rejected decisions, DEFERRED with a
trigger each, DROPPED with the rationale) and `01-settled.md` (the DONE-by-choice rows),
**D-01 to D-40**, one sequence by decision date; the `**DEFERRED**` / `**DROPPED**` status lines
the `decisions-index` gate keys on, inserted as the first body line under the six roadmap
headings whose subject *is* the decision (07 § 2, § 3, § 6, § 7; 02 § guardrails, § triggers),
each with a dated note pointing at its row; `scripts/check_data.json` (`docs/design` and
`docs/decisions` in `index_roots`, the two decisions tables in `status_vocab_only.tables`,
07's `size_baseline` raised for the four status lines with the reason in a `_comment`);
`docs/README.md`, the root `README.md`, `STRUCTURE.md` and the roadmap index with the two
folders turned into links.

**The fact homes (decision D-40).** The golden contour counts live in
[design 07](../../design/07-invariants-and-limits.md#the-golden-contour-counts) and nowhere
else in the present tense: roadmap 00's "Canonical test oracle" section is a one-line link
with a dated note (00 is the "living reference" page, so its body is rewritable; its heading
stands), and its provenance paragraph — which still named `e345bf3` — is a link to
`native/README.md`. The entry-point count is stated once, in
[design 01](../../design/01-native-interop.md), verified as eleven `[DllImport]`s in
`NativeMethods.cs`; `native/README.md` and the roadmap index link there instead of restating
it, and roadmap 01 — whose "9 entry points" lines are the Phase-1 record and stay — carries a
dated note naming the present-tense owner. The glyph count is stated once, in
[design 04](../../design/04-grasshopper-plugin.md#the-icons), verified as twenty accessors in
`BoletusIcons.cs`. The DLL commit stays in `native/README.md` and is not restated by any design
page. **The test count is on no design page**: design carries no dates, so a live count would
rot there un-datably; its one owner is the gate's floor (`dotnet.tests_expected_min`); the
roadmap index's status snapshot carries it as status until Phase 4 consolidates it, and the
dated records as history. The `117/117`, `e345bf3` and `d6b2808` hits that remain under `docs/` are the
dated status cells of the roadmap index (Phase 4 rewrites it) and dated record lines — history,
not restated present-tense facts. `WrapperTests.cs`'s comments on the counts are Phase 4's
`.cs` comment sweep.

**Evidence at close.** `check.py --docs --strict`: 25 checks — **22 passed, 2 failed, 0 skipped** — the two being the report-only lists Phase 4 owns, `usage-in-record` (2) and `dualc-links` (5 `.cs` comments; 88 files scanned, 67 DualC paths cited, none of the new design/decisions links missing); `--docs` without `--strict` PASS. `links` 405 / 0 broken; `anchors` 113 cross-file / 0 unresolved; `indexes` 5 roots / 26 siblings / 0 unlinked; `status-vocab` 65 cells / 0 off-legend; `sizes` 28 files / 0 over. `design-no-history`:
9 pages, 0 markers after one rewrite pass (one `since` caught on 04). `decisions-index`:
6 decision entries, 19 rows, 0 problems — the six entries are exactly the six status lines this
phase inserted, so the check proves those six rows and no more; the other rows (and the rows
without an anchored link, which the row count omits) were verified by reading each cited
entry, not by the gate. `dotnet test`: **117 passed, 0 failed** (no `.cs` file touched). Sizes: the
largest design page is 04 at 167 lines / 1,629 words / 11.8 KB, the smallest 05 at 61 lines; the decisions README is 46 lines /
9.3 KB and `01-settled.md` 36 lines / 8.4 KB — split from the start because forty rows in one
table would have crossed the byte cap, as DualC's thirty-four did. `rg -c '117/117|e345bf3|11 entry points|d6b2808' docs *.md`: zero hits on any design page;
the decisions pages hit only inside anchor slugs (`…-sync-d6b2808-…`); the rest are `raw/`,
dated roadmap record lines, the roadmap index's status snapshot, 01's grandfathered heading and
03's Phase-2 file table — none a present-tense restatement.

**Deviations from the plan.** (1) The plan expected the decisions index to trip the cap
before splitting; the rows were measured first and split up front (the file's
`status_vocab_only` comment anticipated it). (2) The bold status lines were added only under
the six headings whose subject is the decision; the dropped and deferred sub-decisions inside
DONE increment records (the PNG icon generator, the child-process exporter, multi-mode
`Deform`, the ROI proxy, the superseded skip-preview rule, the `mirror`/`elongate`/`repeat`
components) are rows whose links land on the increment's heading with the status carried by
the row — a `**DROPPED**` line under an increment that shipped would misdescribe it. Triggers
written here after the fact are marked †, DualC's convention. (3) Two roadmap pages the plan
did not name were edited: 01 gained a dated note (its "9 entry points" figures are the
Phase-1 record, and a known-stale present-tense reading would otherwise sit unremarked), and
the roadmap index's one restated entry-point count became a link — the index is a rewritable
snapshot, and leaving a second home for Phase 4 to clean would have failed the close check.
(4) Decision D-37 (re-vendoring at DualC ABI 0.4.0) has no roadmap entry of its own; this
record is where it is taken. (5) The `Mesh → Volume` metric-by-default decision (D-24) has
no roadmap entry either — it landed in commit `88b8c9d` and was recorded only in
`components.md` — so its row cites the commit and the design page.

**Left for Phase 4.** The five stale `.cs` comment citations of the pre-split `11-dualc_field`
page (`dualc-links`), the two Write-to-File I/O tables in 05 (`usage-in-record`), the
`size_baseline` entries for 04, 05, 07 and `components.md`, the roadmap index's status
narrative, and `status-sync`'s anchors.

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
