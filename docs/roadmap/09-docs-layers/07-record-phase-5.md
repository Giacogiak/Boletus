# Record — Phase 5 (close-out + the first semantic lint)

The dated close-of-session entry of [roadmap 09](README.md) Phase 5, the last phase, done as
one session: the plan is
[§ Phase 5](../../raw/2026-09-21-docs-restructuring-plan.md#phase-5--close-out--first-semantic-lint--session)
(items 1–4) and its
[§ Verification](../../raw/2026-09-21-docs-restructuring-plan.md#verification-end-to-end-at-the-close-of-phase-5);
the hand-downs are the "Left for Phase 5" paragraph of [09/05](05-record-phase-4.md).

**DONE (2026-09-21, commit `fadc563` — this record, the run, the fixes and the index flip
together — plus the one follow-up commit that re-read the index rows of the pages the fixes
touched, closing the `index-staleness` cascade the first commit opened).**

- **Item 1 — `docs/raw/README.md` complete.** The plan, the principle text and the `CLAUDE.md`
  snapshot were already rows; the evidence row is new —
  [`2026-09-21-test-run-117.txt`](../../raw/2026-09-21-test-run-117.txt), the console output
  of `dotnet test` at HEAD `663f2d2` plus this session's docs edits: **117 passed, 0 failed,
  0 skipped**, the same count as at `c85c04e` and at every phase close, which is the plan's
  proof that no code was edited to match the docs. The two dated normalization notes on that
  README (Phase 2's re-rooting, Phase 4's retargets) stand; `raw/` is 5 text files, 53 KB.
- **Item 2 — the first `/docs-semantic-lint` run**,
  [06/01](06-semantic-lint/01-2026-09-21-first-run.md), in the new same-numbered folder
  [06-semantic-lint/](06-semantic-lint/README.md) (`index_roots` gained it; records 01–05
  were taken, so the plan's `0X` is 06). The command was invoked **unmodified**; it loads,
  and its run-file path names DualC's `19-docs-layers/08-semantic-lint/`, which Boletus has
  no block for — so the run is written by hand in the command's own anatomy, and the
  one-line parametrisation of that path is the owner's follow-up outside the repo (§ What
  the block leaves). All nine design pages read against the code they name, both inputs the
  same commit (`c4a1bfc`). **11 findings: (a) 5, (c) 6, (b)/(d)/(e) 0**, each zero with
  the measurement behind it — the 30-line comment-only diff between the design pages'
  commits and the last code change; 5 orphan candidates all linked by an index other than
  their own folder's; 15 DEFERRED triggers read, the four nearest measured (D-38's `raw/`
  size, D-25's NEXT-is-not-started, D-08's 0.4.0 bump being additive, D-37). Ten fixed in
  this commit as ordinary session work after the run: five design precisions (the leaf node
  has no `in` key; the two unbounded-field strings differ; `ProcessExit` only kills the
  viewer; three accent shades; `ExportPlan`'s check order) and five dated notes on the record
  (roadmap 00's recap reduced to a sentence, 01's note extended to three more sections,
  05/01's plan framed, 07 README § 4's contradicted ask, 07/02 § 5's stale "Today"); the
  eleventh kept with its reason. **No command-reference default was found wrong** on the
  pages read alongside, so D-39's trigger is not met. Four **code-comment observations**
  came out of (a) and are handed to a code session, nothing under `src/` changed:
  `ProxyPreviewComponent.cs` cites the deleted `ContourExportComponent` and "roadmap
  Phase 5"; `MeshBuffer.WriteObj`'s summary predates the in-memory resolver; `Volume.cs`
  calls the temp-file path "legacy"; `LivePreviewComponent.cs` names "Phase 5" / "5b".
- **Item 3 — the block's README.** #28 and the Phase 5 row DONE with evidence, the
  06 folder and this record in § Record, a "What the block leaves" section, and the first
  body line flipped to `**DONE**` in the same commit as the roadmap index's row 09 cell
  (`status-sync` reads the pair). The roadmap index: § Current focus says the restructuring
  is DONE and the broader `Primitive` set is **NEXT**; a milestone row for this close;
  § Next up renumbered as the snapshot it is; block 09's cell DONE.
- **Item 4 — memory** (`~/.claude/projects/D--Boletus/memory/`): `boletus-project.md`
  rewritten to pointers — its temp-OBJ path and nine entry points were 95 days stale and
  contradicted by design 01 / 03; `boletus-docs-architecture.md` updated to Phase 5 DONE
  with the gate gotchas kept; the two increment memories (`raymarch-sidecar-phase5.md`,
  `strut-lattice-vocabulary.md`) trimmed of their restated counts and commit to pointers;
  the `MEMORY.md` index lines with them.

**Verified at close** (the plan's end-to-end list, measured on the committed tree after both
commits — the gate reads tracked files only; the run right after `fadc563` alone reported four
`index-staleness` INFO entries, the pages the fixes touched being newer than their indexes,
which the follow-up cleared): `python scripts/check.py --selftest` — **26 fixture runs, 0
wrong**; `--docs --strict` — **25 checks, 25 passed, 0 failed, 0 skipped, no report-only
list**: `links` 689, 0 broken (2 external, 40 cross-project);
`anchors` 254 cross-file + 13 same-file, 0 unresolved; `indexes` 9 roots, 47 siblings, 0
unlinked; `sizes` 49 files, 4 exempt, 0 baselined, 0 over (this folder's README 10.7 KB,
the run 11.8 KB, roadmap 04 untouched at 15.0 KB); `heading-status` 53 files, 17
grandfathered, 0 new; `status-sync` 6 anchored rows, 0 disagree; `decisions-index` 6
entries / 19 rows; `design-no-history` 9 pages, 0 markers; `dualc-links` — the mechanical
form of the plan's DualC path-existence sweep — 109 files scanned, 101 DualC paths cited,
**0 missing**. The full `check.py`: `dotnet build` 0 warnings, `dotnet test` 117.
`git config core.hooksPath` = `scripts/hooks`. `rg -n 'components\.md|roadmap\.md' docs
README.md examples STRUCTURE.md`: outside `raw/` and 09 the hits are the child name
`07-strut-lattice-components.md` (in the D-25 row and the 08 / 07 / 05 links too) and the
dated "Files changed" rows of the 05 children. `rg -c
'117/117|e345bf3|11 entry points|d6b2808|16 glyphs' docs *.md`: only `raw/`, dated record
lines, 01's grandfathered heading and the `…-sync-d6b2808-…` anchor slugs on the two
indexes and the decisions page. **The Claude Code `Stop` hook observed firing**: with the
06 folder written and the 09 README flipped but this record not yet created, the turn was
ended; the hook ran `check.py --docs --hook`, exited 2, blocked the stop and handed the
report back — `links` 3 broken (the three links to this file), `status-sync` 1 disagree
(the index cell still PARTIAL against the flipped README), `dualc-links` INFO for the same
three — exactly the state the tree was in; the next turn continued from that report.
Nothing under `src/`, `tests/` or `native/` changed in this phase.

*(2026-09-21, after the close — the lint path, settled as D-41.)* The owner's first reading
of the "DualC-specific run path" left above was to copy the command into each repo's
`.claude/commands/`; that reverses DualC's D-41 (the procedure is the owner's workflow, not
the project's) and leaves two copies to drift, so the split is the other way round: **the
command stays one file outside every repo, and the repo owns the one fact it needs.**
`scripts/check_data.json` gains `semantic_lint_runs.folder`
(`docs/roadmap/09-docs-layers/06-semantic-lint`), and a new fast check,
`semantic-lint-runs`, keeps it true — the folder exists with a README, it is an `index_roots`
entry, every run is `NN-<date>-<slug>.md` with its date on the first body line — with a
fixture pair (`--selftest` 26 → 28 runs; the docs tier 25 → 26 checks). The command's own
edit — its "The run file" paragraph reading the folder from `check_data.json` instead of
naming DualC's — is the owner's, outside the repo; DualC gets the same key in its own
session. [Decision D-41](../../decisions/01-settled.md).

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
