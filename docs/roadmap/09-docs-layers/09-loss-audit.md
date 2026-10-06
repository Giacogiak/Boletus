# Loss audit — the base tree against the restructured one

Part of the [Docs layers record](README.md) (roadmap 09). A post-close child, not a phase:
DualC ran the same audit on its own restructuring before merging it
([DualC 19/09](../../../../DualC/docs/roadmap/19-docs-layers/09-pre-merge-loss-audit.md)), and
Boletus, which restructured straight onto `main`, had no such proof. Item **#30**.

**DONE (2026-10-02).** Base `c85c04e` (the last commit before the plan landed in `cb50344`)
against `cb138ad`. The script is [`scripts/docs_loss_audit.py`](../../../scripts/docs_loss_audit.py),
its triage [`scripts/docs_loss_audit.json`](../../../scripts/docs_loss_audit.json), the full
report [`2026-10-02-loss-audit-c85c04e-cb138ad.txt`](../../raw/2026-10-02-loss-audit-c85c04e-cb138ad.txt). Result:
**0 untriaged, 4 facts restored** — every other residual is a move or a removal a phase
record names, a correction against the code, or noise, each with a citation the script
verifies on the head tree.

## Why

The gate proves shape: links, anchors, sizes, index rows, status words. No phase record of
this block claims more. None proves that a sentence, a number, a heading or an identifier
that existed before the restructuring still exists after it. The audit does, as set
arithmetic over two git trees: the same base, head and triage file give the same bytes and
the same exit code. Two runs at `cb138ad` were byte-identical.

## Method

DualC's script, ported with only its corpus constants changed:

| Constant | Boletus |
| --- | --- |
| scope (base tree) | `docs/`, `CLAUDE.md`, `README.md`, `STRUCTURE.md`, `native/README.md`, `examples/README.md`: 16 files |
| annex (reported apart) | `CLAUDE.md`, dispersed whole in Phase 2 |
| tier a (live docs) | `docs/design`, `roadmap`, `command_reference`, `decisions`, the root markdown files, `native/README.md`, `examples/README.md`, `.claude/`: 46 files |
| tier b (verbatim homes) | none: no file moved into `raw/` unchanged |
| tier c (quotation only) | `docs/raw/` and this block's records, which quote what they removed: 16 files |
| code (identifiers) | `src/`, `tests/`, `examples/`, `scripts/`, `.cs` included: 115 files |
| default base | `c85c04e` in place of DualC's merge-base |

The units, thresholds, buckets and verdicts are DualC's, unchanged (the thresholds are pinned
in the triage file). The audit's own script, triage, dump and this record are excluded from
the corpus.

## Result

| Unit | Kept | Residual | intentional | superseded | noise |
| --- | --- | --- | --- | --- | --- |
| sentence | 841 | 658 | 618 | 25 | 15 |
| evidence | 319 | 30 | 28 | 2 | 0 |
| heading | 155 | 32 | 32 | 0 | 0 |
| identifier | 1,430 | 19 | 14 | 5 | 0 |
| `CLAUDE.md` annex, all units | 146 | 78 | 75 | 3 | 0 |

805 distinct residual keys (a sentence shared by two base files is one key) carry one
verdict each in the triage file. Every `gone`, `partial`, `token-only`, `raw-only` and
`unverifiable-context` row was read against the head tree and, where it stated a behaviour,
against `src/`; the `rewritten` rows were sampled, about ten per group. Three readings ran in
parallel: `components.md` (270 keys), the root files (291), the roadmap (246).

- **Moved** (cited to the phase record that moved them): `components.md` → the command
  reference by the section map of [05](05-record-phase-4.md); the old `STRUCTURE.md` rows → the
  rewritten tree, `design/` and the records; `roadmap.md` → the snapshot and the block
  pages; `CLAUDE.md`'s facts → `AGENTS.md`, `design/`, `decisions/`, `native/README.md`.
- **Removed by policy**: status claims, restated counts and dates (Phase 2), DualC engine
  defaults and copied C declarations (Phase 4: linked, never restated), the deleted pages'
  heading anchors (their inbound links were retargeted).
- **Superseded**: base text that was wrong against the code or git — the DLL commit
  `e345bf3` (re-vendored from `d6b2808`), "used by both terminals" for `RhinoMeshConvert`
  (only `ProxyPreviewComponent` calls it), 12 icon glyphs (20), Live Preview listed as absent,
  "`.stl` export is always tiled" (the Tiled / Monolithic mode), `native/README.md`'s
  promised `update-native.ps1` and startup version check (never built, D-08 and D-10),
  "scale Rhino doc units to mm on export" (never implemented; one unit is one millimetre,
  design 06), `BUSY` listed as an `Info` value (it is a Warning).
- **Lost, restored in `cb138ad`**, each checked against `src/` first and reworded into its
  owner:
  - [cmdref 02 § Bend](../../command_reference/02-decorators.md#bend): it reads best on a
    bounded shape, where the arc is visible.
  - [cmdref 02 § Displace](../../command_reference/02-decorators.md#displace): a
    surface-finishing step, usually near the end of a graph.
  - [design 03 § MeshBuffer](../../design/03-volume-and-resolvers.md#meshbuffer): `ContentHash()`
    is a 64-bit FNV-1a over the lengths, vertices and indices, the dedupe key.
  - [design 02 § The files](../../design/02-field-graph-model.md#the-files): `Ops.Tokens`
    lists every registered token.

**Handed on to a code session** (not a docs fact): the `Info` output's hover description in
`WriteToFileComponent.cs` still mentions "a BUSY note", while the BUSY text is a runtime
Warning; [cmdref 04](../../command_reference/04-terminals.md) states the strings as the code
emits them.

## Rerun

```bash
python3 scripts/docs_loss_audit.py                    # base c85c04e vs HEAD; exit 0 = clean
python3 scripts/docs_loss_audit.py --untriaged        # what is left, if anything
python3 scripts/docs_loss_audit.py --selftest         # the pure functions, no git
```

A fact moved after `cb138ad` shows as a new residual and is triaged by key, with the ratchet at
0. The same script audits any future restructuring: a new key under `audits`, keyed by its
base commit.

**Limits.** The audit proves *presence*, not *truth*; truth is the semantic lint's job
([06](06-semantic-lint/README.md)). A `rewritten` sentence's nuance is scored, not read.

*Rerun (2026-10-02, after `fda1aff`):* the five-row milestones window (D-43) removed the
roadmap index's last copy of one evidence token, the Live Preview's "2026-07-03, verified"
(base `roadmap.md:101` and `CLAUDE.md:83`, one key). The ratchet reported it as untriaged.
The dated fact is in [07 § 5](../07-upstream-coordination/02-viewer-and-uniform-push.md#5-dualc_field_view-viewer-binary--gates-the-raymarch-preview-phase),
so it was triaged *intentional* by key, citing that line; exit 0 again.

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
