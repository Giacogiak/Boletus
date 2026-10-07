# 10 — Public delivery: the repo as others build it

**DONE** — 2026-10-06, three items; the publication step itself is dated in [01 § Update](01-fresh-root.md#32-published-with-a-fresh-root-the-history-archived-offline).
The record of taking Boletus public: the fresh root and the offline archive of the history
that preceded it, DualC turned from vendored binaries into a git submodule the build
compiles, and the gate hosted as CI with one `.yak` as its artifact. Born 2026-10-06 from
the owner's decision to publish, on the model of DualC's own
`docs/roadmap/20-public-delivery/README.md` (2026-10-03); split into this folder the same day when
the page crossed the size cap, every heading verbatim in its child. Headings are frozen at
ID + title; status, date and evidence live on the body lines. The present-tense rules live
in [design 06 § Local only](../../design/06-conventions.md#local-only) and
[design 06 § Provenance by commit](../../design/06-conventions.md#provenance-by-commit); the
decisions are [D-46, D-47 and D-10](../../decisions/01-settled.md), [D-48 and
D-04](../../decisions/README.md). The plan as drafted is
[raw/2026-10-06](../../raw/2026-10-06-public-delivery-plan.md).

## Items

| File | Item | What it records |
| --- | --- | --- |
| [01-fresh-root.md](01-fresh-root.md) | #32 | The fresh root commit, the archive of the 70 pre-publication commits, the restore guide, what the old hashes mean from here on; the publication update with the root hash |
| [02-submodule.md](02-submodule.md) | #33 | DualC as the git submodule `external/DualC`; `scripts/build_native.py` in place of the vendored binaries; the csproj, test and gate changes; what was rejected; the single-file `.gha` deferred (D-48) |
| [03-ci.md](03-ci.md) | #34 | The GitHub Actions workflow and the `.yak` artifact; what the first runs found, the first green run, and the flaky Windows gate as an open item |

## How the items relate

**#32** is the legal shape of the public history; **#33** the shape of the dependency a
public repo can carry (source, not binaries); **#34** the proof that a stranger's machine
builds it, run on every push. The three landed as one preparation commit on the old `main`
and the fresh root carries that tree.

---

← Back to the [Roadmap index](../README.md).
