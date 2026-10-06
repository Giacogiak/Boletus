# Boletus — Roadmap & development record

The project's planning, decisions and development history, one numbered block per topic.
Each block links a topic file (or a same-numbered folder) where the original entries are
preserved verbatim, chronologically within the topic. For how the plugin *is* today see
[../design/](../design/README.md); for how each component is driven,
[../command_reference/](../command_reference/README.md); for every open or rejected decision
with its trigger, [../decisions/](../decisions/README.md).

**Status legend:** **DONE** · **NEXT** (the one item picked up next) · **PLANNED** ·
**DEFERRED** (with a trigger to revisit) · **DROPPED** (with the rationale). **PARTIAL**
qualifies a DONE whose remaining batches are listed in the topic file, on the body line as
`PARTIAL: DONE (dates); the rest PLANNED|DEFERRED`. `scripts/check.py` rejects any other
word in a Status cell and checks a cell against the entry it links.

This page is an **index and a current-state snapshot**, not a record: every claim is one line
plus a link to the topic file that holds the evidence. Detail belongs there, never here; a
count, a version or a commit hash is stated only in its home — the test floor in the gate's
`check_data.json`, the DLL commit in [`native/README.md`](../../native/README.md), the entry
points in [design 01](../design/01-native-interop.md), the glyphs in
[design 04](../design/04-grasshopper-plugin.md#the-icons). **Recent milestones** is a rolling
window of the last five rows — an older row lives in the block record it linked — so the page
stays under the cap by rule, not by baseline ([D-43](../decisions/01-settled.md)).

## Current focus

**Boletus is public** ([10](10-public-delivery.md), 2026-10-06): the repository at
`https://github.com/Giacogiak/Boletus` under MIT, with a fresh root commit and the
pre-publication history archived offline on DualC's procedure (#32, D-46); the vendored
binaries replaced by the DualC git submodule `external/DualC` that `scripts/build_native.py`
compiles into the gitignored `native/<rid>/` (#33, D-47 — the pin is the gitlink, so the
Windows DLL is no longer behind it); and the gate hosted as GitHub Actions on Ubuntu and
Windows, the Windows job packaging one `.yak` artifact (#34). The pre-publication record
below stands as written; its commit hashes name the archive.

**The documentation restructuring** ([09](09-docs-layers/README.md)) — the docs split into
layers on DualC's model, the gate green at every close, the first two `/docs-semantic-lint` runs
recorded ([09/06](09-docs-layers/06-semantic-lint/README.md), the second a same-day verification of the first's fixes) — is **DONE** (2026-09-21, Phases 0–5); the lint runs monthly from here (into the folder `scripts/check_data.json` names — D-41) and after any session that
edits both `src/` and `docs/design/`. Two post-close children (2026-10-02) made the gate
portable to a second machine and audited the restructuring for lost facts
([09/08](09-docs-layers/08-gate-portability.md), [09/09](09-docs-layers/09-loss-audit.md)); a
third (2026-10-03) made Linux a build-and-test platform, the full gate green there
([09/10](09-docs-layers/10-linux-native.md), D-44). The fourth lint run
([09/06](09-docs-layers/06-semantic-lint/README.md), 2026-10-03, after the broader-`Primitive`
session) handed twelve findings on; the 2026-10-05 session applied all twelve (the
design-prose fixes, and the three that converged on DualC's ABI 0.5.0 with the re-vendor), so
the fifth and sixth runs ([09/06](09-docs-layers/06-semantic-lint/README.md), both 2026-10-05, after Phase C and after the contour diagnostics) found four, then seven design sentences behind the code, each fixed the same day; the seventh ([09/06](09-docs-layers/06-semantic-lint/README.md), 2026-10-06, after the publication) found three, one of them in the code; the next is due by 2026-11-06. **Block 05 is DONE** (2026-10-05): the broader
`Primitive` set closed increment 3b.3 on 2026-10-03
([05/08](05-phase3-grasshopper-components/08-broader-primitive-set.md)), and `Write to File`
Phase C — a `Cancel ■` button that stops a write at the engine's next checkpoint and the
engine's percentage in the label — landed on 2026-10-05 over DualC ABI 0.5.0
([05/06 § Phase C](05-phase3-grasshopper-components/06-write-to-file.md#phase-c--cooperative-cancel-and-a-real-percentage),
D-30 settled); the same day the 0.4.0 diagnostics twins were bound, so `Proxy preview` warns
instead of drawing the engine's placeholder triangle when a contour is empty, and
`Write to File` after such a write
([05/09](05-phase3-grasshopper-components/09-contour-diagnostics.md), D-37 settled). **The pin moved to DualC `2fcd19f`**: the Linux library was rebuilt from it
while the Windows DLL and the viewer trailed it, waiting for a Windows machine — a gap the
submodule closed on 2026-10-06, both platforms building from the gitlink in CI; the plugin
still detects an older DLL at runtime and keeps its Phase-A path
([07 § 7](07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c)).
What the pinned DualC offers that Boletus does not yet use is inventoried in
[07 § 9](07-upstream-coordination/03-export-callback-and-strut-sync.md#9-dualc-at-the-pin--what-2fcd19f-offers-that-boletus-does-not-use).
Every value pillar of the
MVP — a TPMS or strut lattice, clipped and shelled, previewed coarse in the viewport or
exactly in the GPU side-car, exported as a tiled STL or a 3MF, cancellable — shipped between 2026-06-18
and 2026-10-05 ([05](05-phase3-grasshopper-components/README.md),
[07 § 5](07-upstream-coordination/02-viewer-and-uniform-push.md#5-dualc_field_view-viewer-binary--gates-the-raymarch-preview-phase)),
Core-tested; the Grasshopper paths have no automated coverage and several increments —
the primitive components and Phase C among them — still list their manual Rhino smoke test as
pending in their records.

**Recent milestones** — date, one clause, the record:

| Date | What landed | Record |
| --- | --- | --- |
| 2026-10-06 | **Boletus is public** — a fresh root commit, the history archived offline on DualC's procedure, MIT + `CITATION.cff`; the local-only rule reversed (D-46). | [10 #32](10-public-delivery.md#32-published-with-a-fresh-root-the-history-archived-offline) |
| 2026-10-06 | **DualC as a submodule, the gate as CI** — `external/DualC` at the pin, `build_native.py` in place of the committed binaries (D-47, D-10 settled), GitHub Actions on Ubuntu and Windows with one `.yak` artifact. | [10 #33](10-public-delivery.md#33-dualc-as-a-git-submodule--the-binaries-built-never-committed) · [10 #34](10-public-delivery.md#34-ci--the-gate-as-a-github-actions-workflow-and-one-yak) |
| 2026-10-05 | **The contour diagnostics bound** — DualC's 0.4.0 `DualcDiagnostics` behind `out` overloads and a second entry-point probe; `Proxy preview` warns and draws nothing on an empty contour, `Write to File` warns after one; D-37 settled. | [05/09](05-phase3-grasshopper-components/09-contour-diagnostics.md) · [07 § 9](07-upstream-coordination/03-export-callback-and-strut-sync.md#9-dualc-at-the-pin--what-2fcd19f-offers-that-boletus-does-not-use) |
| 2026-10-05 | **`Write to File` Phase C + the pin at DualC `2fcd19f`** — cooperative cancel and the engine's percentage over ABI 0.5.0, the Linux library rebuilt at the pin, the old-DLL fallback probed at runtime; the Windows binaries owe a rebuild. | [05/06 § Phase C](05-phase3-grasshopper-components/06-write-to-file.md#phase-c--cooperative-cancel-and-a-real-percentage) · [07 § 7](07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c) |
| 2026-10-03 | **The broader `Primitive` set** — `Segment Primitive` and `Axial Primitive` over a Core catalog, `Primitive` grown by BoxFrame and Ellipsoid; 21 shapes contoured through the native library; increment 3b.3 closed. | [05/08](05-phase3-grasshopper-components/08-broader-primitive-set.md) |

## Next up

Highest-priority first; the rationale and evidence in the linked topic file, the trigger of
every DEFERRED item in its [decisions](../decisions/README.md) row.

1. **The Rhino smoke tests on the CI `.yak`** — the primitive components
   ([05/08 § Verification](05-phase3-grasshopper-components/08-broader-primitive-set.md#verification-broader-primitive-set))
   and Phase C
   ([05/06 § Phase C](05-phase3-grasshopper-components/06-write-to-file.md#phase-c--cooperative-cancel-and-a-real-percentage)),
   the records list as pending: install the `boletus-yak` artifact of a green run on a Windows
   machine with Rhino 8 — the first time the Windows DLL runs at the pin. **NEXT.**
2. **Re-pin the submodule to a DualC `main` commit** once DualC's `main` is pushed past
   `2fcd19f` ([10 #33](10-public-delivery.md#33-dualc-as-a-git-submodule--the-binaries-built-never-committed));
   until then the gitlink resolves through DualC's `ci/gate-workflow` branch. **PLANNED.**
3. **Distribution & packaging** ([06](06-phase4-distribution-and-packaging.md)) — the `.yak`
   exists as a CI artifact; zero-prerequisite install waits on DualC's static-runtime build
   ([D-05](../decisions/README.md)); a Yak-server release is the owner's call. **PLANNED.**
4. **What the pinned DualC offers and Boletus does not bind**, each on its trigger
   ([07 § 9](07-upstream-coordination/03-export-callback-and-strut-sync.md#9-dualc-at-the-pin--what-2fcd19f-offers-that-boletus-does-not-use)):
   tiled 3MF through the ABI (D-32), a version bump per vocabulary change (D-08); and the standing asks — the
   uniform-push channel (D-27), DAG-ref serialization (D-06).
5. **Palette gaps, DEFERRED on a canvas that needs them**: `Mirror` / `Elongate` / `Repeat`
   (D-23), `Triangle` / `Quad` plates (D-45), proxy refinements past the depth ceiling (D-19);
   a single-file `.gha` with embedded natives (D-48).

Standing constraints — public source with no committed binary, MVP first, consolidated
multi-mode components except the standalone `Onion` / `Graded Onion` / `Normalize`, the
meshless `Volume` currency, tiled binary export, lattice fidelity as the side-car's job — are
settled decisions ([D-46, D-47, D-03, D-13, D-15, D-16](../decisions/01-settled.md)) whose
present-tense effect is [design 06](../design/06-conventions.md).

## Principal blocks

| # | Topic | What it covers | Status |
| --- | --- | --- | --- |
| 00 | [References & environment](00-references-and-environment.md) | Where the authoritative DualC sources live, the fixtures and binaries to test against, the dev-environment facts. | DONE — a living reference |
| 01 | [Architecture & the C-ABI contract](01-architecture-and-contract.md) | The layering, the "graph string is the API" property, the contract the wrapper targets — the Phase-1/2 record. | DONE |
| 02 | [Dependency strategy](02-dependency-strategy.md) | NuGet vs vendored DLL vs project reference; the guardrails and the migration triggers. | DONE (D-01); guardrails DEFERRED |
| 03 | [Phase 2 — `Boletus.Core` wrapper](03-phase2-core-wrapper.md#03--phase-2-boletuscore-pinvoke-wrapper) | The Rhino-free P/Invoke layer and its verified boundary. | DONE |
| 04 | [Phase 3a — field-graph model & serializer](04-phase3-field-graph-serializer.md#04--phase-3a-field-graph-model--serializer) | The managed node tree, the canonical serializer, the builders, the validator; the full op vocabulary pinned. | DONE |
| 05 | [Phase 3b — Grasshopper components](05-phase3-grasshopper-components/README.md#05--phase-3b-grasshopper-components-mvp) | The `.gha`: the plan, then every increment — the thin slice, the `Volume` palette, the diskless flip, tiled STL, the proxy, the icons, the warps, the example graph, `Write to File` and its Phase C, the strut lattices, the broader `Primitive` set. | DONE — the Rhino smoke tests pending on Windows |
| 06 | [Phase 4 — distribution & packaging](06-phase4-distribution-and-packaging.md#06--phase-4-distribution--packaging) | The local Yak and the zero-prerequisite install; the local-only constraint. | PLANNED |
| 07 | [Upstream coordination with DualC](07-upstream-coordination/README.md#07--upstream-coordination-with-dualc) | What Boletus needs from or triggers in DualC: the in-memory mesh resolver, the viewer binary and the file-watch, the uniform push, the export callback, the strut-vocabulary sync, the version trap, the inventory at the pin. | PARTIAL — per item |
| 08 | [Strut lattices](08-strut-lattices.md) | The strut-lattice node vocabulary as pinned, the `--expr` exemplars the tests round-trip, the Boletus-side implementation (#20). | DONE |
| 09 | [Docs layers](09-docs-layers/README.md#docs-layers--the-documentation-restructuring) | The 2026-09-21 documentation restructuring: the plan, items #21–#31, the phase table, the per-phase records, the post-close gate portability, loss audit and Linux build, the DualC handoff. | DONE — Phases 0–5 and three post-close children; the semantic-lint runs accumulate under it |
| 10 | [Public delivery](10-public-delivery.md#10--public-delivery-the-repo-as-others-build-it) | Taking Boletus public: the fresh root and the offline archive (#32), DualC as a git submodule the build compiles (#33), the gate as CI with one `.yak` artifact (#34). | DONE |

## How the blocks relate

**00** and **01** are the ground: where DualC's truth lives and what the C ABI promises. **02**
decided how DualC's binary reached the build in the vendored era and **10** how it does
from publication on (a submodule built by one script, pinned by its gitlink); **06** is how
the finished plugin reaches a user (the `.yak` CI builds) — two different layers. **03** and
**04** are the Rhino-free core the components stand on: the wrapper binds the ABI, the
serializer turns a tree into the one string the ABI takes. **05** is the plugin itself, every
increment dated, and **08** the one vocabulary extension large enough for its own block.
**07** is the seam with DualC — what landed upstream because Boletus asked, and what is still
asked. **09** is the documentation system these pages live in, **10** the public repository
and the CI they are published through.
