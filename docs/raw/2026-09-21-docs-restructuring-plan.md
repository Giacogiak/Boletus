# Boletus docs restructuring — the DualC layered architecture, ported

Drafted 2026-09-21 in the planning session, approved by the owner the same day; imported verbatim.
Status lives in [roadmap 09](../roadmap/09-docs-layers/README.md), never here. Never edited.

## Context

DualC (`D:\DualC`, **read-only for this work**) restructured its documentation on
2026-09-17…21 (its roadmap block 19) into layers, each with one job: a ≤ 80-line `AGENTS.md`
entry (`CLAUDE.md` = `@AGENTS.md`), a mutable `STRUCTURE.md` codebase map, and `docs/` split
into `design/` (how it is — rewritten freely, dated nowhere), `decisions/README.md` (one table),
`roadmap/` (append-only record), `command_reference/` (usage contract), `raw/` (immutable
inputs), all enforced by one Python gate `scripts/check.py` (26 checks, config in
`check_data.json`, fixtures, `--selftest`) wired to a pre-commit hook and a Claude Code `Stop`
hook. The *process* half lives outside the repo in the owner's `~/.claude/skills/repo-docs-lifecycle`
and `~/.claude/commands/docs-semantic-lint.md`, both of which hardcode
`python scripts/check.py --docs [--strict]`, `AGENTS.md`, `docs/README.md`, `docs/design/`,
`docs/decisions/README.md`, `docs/roadmap/README.md`.

Boletus today: 3,049 doc lines in 11 files (`docs/roadmap/00–08` + `roadmap.md` index +
`docs/components.md`) plus `CLAUDE.md` (189 lines, a status dump), `README.md`, `STRUCTURE.md`
(232 lines, stale), `native/README.md`, `examples/README.md`. No `AGENTS.md`, no `scripts/`, no
`.claude/settings.json`, no lint, no `design/`/`decisions/`/`command_reference/`/`raw/`. Facts are
restated in 3–6 places and have drifted (DLL commit `e345bf3` vs `d6b2808`, "9 entry points" vs
11, test counts 83/86 vs 117, icon count 12/16/20). Boletus last synced its DualC links at DualC
`3d73220` (2026-09-11); DualC has since deleted `docs/ARCHITECTURE.md` and
`docs/report-quality-inspection-contouring.md`, moved `docs/study/` → `docs/raw/study/`, and
tombstoned roadmap 16/18. **Every DualC path Boletus cites still resolves** (verified this
session, 23/23), but 8 `.cs` comments still name the pre-split `11-dualc_field.md` /
`02-dualc_primitive.md`, `05:40` has a malformed link, `README.md:4` uses `file:///D:/DualC`,
`00:23` cites the tombstone `18-c-abi-continued.md`, and nothing yet points at DualC's new
`design/` and `decisions/` layers (the right targets for present-state engine facts).

**Goal:** give Boletus the identical architecture and gate so the owner's two workflows run in
Boletus unmodified, route every existing fact to its one home, and re-point every DualC reference
at DualC's current layers.

## Decisions taken (2026-09-21, with the owner)

1. Usage folder is **`docs/command_reference/`** (same name as DualC, so the skill's routing table
   applies literally); anatomy adapted: one page per component family with an **Inputs / Outputs
   table** (`| Input | Type | Default | Meaning |`) in place of a flag table. Recorded in block 09.
