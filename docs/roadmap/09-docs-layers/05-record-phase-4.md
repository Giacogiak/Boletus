# Record — Phase 4 (`command_reference/` + roadmap consolidation + the DualC link sweep)

**DONE (2026-09-21).** Delivered: `docs/command_reference/` — the index README (the family
table, a task → page table, how the pages are kept true) and seven pages, `00-shared-behaviour`
to `06-worked-examples`, every inputs table with the defaults read from the
`pManager.Add*Parameter` calls, every dropdown's full option set, the remark, warning and error
strings quoted from the source, letter-ID recipes (S1–S8, D1–D10, B1–B7, T1–T7, W1–W4) and a
troubleshooting table of every string the canvas can show; `docs/components.md` **deleted** and
its six inbound links retargeted (`README.md`, `examples/README.md`, `STRUCTURE.md`,
`docs/README.md`, `docs/design/README.md`, roadmap 08, plus the dated note in the strut
increment); roadmap **05 and 07 promoted to same-numbered folders** with every heading
verbatim and every inbound anchor retargeted (27 decisions rows, the roadmap index, 00, 01,
02, 03, 04, 06, 08, `native/README.md`, the 09 records and three URLs in the `raw/` snapshot);
roadmap **04**'s op tables reduced to a sentence and a link to the command reference;
the **roadmap index rewritten** to DualC's anatomy (legend, Current focus, Recent milestones,
Next up, Principal blocks with anchored Status cells, How the blocks relate) with its restated
counts gone; the "how it works today" prose in the topic intros (05 § Project and § The Volume
datatype, 07 § 1's clarification and copied C declaration, 08's Normalize rule, hard rules and
`mix` caveat, 01's units line, 06's "not even a git repo yet") reduced to a sentence plus a
link with a dated note each; the **DualC link sweep** — nine comment-only `.cs` edits, roadmap
00's table (the retired tombstone dropped, four rows added for DualC's `docs/README.md`,
`design/README.md`, `decisions/README.md` and `capi/README.md`), the engine facts Boletus
restated turned into links into DualC's design 09 / 10 — and **09 § DualC handoff** filled
(five rows); `scripts/check_data.json` with `size_baseline` **empty**, three new `index_roots`
and the per-child `heading_status_baseline` entries.

**The `components.md` section map** (717 lines → the command reference; the old page is in git
at `7e7c95f`):

