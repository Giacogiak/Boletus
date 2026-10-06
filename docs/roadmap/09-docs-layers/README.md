# Docs layers — the documentation restructuring

**DONE** (2026-09-21): Phases 0–5, one day; the first `/docs-semantic-lint` run recorded under § Record.
**DONE** (2026-10-02): two post-close children, not phases — the gate on a second machine
(#29, [08](08-gate-portability.md)) and the loss audit (#30, [09](09-loss-audit.md)); the
roadmap snapshot's five-row window, D-43.
**DONE** (2026-10-03): a third post-close child — the full gate on Linux, the C ABI built for
it and vendored (#31, [10](10-linux-native.md)).

The record of the **2026-09-21 documentation restructuring**: Boletus adopts the layered docs
architecture DualC built in its roadmap block 19 — an entry file (`AGENTS.md`, `CLAUDE.md` =
`@AGENTS.md`), a mutable `STRUCTURE.md`, and `docs/` split into `design/` (how it is),
`decisions/` (what was decided), `roadmap/` (how it got here), `command_reference/` (how to use
it) and `raw/` (immutable inputs), enforced by the ported gate `scripts/check.py`. The plan is
immutable and lives in
[`docs/raw/2026-09-21-docs-restructuring-plan.md`](../../raw/2026-09-21-docs-restructuring-plan.md);
the advice it follows is
[`docs/raw/2026-09-21-docs-architecture-principle.md`](../../raw/2026-09-21-docs-architecture-principle.md).
This README is where status lives — the item list, the phase table and the DualC handoff table;
the dated entry each phase closes with, and its evidence, is in the numbered record children
indexed under § Record.

Two rules from the plan apply to every phase:

- **Resolution rule.** No conflict is settled by preferring one document over another.
  Present-tense facts are checked against the implementation (`src/`, `tests/`,
  `native/README.md`); historical values against git at that commit. Where git cannot decide,
  the line becomes `**Reconstructed (<date>) from commit <hash>**` or
  `DRIFT-PENDING: <what is owed>` — never a guess.
- **Close of a phase.** `python scripts/check.py --docs` green (red only between Phases 1 and 2,
  by design), `dotnet test` count unchanged (the proof no code was edited to match the docs), a
  commit, and a dated entry in § Record below.

## Decisions taken with the owner

Taken with the owner before Phase 0; each is a row of the decisions index —
[D-33 to D-36](../../decisions/01-settled.md) — from Phase 3 on.

1. The usage folder keeps DualC's name, **`docs/command_reference/`**, with one page per
   component family and an Inputs / Outputs table in place of a flag table.
2. **The roadmap index `roadmap.md` becomes `docs/roadmap/README.md`** (Phase 2); DualC's citation of the old name
   is reported in § DualC handoff, not fixed in DualC.
3. **`scripts/check.py` is ported from DualC**, same name and flags, so the owner's
   `/repo-docs-lifecycle` and `/docs-semantic-lint` workflows run here unmodified.
4. **Five phases**, one session each; `src/` and `tests/` behaviour untouched.

## Items

One tracked item per deliverable, **#21–#28 assigned at birth** (#29–#30 post-close, 2026-10-02; #31, 2026-10-03) (the numbers continue from
[08](../08-strut-lattices.md)'s #20).

| Item | Deliverable | Phase | Done when | Status |
| --- | --- | --- | --- | --- |
| #21 | `scripts/check.py` ported + fixtures + `Stop` hook + pre-commit | 1 | `--selftest` passes; `--docs` under 3 s | **DONE** (2026-09-21 — 26 fixture runs, `--fast` 0.56 s; [02](02-record-phase-1.md)) |
| #22 | The `dualc-links` check | 1 | every cited DualC path (docs and `.cs` comments) resolves against `D:\DualC` | **DONE** (2026-09-21 — reports the 6 stale citations Phase 4 fixes; [02](02-record-phase-1.md)) |
| #23 | `AGENTS.md` ≤ 80 lines, `CLAUDE.md` = `@AGENTS.md`, `docs/README.md` ≤ 60 | 2 | `root-entry`, `claude-md` green | **DONE** (2026-09-21 — 74 / 1 / 63 lines; [03](03-record-phase-2.md)) |
| #24 | `STRUCTURE.md` rewritten, root `README.md` trimmed, `roadmap.md` → `README.md` | 2 | `structure`, `indexes` green; no status or dates in the map | **DONE** (2026-09-21 — 12.7 KB map, 0 broken links; [03](03-record-phase-2.md)) |
| #25 | `docs/design/` 01–08 | 3 | `design-no-history` green; every drifted count or version has one home | **DONE** (2026-09-21 — README + 8 pages, 0 history markers; [04](04-record-phase-3.md)) |
| #26 | `docs/decisions/README.md` | 3 | `decisions-index` green; every DEFERRED row names a trigger | **DONE** (2026-09-21 — README + `01-settled.md`, D-01…D-40; [04](04-record-phase-3.md)) |
| #27 | `command_reference/` + roadmap consolidation + the DualC link sweep | 4 | `size_baseline` empty; `status-sync`, `stale-paths`, `dualc-links` green; `components.md` gone; handoff table filled | **DONE** (2026-09-21 — README + 7 pages, `size_baseline` empty, `dualc-links` 0 missing, 6 anchored status rows; [05](05-record-phase-4.md)) |
| #28 | First `/docs-semantic-lint` run recorded | 5 | run file under `0X-semantic-lint/` | **DONE** (2026-09-21 — [06/01](06-semantic-lint/01-2026-09-21-first-run.md): (a) 5 · (c) 6 · (b)/(d)/(e) 0, 10 fixed in the run's commit; [07](07-record-phase-5.md)) |
| #29 | The gate runs on a second machine (post-close, 2026-10-02) | — | `--docs --strict` green with no environment variable on Linux | **DONE** (2026-10-02 — 26/26, 0 skipped; [08](08-gate-portability.md)) |
| #30 | The loss audit: no fact lost between the base tree and the restructured one (post-close, 2026-10-02) | — | `docs_loss_audit.py` exits 0, 0 untriaged | **DONE** (2026-10-02 — 805 residual keys triaged, 4 facts restored; [09](09-loss-audit.md)) |
| #31 | The full gate on Linux (post-close, 2026-10-03) | — | `python3 scripts/check.py` green with no environment variable on Linux | **DONE** (2026-10-03 — 28 checks, 117 tests; [10](10-linux-native.md)) |

## Phases

| Phase | Sessions | Depends on | Gate proof at close | Status |
| --- | --- | --- | --- | --- |
| 0 Land the plan (`raw/`, this block) | ½ | — | commit; no gate yet | **DONE** (2026-09-21) |
| 1 The gate (#21, #22) | 1 | 0 | `--selftest` all pairs; `--fast` ≈ 1 s | **DONE** (2026-09-21 — 26/26, 0.56 s, `dotnet-test` 117; [02](02-record-phase-1.md)) |
| 2 Entry points (#23, #24) | 1 | 1 | `root-entry`, `claude-md`, `structure`, `indexes` | **DONE** (2026-09-21 — `--fast` 20 passed / 0 failed; [03](03-record-phase-2.md)) |
| 3 `design/` + `decisions/` (#25, #26) | 1–2 | 2 | `design-no-history`, `decisions-index` | **DONE** (2026-09-21 — both green; [04](04-record-phase-3.md)) |
| 4 `command_reference/` + roadmap + DualC links (#27) | 1–2 | 3 | `--docs --strict` fully green | **DONE** (2026-09-21 — 25 passed / 0 failed / 0 skipped, no report-only list; `dotnet test` 117; [05](05-record-phase-4.md)) |
| 5 Close-out + semantic lint (#28) | ½ | 4 | first lint entry recorded | **DONE** (2026-09-21 — `--docs --strict` 25 passed / 0 failed / 0 skipped, `--selftest` 26 runs, `dotnet test` 117; [07](07-record-phase-5.md)) |

Sequential, one thread per phase. Nothing in the plan touches `src/` or `tests/` behaviour;
the `.cs` edits of Phase 4 are comment-only.

## DualC handoff

DualC (`D:\DualC`) is read-only for this block. Its citations into Boletus that this
restructuring breaks are listed here for a DualC session to apply — DualC's own precedent
(its plan § 7, decision 3: paths are reported to the sibling, not kept alive). Filled at the
close of Phase 4 (2026-09-21) from a sweep of every `Boletus` path under DualC's `docs/` and
`capi/`; the rows not listed (`15:24`, `15:25`, `15:29`, `15:163` — roadmap 03, 04, 06, 01) still
resolve.

| DualC file:line | Cites today | Target after this block |
| --- | --- | --- |
| `docs/roadmap/15-boletus-handoff.md:162` | `D:\Boletus\docs\roadmap\roadmap.md` (a `file://` link) | `docs/roadmap/README.md` (renamed in Phase 2, decision D-34) |
| `docs/roadmap/15-boletus-handoff.md:26` | the pre-split roadmap page `05-phase3-grasshopper-components.md`, status **IN PROGRESS** | `docs/roadmap/05-phase3-grasshopper-components/README.md` (a folder since Phase 4); the status is PARTIAL on that page |
| `docs/roadmap/15-boletus-handoff.md:28` | the pre-split roadmap page `07-upstream-coordination.md` § 5, status **PLANNED** | `docs/roadmap/07-upstream-coordination/02-viewer-and-uniform-push.md#5-dualc_field_view-viewer-binary--gates-the-raymarch-preview-phase`; DONE 2026-07-03 |
| `docs/roadmap/15-boletus-handoff.md:164` | the pre-split roadmap page `07-upstream-coordination.md` | `docs/roadmap/07-upstream-coordination/README.md` |
| `docs/roadmap/12-field-graph-and-app/03-raymarch-app.md:106` | the same page by relative link (`../../../Boletus/docs/roadmap/07-…`), § 6 | `../../../Boletus/docs/roadmap/07-upstream-coordination/02-viewer-and-uniform-push.md#6-real-time-parameter-push-uniform-ipc-channel--deferred-performance-optimization` |
| `scripts/hooks/pre-commit:6`, `.claude/settings.json:8` (added 2026-10-02, [08](08-gate-portability.md)) | `python`, which a Linux machine with only `python3` does not have; the hooks then do not run | `$(command -v python3 \|\| command -v python)`, as Boletus's hooks now call it — **applied in DualC `c119ac3` (2026-10-02)**, with a dated note in its 17/09/03 § Enabling the hook |
| `capi/CMakeLists.txt:29` (added 2026-10-03, [10](10-linux-native.md)) | `POSITION_INDEPENDENT_CODE ON` on the `dualc_capi` target only; the static `dualc` and example libraries it links are not PIC, so `libdualc_capi.so` fails to link on Linux | PIC on every static library the C ABI links (or `CMAKE_POSITION_INDEPENDENT_CODE` when `DUALC_BUILD_C_ABI` is on); Boletus passes `-DCMAKE_POSITION_INDEPENDENT_CODE=ON` until then |

## Record

One dated entry per phase, with its evidence, in numbered children of this folder (this README
stays the status surface and under the cap):

| Phase | Record | Closed |
| --- | --- | --- |
| 0 — land the plan | [01-record-phase-0.md](01-record-phase-0.md) | 2026-09-21 |
| 1 — the gate | [02-record-phase-1.md](02-record-phase-1.md) | 2026-09-21 |
| 2 — entry points | [03-record-phase-2.md](03-record-phase-2.md) | 2026-09-21 |
| 3 — `design/` + `decisions/` | [04-record-phase-3.md](04-record-phase-3.md) | 2026-09-21 |
| 4 — `command_reference/` + roadmap + DualC links | [05-record-phase-4.md](05-record-phase-4.md) | 2026-09-21 (commits `c4a1bfc`, `1c8020a`) |
| the semantic-lint runs (item #28 and every run after it) | [06-semantic-lint/](06-semantic-lint/README.md) — one dated child per run | 2026-10-05 (the first and its same-day verification on 2026-09-21, then [06/03](06-semantic-lint/03-2026-10-03-linux-port-run.md) after the Linux port and [06/04](06-semantic-lint/04-2026-10-03-broader-primitive-run.md) after the broader `Primitive` set — the latter's twelve findings applied by the Phase C session of 2026-10-05 — [06/05](06-semantic-lint/05-2026-10-05-phase-c-run.md) after Phase C, and [06/06](06-semantic-lint/06-2026-10-05-contour-diagnostics-run.md) the same day after the contour diagnostics binding — six runs) |
| 5 — close-out + the first semantic lint | [07-record-phase-5.md](07-record-phase-5.md) | 2026-09-21 (commits `fadc563`, `ee64c2d` and the evidence correction after them) |
| post-close — the gate on a second machine (#29) | [08-gate-portability.md](08-gate-portability.md) | 2026-10-02 |
| post-close — the loss audit (#30) | [09-loss-audit.md](09-loss-audit.md) | 2026-10-02 |
| post-close — the full gate on Linux (#31) | [10-linux-native.md](10-linux-native.md) | 2026-10-03 |

## What the block leaves

- **The plan's end state holds.** The tree matches the plan's target layout — `AGENTS.md` +
  `CLAUDE.md` = `@AGENTS.md`, `STRUCTURE.md` without status clauses, `design/` 01–08 + README,
  `decisions/` two tables, `roadmap/` 00–09 with 05 and 07 as folders, `command_reference/`
  00–06, `raw/` with the plan, the principle, the `CLAUDE.md` snapshot and the test-run dump;
  `size_baseline` is empty, `docs/raw/` the only exempt tree, every maintained page under the
  cap. Items #21–#28 are all DONE; **this block's status cell becomes DONE.**
- **To ordinary sessions, not to a phase:** the monthly `/docs-semantic-lint` (next due by
  2026-10-21, or after the first session that edits both `src/` and `docs/design/` — principle 1
  of `AGENTS.md`), its run the next child of [06](06-semantic-lint/README.md) after the same-day verification run 06/02; the four code-comment
  observations the first run handed on ([06/01](06-semantic-lint/01-2026-09-21-first-run.md)
  § Counts), a code session's call; the broader `Primitive` set, **NEXT** on the
  [roadmap index](../README.md#next-up).
- **Handed on at the post-close children (2026-10-02):** to a code session, the `Info`
  hover text of `WriteToFileComponent.cs` that still mentions "a BUSY note" while BUSY is a
  Warning ([09/09](09-loss-audit.md)) *(fixed 2026-10-05, in the commit of
  [06/05](06-semantic-lint/05-2026-10-05-phase-c-run.md))*; to the next `/docs-semantic-lint` run's check (e),
  DualC's writers gained cancel and progress (its #48, commit `bc05b72`, 2026-09-22) — whether
  that is the upstream callback D-30 waits on is read there, not assumed here. *(Read the same
  day with the owner: it is; D-30 is PLANNED, [07 § 7](../07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c).)*
  To a DualC session: on an unbuilt checkout (Linux, 2026-10-02) DualC's `vendoring` check
  fails on `THIRD_PARTY.md`'s `data/bunny.obj`, a file the build generates, so its hook
  blocks every commit there. DualC `c119ac3` (the hooks fix) was committed with
  `--no-verify` for that reason.
- **To a DualC session:** the five rows of § DualC handoff, unchanged since Phase 4 — DualC
  is read-only for this block and its citations into the pre-split pages stay reported, not
  fixed.
- **To the owner, outside the repo:** the `/docs-semantic-lint` command's "The run file"
  paragraph reads the folder from the repo's `scripts/check_data.json`
  (`semantic_lint_runs.folder`, the `semantic-lint-runs` check) instead of naming DualC's —
  decision [D-41](../../decisions/01-settled.md): the procedure is the owner's one file, the
  folder is the repo's fact; DualC gains the same key in a DualC session.
- **Deferred with a trigger, measured at close:** the FTS5 search index, D-38 — `docs/raw/` is
  5 text files, 53 KB, against 50 files / 1 MB; the `component-io-table` gate check, D-39 —
  no command-reference default has been found wrong against the source; both rows in the
  [decisions index](../../decisions/README.md).

---

← Back to the [Roadmap index](../README.md).