2. **`docs/roadmap/roadmap.md` → `README.md`** (`git mv`, in Phase 2). DualC's one citation of `roadmap.md`
   (`D:\DualC\docs\roadmap\15-boletus-handoff.md:162`) is **reported to DualC as a handoff row**
   in block 09, not fixed here (DualC's precedent, its plan § 7 decision 3). No stub.
3. **Port DualC's `scripts/check.py`** (same name, same flags) — drop the cmake/ctest/parity/
   vendoring tier, add a `dotnet` build tier and a new **`dualc-links`** check.
4. **5 phases**, one session each, gate green + a commit at every close. Nothing touches `src/`
   or `tests/` behaviour (the `.cs` edits are comment-only); **`dotnet test` = 117/117 unchanged at
   every commit** is the proof — the role DualC's unchanged ctest count played.
5. Numbers are IDs: roadmap files **00–08 keep their names** (DualC links into `01`, `03`–`07` by
   name); the new block is **`09-docs-layers/`**; tracked items continue from Boletus's only
   existing item `#20` → **#21–#28 assigned at birth** (one per deliverable below); decisions get
   `D-NN` from `D-01`.

## Target tree

```
D:\Boletus\
├── AGENTS.md                      ≤ 80 lines: what Boletus is (2 lines), build/test/gate, 5 principles, reading order
├── CLAUDE.md                      "@AGENTS.md" only
├── README.md                      front page ≤ 150 lines: pitch, architecture diagram, build, one link per doc area — no counts/dates
├── STRUCTURE.md                   codebase map, file-by-file for src/ tests/ native/ examples/ scripts/; docs/ = one line per folder; no status/dates
├── scripts/
│   ├── check.py                   the gate (ported)          ├── check_data.json   its exceptions
│   ├── check_fixtures/            pass/fail trees (copied + dualc-links pair)
│   └── hooks/pre-commit           `check.py --fast`; .gitattributes pins LF
├── .claude/settings.json          Stop hook → python scripts/check.py --docs --hook
└── docs/
    ├── README.md                  ≤ 60 lines: what lives where, fact-class → owner table, conventions, how to find things
    ├── design/                    CURRENT STATE
    │   ├── README.md              layering diagram (GH → Core → dualc_capi.dll), the Volume contract, page table, "Things we do not do"
    │   ├── 01-native-interop.md   DualcField/SafeHandle/blittable structs/UTF-8 marshaling, the 11 entry points as a link to DualC capi/README, single-threaded rule
    │   ├── 02-field-graph-model.md FieldNode/FieldValue/Ops/OpSchema/Field builders/FieldGraphSerializer/Validator; metric vs non-metric; tree-only
    │   ├── 03-volume-and-resolvers.md Volume/MeshBuffer/FieldTree/VolumeResolver (mem:// → id=) / MeshMaterializingResolver (mem:// → path=)
    │   ├── 04-grasshopper-plugin.md   GH_AssemblyPriority DLL resolver, VolumeGoo/VolumeParameter, component families, ProxyPreview cap, WriteToFile threading model, LivePreview process model, icons
    │   ├── 05-export-planning.md      ExportPlan (path/extension/strategy/tile rules), tiled STL, 3MF = mm
    │   ├── 06-conventions.md          units, metric-by-default sources, Normalize rule, onion-before-clip, dropdown-doc rule, GUID rule, DLL provenance-by-commit rule
    │   ├── 07-invariants-and-limits.md golden counts (the ONE home), maxDepth≈6 rule, MaxProxyDepth=7, version trap, single-flight/BUSY, "cannot hard-cancel a native export"
    │   └── 08-glossary.md             Volume, MetricSource, mem://, proxy, side-car, tiled, `#N` = Boletus item vs "DualC #N" / "DualC D-NN"
    ├── decisions/README.md        one table: ID · decision · status (+trigger) · date · where recorded (+ 01-settled.md if the cap forces it)
    ├── roadmap/
    │   ├── README.md              (was roadmap.md) legend, Current focus, Next up, Principal blocks table, how the blocks relate
    │   ├── 00–08 …                unchanged names; entries only appended; oversize pages promoted to same-numbered folders
    │   └── 09-docs-layers/        the record of THIS restructuring: README (items #21–#28, phase table, DualC handoff table) + 01-record-phase-N.md children + 0X-semantic-lint/
    ├── command_reference/
    │   ├── README.md              palette index (family → page), the wire type, shared behaviour (Remarks/Warnings), the worked examples' index
    │   ├── 00-shared-behaviour.md Volume wire, Normalize rule, error/remark messages table (from components.md 699–713)
    │   ├── 01-sources.md          TPMS, Primitive, Mesh→Volume, Strut Lattice   (from components.md 75–…, roadmap 08 tables)
    │   ├── 02-decorators.md       Onion, Graded-onion, Normalize, Transform, Offset/Twist/Bend/Displace, Graded Offset
    │   ├── 03-booleans.md         Boolean, Mix
    │   ├── 04-terminals.md        Write to File, Proxy preview, Live Preview
    │   ├── 05-field-graph-ops.md  the Core builder ↔ DualC op token table (from roadmap 04 §"Pinned op signatures", 57–154)
    │   └── 06-worked-examples.md  the three canvases + examples/demo.gh (from components.md 586–695)
    └── raw/
        ├── README.md
        ├── 2026-09-21-docs-architecture-principle.md   the pasted "Each element, one job" text (the advice this plan follows)
        └── 2026-09-21-docs-restructuring-plan.md       this plan, verbatim, immutable
```

`docs/components.md` is **deleted** after Phase 4 (git keeps it; 09 records the section map).
`examples/README.md` and `native/README.md` stay (folder READMEs; provenance is a `raw`-like fact
that lives with the binary).

## Size end-state (cap 300 lines / 2,500 words / 15,360 B, `raw/` exempt)

| File today | Size | Phase → result |
| --- | --- | --- |
| `docs/roadmap/05-phase3-grasshopper-components.md` | 830 lines / 63 KB | 4 → `05-phase3-grasshopper-components/README.md` + children `01-volume-datatype-and-palette.md`, `02-diskless-mesh-and-tiled-stl.md`, `03-proxy-preview-and-icons.md`, `04-domain-warps-and-example-graph.md`, `05-write-to-file.md` (cut at the eight dated "Implemented …" records) |
| `docs/components.md` | 717 / 41 KB | 4 → `command_reference/00–06` (deleted) |
| `docs/roadmap/07-upstream-coordination.md` | 337 / 23 KB | 4 → `07-upstream-coordination/README.md` + `01-in-memory-mesh.md`, `02-viewer-and-uniform-push.md`, `03-export-callback-and-strut-sync.md` |
| `docs/roadmap/04-phase3-field-graph-serializer.md` | 290 / 20 KB | 4 → op tables move to `command_reference/05`; page left ≈ 150 lines |
| `STRUCTURE.md` | 232 / 27 KB | 2 → ≤ 15 KB (docs/ collapsed to one line per folder; stale rows fixed) |
| `CLAUDE.md` | 189 / 14 KB | 2 → 1 line |
| `docs/roadmap/roadmap.md` | 150 / 13 KB | 4 → README.md ≈ 100 lines (Current focus narrative → one paragraph + links) |

`size_baseline` starts with the 4 oversize docs and is **empty at the close of Phase 4**.

---

## Phase 0 — Land the plan (½ session)

1. `docs/raw/README.md` + the two immutable imports (this plan; the pasted principle text). Date
   in the filename and on the first body line, never in the H1.
2. `docs/roadmap/09-docs-layers/README.md`: intro, the item table **#21–#28** (below), the phase
   table (5 rows, all PLANNED), § DualC handoff (empty table, filled in Phase 4), footer. Add row
   09 to `roadmap.md`'s detail list (the rename happens in Phase 4, but the row lands now).
3. Commit. No gate yet — Phase 1 lands it.

| Item | Deliverable | Done when |
| --- | --- | --- |
| #21 | `scripts/check.py` ported + fixtures + `Stop` hook + pre-commit (Phase 1) | `--selftest` passes; `--docs` < 3 s |
| #22 | `dualc-links` check (Phase 1) | every cited DualC path (docs + `.cs`) stats OK against `D:\DualC` |
| #23 | `AGENTS.md` ≤ 80, `CLAUDE.md` = `@AGENTS.md`, `docs/README.md` ≤ 60 (Phase 2) | `root-entry`, `claude-md` green |
| #24 | `STRUCTURE.md` rewritten, README trimmed (Phase 2) | `structure` green; ≤ 15 KB; no status/dates |
| #25 | `docs/design/` 01–08 (Phase 3) | `design-no-history` green; every stale count/version has exactly one home |
| #26 | `docs/decisions/README.md` (Phase 3) | `decisions-index` green; every DEFERRED row names a trigger |
| #27 | `command_reference/` + roadmap consolidation + DualC link sweep (Phase 4) | `size_baseline` empty; `status-sync`, `stale-paths` green; `components.md` gone; handoff table filled |
| #28 | first `/docs-semantic-lint` run recorded (Phase 5) | run file under `09-docs-layers/0X-semantic-lint/` |

## Phase 1 — The gate (1 session; before any big move)

Source: `Copy-Item D:\DualC\scripts\check.py`, `check_data.json`, `check_fixtures\`,
`hooks\pre-commit`, `.gitattributes` line, `.claude\settings.json` — then adapt:

1. **Delete** the build tier (`check_configure`, `check_build`, `check_warnings`, `check_ctest`,
   `check_parity`, `--gpu`, `--build-dir`, `--config`, `parity_expected_cases`), `check_vendoring`
   + `third_party_roots`, `check_flag_table` (parses `examples/*.cpp` — no referent here).
2. **Replace** the build tier with `check_dotnet_build` (`dotnet build Boletus.sln -c Debug`,
   fail on any warning not in an allowlist — NU1701 is suppressed in the csproj already) and
   `check_dotnet_test` (`dotnet test tests/Boletus.Core.Tests -c Debug`; report the count, assert
   ≥ the recorded floor in `check_data.json` `tests_expected_min: 117`). Tier `build`, minutes,
   not in `--fast`.
3. **Add `dualc-links`** (fast, report_only → fails under `--strict`): scan `docs/**/*.md`,
   root `*.md`, `native/README.md`, `src/**/*.cs`, `tests/**/*.cs` for `D:\DualC\…`, `D:/DualC/…`,
   `../../../DualC/…`, and bare `docs/(roadmap|command_reference|design|decisions)/…` or
   `capi/…` paths inside backticks; resolve against `DUALC_ROOT` (env, default `D:\DualC`, key in
   `check_data.json`); **SKIP** when the checkout is absent; list each missing path with
   `file:line`. Also flag `file:///D:/DualC` URIs (must be `../../DualC/…` relative or backticked
   prose). Fixture pair under `check_fixtures/dualc-links/{pass,fail}/` with a `data.json`
   pointing `dualc_root` at a miniature tree inside the fixture.
4. **`check_data.json` for Boletus:** `size_scope: ["docs/"]`; `frozen_prefixes: ["docs/raw/"]`;
   `footer_roots` = roadmap/command_reference/design/decisions; `status_tables:
   ["docs/roadmap/README.md", "docs/roadmap/09-docs-layers/README.md"]`; `root_entry_files:
   {"AGENTS.md": 80, "CLAUDE.md": 0}`; `claude_md_forbidden` keeps DualC's shape-anchored
   patterns and gains `"entry-point count": "\\b\\d+ entry points"` (no free-form hash regex — a
   7-hex pattern matches prose; the DLL hash's single home is enforced by Phase 3's `rg -c`);
   `doc_lag.code_paths: ["src/", "tests/", "native/"]`; `decisions_index.scope: "docs/roadmap/"`;
   `usage_in_record.headers: ["Input", "Output", "Default", "Flag"]`; `out_of_repo_links.prefixes:
   ["../../../DualC/", "../../DualC/", "../DualC/"]`; `structure_summarized_prefixes` = the five
   docs folders + `scripts/check_fixtures/` + `native/x64/` (binaries: one line);
   `index_roots` = every docs folder (grown as folders are promoted); `size_baseline` = the 4
   oversize docs at their current lines/words/bytes; `heading_status_baseline` = per-file counts
   of today's headings that carry DONE/dates (e.g. `roadmap.md`, `03`, `04`, `05`, `07`, `08`) —
   never rewritten, only shrinking as pages are split in Phase 4.
5. Wire: `.claude/settings.json` Stop hook (verbatim from DualC); `scripts/hooks/pre-commit`;
   `.gitattributes` `scripts/hooks/* text eol=lf`; `git config core.hooksPath scripts/hooks`;
   `.gitignore` += `__pycache__/`. The `--selftest` has no CTest to register with — `AGENTS.md`
   lists it as the third gate command instead.
6. Run `check.py --docs` against the untouched tree: expect FAILs on `indexes` (no
   `docs/README.md`, `roadmap.md` not `README.md`), `footer`, `sizes` (baselined → OK),
   `structure` (13 missing files, 1 deleted), `root-entry`/`claude-md` (CLAUDE.md is 189 lines).
   Record the counts in `09/01-record-phase-1.md`; these are the worklist for Phases 2–4. The
   gate is **red between Phase 1 and Phase 2** (one session) and 09 says so; no gate code is
   bent to accommodate a filename Phase 2 removes.

Verify: `--selftest` all pairs pass (the ported 11 + `dualc-links`); `--fast` ≈ 1 s;
`dotnet test` 117/117.

## Phase 2 — Entry points (1 session)

1. **`AGENTS.md`** (new, ≤ 80 lines), exactly three sections: *what Boletus is* (2 lines);
   *Build, test, gate* (`dotnet build Boletus.sln -c Debug`, `dotnet test …`, the three
   `check.py` forms, `git config core.hooksPath scripts/hooks`; the NuGet-online-restore note
   and the "native code is never published" rule as one line each — the local-only constraint is
   a principle-level fact); *Five principles* (copy DualC's wording, "command reference" → "the
   component reference grows with the code: a component or input created/changed/removed gets its
   `command_reference/` page updated in the same session"); *Reading order* (DualC's six steps
   with Boletus paths). No counts, versions, hashes, dates, item ranges.
2. **`CLAUDE.md`** = `@AGENTS.md`. Its content disperses: § What this is (lines 5–99) → nothing
   (it is the roadmap README's Current focus, already there) except the design facts, which
   Phase 3 harvests from it into `design/`; § Commands → `AGENTS.md` + root README; § Architecture
   → `design/README.md`; § Gotchas → `design/06`/`07`; § DualC dependency → `docs/README.md` link +
   `native/README.md`. **Do the harvest before deleting**: copy CLAUDE.md verbatim to
   `docs/raw/2026-09-21-claude-md-before-phase-2.md` (DualC did the same for its skill) so
   nothing is lost while Phase 3 mines it.
3. **`docs/README.md`** (new, ≤ 60): DualC's file with Boletus paths; the fact-class → owner
   table gains one row: *"a DualC fact (an op token, an ABI signature, an engine invariant) →
   link to DualC's own layer (`design/`, `command_reference/`, `capi/`), never restated"*. "How to
   find things": `rg -n '#2[1-8]' docs/`, `rg -n 'D-0' docs/decisions docs/roadmap`,
   `rg -n 'DRIFT-PENDING|Reconstructed' docs`, `rg -n 'mem://' src docs`.
4. **Root `README.md`**: keep pitch/architecture/build/usage; the status blockquote (8–25) → one
   sentence + link to `docs/roadmap/README.md`; "Repository layout" (60–80) → link to
   `STRUCTURE.md`; the roadmap table (152–167) → one link to `docs/README.md`; line 4
   `file:///D:/DualC` → backticked `D:\DualC` prose; drop "117/117", "12 glyphs", "11 entry
   points" (they get one home in Phase 3). Target ≤ 150 lines.
5. **`STRUCTURE.md`**: rewrite from the explorer's tree — add the 13 files missing
   (`LivePreviewComponent.cs`, `StrutLatticeComponent.cs`, `GradedOffsetComponent.cs`,
   `MixComponent.cs`, `OffsetComponent.cs`, `TwistComponent.cs`, `BendComponent.cs`,
   `DisplaceComponent.cs`, `AxisName.cs`, `MeshMaterializingResolver.cs`,
   `MeshMaterializingResolverTests.cs`, `docs/roadmap/08-strut-lattices.md`,
   `native/x64/dualc_field_view.exe`), remove `ContourExportComponent.cs`, strip every
   status/date/count annotation (`:57` 86, `:184` 83, `:76`/`:204` `e345bf3`), `docs/` → one
   line per folder, add `scripts/`, `AGENTS.md`, `.claude/settings.json`. ≤ 15 KB.
6. `native/README.md`: keep (it is the DLL's provenance); fix `:68` (phantom
   `scripts/update-native.ps1` → "not written; D-NN") and re-point `:46` to DualC
   `command_reference/12-dualc_field_view/README.md` + `roadmap/12-field-graph-and-app/03-raymarch-app.md`.
   Same sweep for the other **Boletus-internal phantom paths** the `dualc-links` check cannot
   see — `rg -n 'scripts/update-native|copy of .dualc_c\.h|version assert' docs native`
   (`02:115`, `02:122`, `native/README:75`): each becomes a dated append naming the decision row
   Phase 3 will create.
7. **`git mv docs/roadmap/roadmap.md docs/roadmap/README.md`** — the rename only (the index
   *rewrite* is Phase 4); retarget the in-repo inbound links (`README.md`, `00:3,89`, `01`…`08`
   footers, `examples/README.md`). This turns `indexes` green without touching gate code.

Verify: `root-entry`, `claude-md`, `structure`, `indexes` green; `links`/`anchors` 0 broken;
`dotnet test` 117/117.

## Phase 3 — `design/` + `decisions/` (1–2 sessions)

**3a — `docs/design/` (harvest, don't write fresh).** Sources, by page (the explorer's class-(a)
map): `README` ← roadmap 01 §7–46 layering + `roadmap.md` 25–34 diagram + the Volume paragraph
(`roadmap.md` 61–71); `01` ← roadmap 01 §48–124 (ABI table → **link** to `D:\DualC\capi\README.md`
/ `capi/dualc_c.h`, keep only the P/Invoke-side facts), roadmap 03 §34–46; `02` ← roadmap 04
§156–179 design decisions (present-tense part), `04` §31–55 op coverage (as prose; the tables go
to cmdref 05); `03` ← roadmap 05 §29–60, 07 §7–82 (the pinned-for-the-call-only rule); `04` ←
roadmap 05 §21–27, 134–157, the WriteToFile threading paragraph (CLAUDE.md 99–121), the
LivePreview paragraph (CLAUDE.md 123–140), ProxyPreview cap, icons (`BoletusIcons.cs`, 20 glyphs
— verify by reading the file, not the docs); `05` ← `ExportPlan.cs` + roadmap 05 §696–716
(the I/O table itself goes to cmdref 04); `06` ← CLAUDE.md gotchas, `roadmap.md` 114–138
standing constraints (the non-decision ones), memory rules `metric-by-default-sources`,
`gh-multichoice-input-docs` (today referenced at `05:715` but living nowhere in the repo — this is
its home); `07` ← golden counts (`00` 55–70 — **the one home**; `CLAUDE.md` 159–168, `README`
100–104, `03:50`, `WrapperTests.cs` comments become links/refs), maxDepth rule, `MaxProxyDepth`,
version trap (`native/README` 32–36, `07` 115–121), the cancel constraint; `08` ← terms + the
ID vocabulary rule (`#N` = Boletus item; DualC's are written `DualC #17`, `DualC D-12`).
Every page: names files/symbols, no line numbers, no dates/"now"/"since" (gate), footer
`← Back to the [design index](README.md) · the [docs index](../README.md)`. Budget one
`design-no-history` rewrite pass per page: `\bnow\b` (case-insensitive) is the pattern that
bites present-tense mechanism prose — it is the gate working, not a gate bug. "Things we do not
do": no publishing (local-only), no mesh between components, no temp files for mesh leaves, no
per-primitive ABI factories, no renumbering, no restated counts, no vector index, no reflow of
`raw/`.
**Resolution rule** (DualC plan § 4): every present-tense claim is checked against `src/` /
`tests/` / `native/README.md` (e.g. the DLL commit is `d6b2808` per `native/README.md`; entry
points = count the `[DllImport]`s in `NativeMethods.cs`; glyphs = count in `BoletusIcons.cs`;
tests = `dotnet test` output). Where git cannot decide → `**Reconstructed (2026-09-xx) from
commit <hash>**` or `DRIFT-PENDING:`.

**3b — `docs/decisions/README.md`.** Seed from the explorer's 45-row list (§ 6 of its report);
expected ≈ 25–30 rows after merging duplicates. Columns as DualC. IDs `D-01…` by decision date.
Rows that must exist (each with a trigger written **here**, and a one-line dated append on the
roadmap entry pointing back): DAG-ref serialization (DEFERRED, `04:172`, `07:99`); static `/MT`
runtime (DEFERRED, `07:84`, trigger: first Yak for a non-dev machine); uniform push IPC
(DEFERRED, `07:178`); export progress/cancel Phase C (DEFERRED, `07:236`); child-process exporter
Phase B (DROPPED, `05:618`); embedded-PNG icons (DROPPED, `05:454`); multi-mode "Deform"
(DROPPED, `05:506`); `mirror`/`elongate`/`repeat` components (DEFERRED, `05:506`); ROI sub-box
proxy (DEFERRED, `05:404`); `IGH_VariableParameterComponent` for Primitive (open, `05:820`);
NuGet feed for the DLL (DEFERRED, `02:126` with its 4 triggers); `scripts/update-native.ps1`,
committed `dualc_c.h` copy, runtime version assert (`02:107–124` — DEFERRED or DROPPED, decide by
reading; today they are promised and absent); re-vendor DualC ABI 0.4.0 / 13 entry points
(DEFERRED — new, trigger: a Boletus feature needing a 0.4.0 entry point; DualC `14/04`); the
skip-preview-when-exporting decision (`05:356`, superseded → DROPPED); FTS5/search index
(DEFERRED, trigger as DualC's D-40). Settled/DONE-by-decision rows (net7.0-windows TFM, Grasshopper
metapackage, project-ref + vendored DLL, local-only, tiled STL always, binary formats, standalone
Onion/Graded-onion/Normalize, `Volume` currency, viewer's own camera, GUID `…033`, DLL pinned by
commit) go in `01-settled.md` if the README trips the cap, else in the same table.
Gate: `decisions-index` needs each DROPPED/DEFERRED roadmap entry's **first body line** to open
with `**DEFERRED`/`**DROPPED` — today most are inline prose (`07 §6`, `05:618`), so this phase
adds the bold status line as a dated append under each such heading (append-only respected).

Verify: `design-no-history`, `decisions-index`, `status-vocab` green; `rg -c '117/117|d6b2808|11 entry points' docs` shows exactly one home each (design/07 or native/README);
`dotnet test` 117/117.

## Phase 4 — `command_reference/`, roadmap consolidation, DualC link sweep (1–2 sessions)

1. **`command_reference/00–06`** from `docs/components.md` + roadmap 08 §43–179 + roadmap 04
   §57–154 + roadmap 05 §696–716 (the per-page map is in the target tree). Page anatomy:
   `# Family N: <name>` → one sentence → per component: purpose, `| Input | Type | Default |
   Meaning |` (defaults read from `pManager.Add*Parameter` calls in `src/Boletus.Grasshopper/*Component.cs`,
   not from the old doc), outputs, dropdown option sets in full (the `gh-multichoice-input-docs`
   rule), exact `Remark`/`Warning`/`Error` strings quoted from the source, `## Recipes` (letter
   IDs `S1…`, `D1…`, `B1…`, `T1…`) → footer. `05-field-graph-ops.md`: the Core builder ↔ DualC op
   token table; parameters/defaults are **links** to DualC `command_reference/11-dualc_field/01-op-vocabulary.md`
   and `02-dualc_primitive.md`, never copied. Delete `docs/components.md`; retarget its 3 inbound
   links (`README.md:136`, `examples/README.md`, `05:715`).
2. **Roadmap consolidation** (moves only; existing entries get dated appends, never rewrites):
   rewrite `roadmap/README.md` (renamed in Phase 2) to DualC's index anatomy (legend; **Current focus**
   = one paragraph + links; **Next up**; **Principal blocks** table 00–09 with a Status column the
   gate syncs; how the blocks relate). Promote `05` and `07` to folders (cut list in the size
   table; parent README keeps the intro + a page table; children keep every heading verbatim so
   anchors survive — inbound anchors are retargeted by the `anchors` check). `04`: op tables → one
   sentence + link to cmdref 05; the pre-implementation planning text (213–278) stays (it is the
   record). Every "how it works today" paragraph in a topic intro → one sentence + link to the
   `design/` child (item (c) of the semantic lint; do the obvious ones now: `roadmap.md` 51–112,
   `05` 21–60, `07` 7–82, `08` 23–41). `06:15` "not even a git repo yet" → dated append. `05:40`
   malformed link → `` `D:\DualC\docs\roadmap\02-implicit-sdf-foundation.md` ``. Usage tables inside
   `roadmap/` (`usage-in-record` report) → links to the cmdref page.
3. **The DualC link sweep** (the `dualc-links` check is the proof; run it `--strict`):
   - `00-references-and-environment.md` table: `:23` drop `18-c-abi-continued.md` (tombstone) →
     `docs/roadmap/14-c-abi/` (+ `04-abi-0-4-0.md`); `:27` `docs/roadmap/README.md` → **add rows**
     for `docs/design/README.md` (engine as it is — `08-implicit-field-layer.md`,
     `09-conventions.md` "1 unit = 1 mm", `10-invariants-and-tolerances.md` mix/thin-feature rules),
     `docs/decisions/README.md` (Boletus-driven DEFERREDs D-12, D-17, D-21, D-29), `docs/README.md`
     (DualC's own map), `capi/README.md`; `:33` `e345bf3` → link to `native/README.md` (no hash
     here); `:57` "roadmap 14 §8" → `docs/roadmap/14-c-abi/03-implementation-and-verification.md`.
   - Prose paths that predate the 09-11 splits in **code comments** (comment-only edits, 117/117
     unchanged): `OpSchema.cs:59`, `Ops.cs:10,23,49,74`, `Field.cs:178`,
     `FieldGraphExampleTests.cs:13,67` → `11-dualc_field/01-op-vocabulary.md` /
     `11-dualc_field/04-workflow-open-surface.md`; `WrapperTests.cs:10,11,57`, `MeshBufferTests.cs:12`
     "roadmap 14 §8" → `14-c-abi/03-…`.
   - `07:163`, `native/README.md:46`, `CLAUDE.md:97` (gone by then) "roadmap 12 §D" →
     `12-field-graph-and-app/03-raymarch-app.md`; `07:193–194` relative links: keep, they resolve
     (the gate marks them foreign).
   - Wherever Boletus restates a DualC *engine* fact (op defaults, the thin-feature rule, "mix
     blends values not crystals" in `08:117–146`, the 3MF mm rule), replace with one sentence + a
     link into DualC `design/09`/`10` or the cmdref page.
   - Fill **09 § DualC handoff**: `15-boletus-handoff.md:162` cites `roadmap.md` → now `README.md`;
     `15:24–28` cite `03`–`07` by name (unchanged, still resolve; `05`/`07` become folders → the
     `.md` citations should become `…/README.md`); `12/03-raymarch-app.md:106` → `07-upstream-coordination/README.md`;
     `check_data.json out_of_repo_links` unchanged. One table, `DualC file:line · today's target`,
     for the DualC owner to apply in a DualC session.
4. `check_data.json`: `size_baseline` emptied; `index_roots` gains the promoted folders;
   `heading_status_baseline` shrinks with each split.

Verify: `--docs --strict` green (0 `DRIFT-PENDING` unlisted, `status-sync`, `stale-paths`,
`usage-in-record`, `dualc-links` all green against a present `D:\DualC`); `rg -c 'components.md' docs README.md examples` = 0 outside `raw/` and 09; `dotnet build` 0 warnings; `dotnet test` 117/117.

## Phase 5 — Close-out + first semantic lint (½ session)

1. `docs/raw/README.md` rows complete (plan, principle text, CLAUDE.md snapshot, any
   `dotnet test` log captured as evidence — `2026-09-xx-test-run-117.txt`).
2. Run the owner's `/docs-semantic-lint` **unmodified** (it reads `AGENTS.md`, `docs/README.md`,
   `docs/design/`, `check.py --docs`); its run-file path is hardcoded to
   `docs/roadmap/19-docs-layers/08-semantic-lint/` — Boletus's is `09-docs-layers/0X-semantic-lint/`,
   so **create the folder + README** and, if the command refuses the path, record the run there by
   hand and note in 09 that the command's path is DualC-specific (a one-line owner-level
   parametrisation is *their* follow-up, outside the repo — same boundary as DualC's D-41).
3. `09-docs-layers/README.md`: all five phase rows DONE with evidence (check counts, link/anchor
   counts, sizes, test count); items #21–#28 DONE; "What the block leaves" paragraph (as DualC
   19/07): the open handoff to DualC, the deferred FTS5 trigger, the cadence rule.
4. Update memory: `boletus-project.md` (rewrite — its ABI/temp-file facts are 95 days stale and
   contradicted by the repo) and add a `boletus-docs-architecture.md` pointer (entry file, gate
   command, the `#21+`/`D-NN` sequences, the DualC handoff convention).

---

## Deliberately not done

- **No edit to `D:\DualC`** — its broken inbound citations are reported, not fixed.
- **No renumbering** of `00–08`; `09` is the next free number; `#21` the next free item.
- **No behaviour change** in `src/`/`tests/`: comment-only edits; 117/117 at every commit.
- **No new skill/command in the repo** — the owner-level `repo-docs-lifecycle` and
  `docs-semantic-lint` are reused as-is (their DualC-specific path `19-docs-layers/08-semantic-lint`
  is the one known mismatch, surfaced in Phase 5).
- **No vector index / search layer**: `rg` + README indexes (FTS5 DEFERRED with a trigger).
- **No rewrite of roadmap history**: dated appends, `Reconstructed`/`DRIFT-PENDING` markers only.
- **No `flag-table` analogue in Phase 1.** A `component-io-table` check (parse
  `pManager.Add*Parameter` names in `*Component.cs` against the cmdref Inputs tables) is the
  natural port of DualC's parser-vs-page check; recorded as DEFERRED in 09 with trigger "the first
  cmdref default found wrong against the source after Phase 4" — a fixture-backed check written
  before there are pages to check would be built blind.

## Verification (end-to-end, at the close of Phase 5)

```powershell
python scripts/check.py --selftest              # every fixture pair (11 ported + dualc-links)
python scripts/check.py --docs --strict         # 0 FAIL, 0 INFO: links, anchors, indexes, sizes (baseline empty),
                                                #   footer, heading-status, status-vocab, status-sync, claude-md,
                                                #   root-entry, decisions-index, design-no-history, heading-hierarchy,
                                                #   stale-paths, directional, usage-in-record, drift-pending,
                                                #   doc-lag, structure, scripts, dualc-links (D:\DualC present)
python scripts/check.py                         # + dotnet build 0 warnings, dotnet test ≥ 117
dotnet test tests/Boletus.Core.Tests/Boletus.Core.Tests.csproj -c Debug   # 117/117, unchanged since c85c04e
git config core.hooksPath                       # scripts/hooks
rg -n 'components\.md|roadmap\.md' docs README.md examples STRUCTURE.md   # 0 hits outside docs/raw and 09
rg -c '117/117|e345bf3|9 entry points|12 glyphs|16 glyphs' docs *.md      # only the one home / the record's dated lines
```

Plus the re-run of this session's DualC path-existence sweep (23 paths) and the Claude Code
`Stop` hook observed firing (edit a doc, end the turn, see the report) — both recorded as the
close-of-phase evidence in `09-docs-layers/`.
