# `docs/raw/` — immutable inputs

Files here are **inputs, not documentation**: plans as they were drafted, imported chat text,
snapshots taken before a rewrite, run dumps kept as evidence. **They are never edited** — a
correction is a new dated file beside the original — and they are exempt from the size contract
(`frozen_prefixes` in `scripts/check_data.json`; the gate still reads their links and anchors).
Compiled, maintained text lives in the other `docs/` folders; the record of what was done with
these inputs is [roadmap 09](../roadmap/09-docs-layers/README.md).

| File | What it is | Date | Why it is here |
| --- | --- | --- | --- |
| [2026-09-21-docs-architecture-principle.md](2026-09-21-docs-architecture-principle.md) | The owner's "Each element, one job" statement of the layered docs architecture, as pasted into the planning session. | 2026-09-21 | The advice the restructuring follows; the evidence must be in the repo, not in a chat. |
| [2026-09-21-docs-restructuring-plan.md](2026-09-21-docs-restructuring-plan.md) | The five-phase plan that ports DualC's docs architecture and gate to Boletus, with the four decisions of 2026-09-21. | 2026-09-21 | The plan roadmap 09 executes; status lives in 09, never here. |
| [2026-09-21-claude-md-before-phase-2.md](2026-09-21-claude-md-before-phase-2.md) | `CLAUDE.md` as it stood before roadmap 09 Phase 2 made it the one-line `@AGENTS.md` import: the status narrative, commands, architecture, gotchas and DualC pointers, verbatim. | 2026-09-21 | The source Phase 3 harvests `docs/design/` from; the roadmap README already holds its status half. |
| [2026-09-21-test-run-117.txt](2026-09-21-test-run-117.txt) | The console output of `dotnet test tests/Boletus.Core.Tests/Boletus.Core.Tests.csproj -c Debug` at the close of roadmap 09, Phase 5 (HEAD `663f2d2` + the docs edits of that session): 117 passed, 0 failed, 0 skipped. | 2026-09-21 | The evidence the plan asked for that no code was edited to match the docs across the five phases; the floor the gate asserts is `tests_expected_min` in `scripts/check_data.json`. |
| [2026-10-02-loss-audit-c85c04e-cb138ad.txt](2026-10-02-loss-audit-c85c04e-cb138ad.txt) | The report of `scripts/docs_loss_audit.py`: the base `c85c04e` (before roadmap 09) against `cb138ad`, every residual unit with its verdict; 0 untriaged. | 2026-10-02 | The evidence of the loss audit, [roadmap 09/09](../roadmap/09-docs-layers/09-loss-audit.md); excluded from the audit's own corpus. |
| [2026-10-06-public-delivery-plan.md](2026-10-06-public-delivery-plan.md) | The plan for taking Boletus public as the owner approved it: the fresh root on DualC's procedure, the DualC submodule in place of the vendored binaries, CI with the `.yak` artifact, the docs rectification, the two-repo restore guide. | 2026-10-06 | The plan roadmap 10 executes; status lives in 10, never here. |

*Import normalization (2026-09-21, before the first commit of the `CLAUDE.md` snapshot):* its
eight relative links were written from the repo root and were re-rooted with `../../` so the
gate's `links` check reads them as the links they were, and the one to the roadmap index
names the file it became (`README.md`); nothing else was touched. *(2026-09-21, roadmap 09
Phase 4: three of those URLs were retargeted the same way when roadmap 05 and 07 became
folders — the link text and every other byte unchanged.)*