| `components.md` section | Went to |
| --- | --- |
| Mental model — the `Volume` wire, the Normalize rule, Onion before clip | [00 § The `Volume` wire](../../command_reference/00-shared-behaviour.md#the-volume-wire), [§ Metric and non-metric](../../command_reference/00-shared-behaviour.md#metric-and-non-metric--the-normalize-rule), [§ Onion before clip](../../command_reference/00-shared-behaviour.md#onion-before-clip); the *why* stays in design 06 |
| Sources — TPMS (with "Keep the TPMS raw, or Normalize it?"), Primitive, Mesh → Volume, Strut Lattice | [01](../../command_reference/01-sources.md) |
| Decorators — Normalize … Mix (ten components) | [02](../../command_reference/02-decorators.md) |
| Booleans — Boolean | [03](../../command_reference/03-booleans.md) |
| Terminals — Write to File, Proxy preview, Live Preview; the shared bounds / depth paragraph | [04](../../command_reference/04-terminals.md); [00 § Bounds](../../command_reference/00-shared-behaviour.md#bounds--min--max-on-a-terminal), [§ Depth](../../command_reference/00-shared-behaviour.md#depth) |
| The `Volume` wire type | [00 § The `Volume` wire](../../command_reference/00-shared-behaviour.md#the-volume-wire) |
| Worked example — a clipped sheet lattice; — a printable part; — a strut-lattice infill; the `demo.gh` pointer | [06](../../command_reference/06-worked-examples.md) W1, W2, W3, W4 |
| Quick troubleshooting | [00 § Troubleshooting](../../command_reference/00-shared-behaviour.md#troubleshooting), every string re-quoted from the source |

Roadmap 04's "Pinned op signatures" tables and roadmap 05's two Write-to-File I/O tables went
to [05 — Field-graph ops](../../command_reference/05-field-graph-ops.md) and
[04 § Write to File](../../command_reference/04-terminals.md#write-to-file); roadmap 08's node
tables stay where they are, as the vocabulary pinned at the sync (they pass `usage-in-record`
as written and the plan did not name them).

**Facts corrected against the source while writing the pages.** `Proxy preview`'s `Depth`
default is **5** (the old page said so, the old roadmap README implied 6); `Write to File`'s
`Depth` is 6, `Format` 0, `Mode` 0, the warning threshold 8; `Displace`'s Amplitude and
Frequency and `Boolean`'s `k` have **no** Boletus default (the pages say "the engine default"
and link DualC rather than repeating 0.1 / 6 / 0.25); the `Tile depth` warning, the extension
override and the 3MF fallback are `ExportPlan`'s exact strings; the old "scale Rhino-doc units
→ mm" line was a plan that never shipped (dated notes on 05/01 and 01). Every option set was
read from the `AddNamedValue` lists.

**The 05 split** — `05-phase3-grasshopper-components/`: `README.md` (37 lines; the PARTIAL
status line, the page table with every section's anchor) and `01-volume-datatype-and-palette`
(the plan sections, 153 lines / 11.6 KB), `02-thin-slice-and-volume-palette` (3b.1, 3b.2),
`03-diskless-mesh-and-tiled-stl`, `04-proxy-preview-and-icons`,
`05-domain-warps-and-example-graph`, `06-write-to-file` (169 lines / 12.0 KB, the largest),
`07-strut-lattice-components` (the strut increment and the remaining increment). **The 07
split** — `07-upstream-coordination/`: `README.md` (the PARTIAL status line, the page table,
and § 2–§ 4 verbatim — the three short standing asks, DualC's 14 README precedent of keeping a
section on the folder page) and `01-in-memory-mesh` (§ 1), `02-viewer-and-uniform-push` (§ 5,
§ 6), `03-export-callback-and-strut-sync` (§ 7, § 8). The six `**DEFERRED**` / `**DROPPED**`
status lines travelled with their headings; `decisions-index` finds all six.

**Evidence at close.** `check.py --docs --strict`: 25 checks — **25 passed, 0 failed, 0
skipped**, no report-only list (`usage-in-record` 0, `dualc-links` 106 files scanned / 100
DualC paths cited / 0 missing, `index-staleness` clean — the folder READMEs and their
children landed in one commit, `c4a1bfc`). `links` 645 / 0 broken (40 cross-project);
`anchors` 247 cross-file + 13 same-file / 0 unresolved; `indexes` 8 roots / 44 siblings /
0 unlinked; `sizes` 46 files / 0 baselined / 0 over; `heading-status` 50 files, 17 grandfathered (13 + 2
moved into the children, 1 on 04, 1 in `raw/`), 0 over; `status-sync` **6 anchored rows
checked** (03, 04, 05, 06, 07, 09 on the roadmap index), 18 unverifiable, 0 disagree;
`stale-paths` 4 folders / 0 new; `decisions-index` 6 entries / 19 rows / 0 problems.
`check.py` (the full gate): `dotnet build` 0 warnings, `dotnet test` **117 passed, 0 failed**
after the comment-only `.cs` edits. Sizes: the command reference is 8 pages, 1,014 lines,
67 KB, the largest page 02 at 242 lines / 2,302 words / 12.8 KB; roadmap 04 is down to 207
lines / 15.0 KB from 290 / 20.0 KB; the roadmap index is 99 lines / 9.7 KB. `rg -c
'117/117|e345bf3|11 entry points|d6b2808|16 glyphs' docs *.md`: outside `raw/` every hit is
a dated record line, 01's grandfathered heading, or an anchor slug (`…-sync-d6b2808-…` — the
only kind of hit on the roadmap index and the decisions pages); no design page. `rg -n 'components\.md|roadmap\.md' docs README.md examples
STRUCTURE.md`: outside `raw/` and 09 the hits are dated "Files changed" rows in the 05
children, the D-34 row, and the child name `07-strut-lattice-components.md` — the plan's
regex matches that name; a literal `docs/components.md` link exists nowhere.

**The `.cs` comment edits** (comment-only; the test count is the proof): `OpSchema.cs` and
`Ops.cs` (the summary) name `11-dualc_field/01-op-vocabulary.md`; `Ops.cs`'s strut block names
`11-dualc_field/02-strut-lattices.md`; `FieldGraphExampleTests.cs` names
`11-dualc_field/04-workflow-open-surface.md` (the `:427` line pin dropped); `WrapperTests.cs`
and `MeshBufferTests.cs` cite `docs/design/07-invariants-and-limits.md` for the golden counts
with DualC's `14-c-abi/03` as the record; `MeshBuffer.cs`'s summary no longer says "once DualC
ships the in-memory resolver" and cites design 03 and `07-upstream-coordination/01`;
`WriteToFileComponent.cs` cites `07-upstream-coordination/03` for Phase C. `Ops.cs:74` and
`Field.cs:178` already named `02-dualc_primitive.md` correctly and were left alone.

**Deviations from the plan.** (1) The 05 cut list named five children; the byte totals do not
fit — `Write to File` + the strut increment is 18.3 KB, the plan sections + 3b.1 are 18.2 KB
against the 15,360 B cap — so the folder has **seven** children, cut by increment, and 02–04
carry names that say what they hold (`02-thin-slice-and-volume-palette`,
`03-diskless-mesh-and-tiled-stl`, `04-proxy-preview-and-icons`). (2) 07 § 2–§ 4 stay on the
folder README rather than on a child. (3) Three link URLs in the immutable `raw/` snapshot of
`CLAUDE.md` were retargeted to the folders (the `links` check reads `raw/`), the same
normalization its `raw/README.md` row already recorded, noted there with the date; no other
byte of `raw/` changed. (4) The roadmap index's Status column anchors only the six rows whose
target's first body line carries the status word (03, 04, 06 do; 05, 07 and 09 gained a status
first line on their rewritable folder READMEs); 00, 01, 02 and 08 link the file, unverifiable
by design. (5) The "Cites today" cells of the handoff table name the pre-split pages by bare
name, not path, because `dualc-links` reads a bare `docs/…` path as a citation and the old
paths exist in neither repo — which is the point of the table. (6) `MeshBuffer.cs`'s comment
was stale beyond the path (it described the in-memory resolver as future); rewriting the
sentence was the precision fix, not a behaviour change.

**Left for Phase 5.** The first `/docs-semantic-lint` run (the command's DualC-specific run
path), `docs/raw/README.md` complete, the 09 README's "What the block leaves" paragraph, the
memory update; and, for a DualC session, the five handoff rows.

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
