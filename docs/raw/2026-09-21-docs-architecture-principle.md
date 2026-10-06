# Docs architecture principle — "Each element, one job"

Imported 2026-09-21. The owner's statement of the documentation architecture DualC adopted in
its roadmap block 19 (2026-09-17 … 21) and Boletus adopts in its roadmap block 09. Pasted into
the planning session verbatim; the plan it produced is
[2026-09-21-docs-restructuring-plan.md](2026-09-21-docs-restructuring-plan.md). Never edited.

---

Each element, one job:

CLAUDE.md / AGENTS.md — the entry point every agent loads automatically. Build/test commands, the five non-negotiable principles, the reading order into docs/. Nothing else; every line here costs attention on every task.
STRUCTURE.md — the codebase map: what lives in which folder/file. Mutable, rewritten whenever the tree changes. No status, no dates.
docs/design/ — the compiled layer (Karpathy's wiki/): how the system is today. Architecture, algorithms, invariants, tolerances, conventions, glossary. Rewritten freely to stay true; never accretes history. This is what an agent reads to understand the present before touching code.
docs/decisions/README.md — one table: ID, decision, status (DONE / DROPPED / DEFERRED + trigger), date, link to where it was recorded. The thing to scan before proposing something already rejected.
docs/roadmap/ — the record: why and how it got here. Append-only, dated, with evidence. Explains the present; never describes it (that's design/'s job — link there).
docs/command_reference/ — the usage contract: every tool, flag, default, recipe, exact error message. No rationale, no history — links to design/ and roadmap/ instead.
docs/raw/ — immutable inputs (Karpathy's raw/): benchmark dumps, experiment logs, imported chats, frozen study material. Never edited; corrections go in an errata file or a new dated file beside it. Exempt from size caps.
tools/docs_lint.py (your scripts/check.py) — the mechanical enforcer, run by hook/pre-commit: index completeness, links, sizes, DRIFT-PENDING sweep, status sync, code-ahead-of-docs detection. Rules live here, not in prose the agent must remember.

The flow between them: session work → facts routed by class (usage → command_reference, current design → design, rationale/evidence → roadmap, decision → decisions row, dumps → raw) → lint. Plus a periodic semantic lint where an agent reads design/ against src/ to catch documented-but-wrong.
