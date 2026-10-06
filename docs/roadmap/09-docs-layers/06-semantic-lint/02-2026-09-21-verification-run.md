# Semantic lint — verification run

Run on 2026-09-21, the second run of [`/docs-semantic-lint`](README.md) on Boletus, the same
day as the [first](01-2026-09-21-first-run.md): the first run's read of all nine design pages
against the code stands at `663f2d2`, so this run does not repeat it. It reads what the first
run created and what landed after it — the five design passages the fixes rewrote, the five
dated notes on the record, the Phase 5 record, the two indexes, the decisions rows — and
confirms the classes whose inputs did not move. The command was invoked unmodified, and for
the first time its run-file paragraph resolved the folder from `scripts/check_data.json`
(`semantic_lint_runs.folder`, D-41) rather than naming DualC's. Docs and code line numbers
are as read at HEAD `ee91a5a`. The gate was green before the read (`check.py --docs`, 26
checks, 26 passed, 0 failed, `dualc-links` 0 missing). At close, `--docs --strict` is 25
passed, 1 failed: `dualc-links` reports 6 missing, every one a build output under DualC's
`build/` tree — the DLL and the two executables that `native/README.md:18,43` and four test
files cite as the vendoring provenance. The citing lines did not change between the two
readings; what changed is the DualC checkout: at close its `build/` holds a CMake configure
with every entry timestamped 18:41–18:42 and no `Release/` output. The DualC checkout's
state, not a Boletus docs defect; the other 25 checks pass.

**Inputs.** Last code change: `c4a1bfc` (2026-09-21) — unchanged since the first run;
`git log c4a1bfc..HEAD -- src tests native` is empty. Last design change: `ee64c2d`
(2026-09-21, `design/README.md`); pages 02, 04 and 05 at `fadc563`, 01 at `7e7c95f`, 03 and
06–08 at `990776c`. `doc-lag`: code 0 commits / 0.0 days ahead of `docs/`.

**Pages read for (a):** the five passages the first run's fixes rewrote, against the code
each names — `design/02:42-44` against `src/Boletus.Core/FieldGraph/FieldGraphSerializer.cs:62-66`
(`in` appended only when `node.Children.Count > 0`); `design/04:87-89` against
`src/Boletus.Grasshopper/ProxyPreviewComponent.cs:125` and `WriteToFileComponent.cs:332`
(the two strings) and `docs/command_reference/00-shared-behaviour.md:102-103` (both quoted,
verbatim); `design/04:149-150` against `LivePreviewComponent.cs:44-46` (`_onProcessExit`
calls `KillViewer()` only) and `:241-248` (`RemovedFromDocument` kills, unsubscribes, deletes
`TempDir()`); `design/04:162` against `BoletusIcons.cs:20-22` (`Accent` `#C8743C`,
`AccentDark` `#9A572D`, `AccentMid` `#B06635` — both darker on every channel, so "two darker
shades" is true); `design/05:18-25` against `src/Boletus.Core/Export/ExportPlan.cs:76-99`
(empty path → `Format` domain → `Mode` domain → extension warning → the resolved path's
directory, in that order; rule 1 says the selectors are checked first, rules 2 and 3 restate
each domain without contradicting the order). Plus `design/README.md:9-12`, the sentence
added at `ee64c2d`, against this folder. All five hold; the design README's new sentence is
process, not code, and links the folder it names.

## Findings

| # | Class | Finding | Docs `file:line` | Code `file:line` | Disposition |
| --- | --- | --- | --- | --- | --- |
| — | — | none | — | — | — |

**(a) — 0 findings.** The five rewritten passages, above.

**(b) — 0 findings.** No code commit since `c4a1bfc`, the commit the first run diffed
(comment-only, 30 lines); the pages that changed since (02, 04, 05, README) are newer than
it. The remaining candidates — 03 and 07, older than `c4a1bfc` and naming files it
touched — are the first run's, read there.

**(c) — 0 findings.** The prose written since the first run: the five dated notes (roadmap
`00:69-75`, `01:123-126`, `05/01:4-7`, `07/README:70-74`, `07/02:11-12`) are each one
sentence plus links to the owning design page, as the class requires; 05's README intro was
re-pointed; the Phase 5 record ([09/07](../07-record-phase-5.md)) and the additions to the 09
README and the roadmap index are dated record and process; this folder's README describes
the lint procedure, which `design/` does not own, so it is not (c). The seed grep
(`today|currently|at present`) hits the same lines the first run dispositioned under its
finding 11 (no change, each the premise of a DEFERRED ask or a dated phase record), the new
dated notes themselves, and roadmap `01:92` (`normals` "currently ignored" — § Marshaling
facts, inside the first run's finding 7 and consistent with `design/01:55,84`). No live
restated check count: every "25 checks" is a dated evidence line, and
`09/07:94` records the 25 → 26 step. `command_reference/`: no hit, and the bounds-string
rows on `00-shared-behaviour.md:102-103` are verbatim to the source.

**(d) — 0 findings, 7 candidates.** The scan: every non-README `.md` under `docs/` but
`raw/`, inbound links counted from every page (README indexes included) except the page's
own folder README, `README.md`, `STRUCTURE.md`, `AGENTS.md` and `examples/` searched as
well. Linked: `09/02` (from `docs/README.md`), `09/04` (decisions), `09/05` and `09/07`
(the roadmap index), `05/05` (decisions, `09/05`, the first run). Exempt as born since the
first run: `09/07`, this folder's README and `06/01`. `09/01` and `09/03` have zero hits —
the first run's sentence that "the 09 records" are linked from the roadmap index,
`raw/README.md` and each other holds for 02 and 05 only, and those links are incidental
(a decision row, a milestone row). This run keeps the first run's reading: a phase record
is reached through its block's § Record table by design, the exemption the rule states for
a page the index links by design, so none of the four is a finding; the roadmap index's
2026-09-21 "Phases 0–4" milestone row (`roadmap/README.md:45`) is where a session that
wants the incidental links too would put them.

**(e) — 15 DEFERRED rows, 0 triggers met.** The rows are the first run's fifteen (D-41,
new since, is DONE, not a trigger). No code changed, so each reading stands; re-measured:
**D-38** — `docs/raw/` is 4 files plus its README, 53,739 bytes, against 50 files / 1 MB;
**D-39** — the one command-reference passage read (the bounds rows) is verbatim, no default
found wrong; **D-25** — the roadmap index's § Next up names the broader `Primitive` set
**NEXT**, no session has opened it; **D-08** / **D-37** — the header and the pinned DLL are
as the first run measured them.

**Observations on the first run, not findings** (a run file is a record; its disposition
column is the one cell a later session changes): finding 1's code cite reads
`FieldGraphSerializer.cs:309-321`; the file is 157 lines and the emission is at `:62-66`
(the code cites of findings 2–5, re-opened here, are exact). Its "fixed in this run's commit"
cells have a hash now — `fadc563`; the dated append at its end names it.

## Counts

(a) 0 · (b) 0 · (c) 0 · (d) 0 · (e) 0 — **0 findings**: the first run's five fixes hold,
nothing behind the code, nothing due. One cite correction on the first run, recorded as its
dated append.

---

← Back to the [semantic-lint runs](README.md) · the [Docs layers index](../README.md).
