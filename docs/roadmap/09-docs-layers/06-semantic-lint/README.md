# Semantic-lint runs

The dated runs of `/docs-semantic-lint` — the owner's `~/.claude/commands/docs-semantic-lint.md`,
outside the repo, the same boundary as DualC's [D-41](../../../../../DualC/docs/decisions/01-settled.md)
— the procedure that reads `docs/design/` against the code and the record against `design/`,
the meaning check the gate cannot make. Item #28 of [roadmap 09](../README.md) Phase 5 made
the first run; the cadence — after any session that edits both `src/` and `docs/design/`,
and at least monthly — is principle 1 of `AGENTS.md`. Each run is one child here, the date
in its filename and on its first body line; the findings table names `file:line` on both
sides so a run can be repeated. Fixes are session work, never part of a run; a run's
disposition column is the only cell a later session changes, as a dated append. This
folder is the one per-repo fact the command reads — `semantic_lint_runs.folder` in
`scripts/check_data.json`, kept true by the `semantic-lint-runs` check
([D-41](../../../decisions/01-settled.md)); the first run was written here by hand before that
key existed. The five
finding classes the runs count: **(a)** a design claim the code contradicts; **(b)** a
design page older than a code change to the files it names; **(c)** present-tense "how it
works" prose in `roadmap/` or `command_reference/` that belongs in `design/`; **(d)** an
orphan page no page links but its folder README; **(e)** a DEFERRED decision whose trigger
is met.

| Run | Date | Findings |
| --- | --- | --- |
| [01-2026-09-21-first-run.md](01-2026-09-21-first-run.md) | 2026-09-21 | (a) 5 · (b) 0 · (c) 6 · (d) 0 · (e) 0 — 10 fixed in the run's commit, 1 kept with its reason; four code-comment observations handed on |
| [02-2026-09-21-verification-run.md](02-2026-09-21-verification-run.md) | 2026-09-21 | (a) 0 · (b) 0 · (c) 0 · (d) 0 · (e) 0 — the first run's five fixes verified against the code, no code change since; the (d) reading of the first run kept for the Phase records; one cite correction on run 01 |
| [03-2026-10-03-linux-port-run.md](03-2026-10-03-linux-port-run.md) | 2026-10-03 | (a) 0 · (b) 3 · (c) 0 · (d) 0 · (e) 1 — owed by `f8e4a47` (code + design); three design pages behind the Linux `.so` and the samples override, D-10's trigger met in substance; one status-line observation handed on |
| [04-2026-10-03-broader-primitive-run.md](04-2026-10-03-broader-primitive-run.md) | 2026-10-03 | (a) 10 · (b) 1 · (c) 0 · (d) 0 · (e) 1 — owed by `2963349` (code + design); every design page read, five by an agent and re-verified; `02`'s files table behind the catalog, three findings converge on DualC's ABI 0.5.0, D-10 carried; all twelve handed on and applied by the Phase C session of 2026-10-05 (D-10 re-read, not resolved) |
| [05-2026-10-05-phase-c-run.md](05-2026-10-05-phase-c-run.md) | 2026-10-05 | (a) 4 · (b) 0 · (c) 0 · (d) 0 · (e) 1 — owed by `71c6fa9` (Phase C: code + seven design pages); the rewritten pages read back from the code, the other five by an agent and re-verified; the four fixed in the run's commit (one in the code), D-10 carried |
| [06-2026-10-05-contour-diagnostics-run.md](06-2026-10-05-contour-diagnostics-run.md) | 2026-10-05 | (a) 7 · (b) 0 · (c) 0 · (d) 0 · (e) 1 — owed by `2ce3d98` (the `DualcDiagnostics` binding: code + four design pages); every design page read by two agents, the rewritten four independently of the session that wrote them, every finding re-verified; one sentence of the commit's own, the glossary's spillover and five older generalizations, all fixed in the run's commit; D-10 carried |
| [07-2026-10-06-public-delivery-run.md](07-2026-10-06-public-delivery-run.md) | 2026-10-06 | (a) 3 · (b) 0 · (c) 0 · (d) 0 · (e) 0 — owed by the publication session (roadmap 10, a folder since the same day: the submodule, the build script, CI, five design pages); the five pages read against the files they name plus a sweep for the vendored era's words; one finding fixed in the code (the CLI no longer copied beside the `.gha`), two stale sentences fixed; D-10 settled, D-05 approaching but not met |

---

← Back to the [Docs layers index](../README.md) · the [Roadmap index](../../README.md).
