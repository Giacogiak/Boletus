# Semantic-lint run 04 — after the broader `Primitive` set

Run on 2026-10-03, the fourth run of [`/docs-semantic-lint`](README.md) on Boletus, owed by
`2963349` (the broader `Primitive` set: `src/` + `design/04`, the same day as
[run 03](03-2026-10-03-linux-port-run.md)). Gate green before reading
(`python3 scripts/check.py --docs`, 25 passed, the four index-staleness lines report-only).

| Commit | What | Date |
| --- | --- | --- |
| `2963349` | last code change (`src/`, `tests/`, `scripts/check_data.json`) | 2026-10-03 |
| `2963349` | last `docs/design/` change (`04`, `06`) | 2026-10-03 |
| DualC `2fcd19f` | the sibling checkout read for the engine's side | 2026-10-03 |

**Pages read:** every `docs/design/` page (`README`, `01`–`08`) against the files each names;
`02`, `04`, `06`, `07` by the owner, `README`, `01`, `03`, `05`, `08` by a read-only agent
whose candidates were each re-verified on both sides before entering the table. Class (b)
candidates: the pages older than `2963349` whose named files that commit touched — `02`
(names `src/Boletus.Core/FieldGraph/`, where `PrimitiveCatalog.cs` was added) and `07`
(names `scripts/check_data.json` and `tests/Boletus.Core.Tests/`; it restates no count and
lists test files as examples, so nothing on it is behind). `03` and `05`: no contradiction.

## Findings

| # | Class | Finding | Docs | Code / record | Disposition |
| --- | --- | --- | --- | --- | --- |
| 1 | (a) | `libdualc_capi.so` "depending on libstdc++ and libc only" — `readelf -d` lists `libstdc++.so.6`, `libm.so.6`, `libgcc_s.so.1`, `libc.so.6`; `native/README.md` has the four. | `design/README.md:47-48` | `native/linux-x64/libdualc_capi.so` (NEEDED), `native/README.md:51` | handed to the next session |
| 2 | (a) | The `Volume` contract says an input-mesh leaf is "referenced by the graph as `mesh(id=…)` / `winding(id=…)`"; inside a `Volume` the leaf is `mesh(path="mem://<hash>")` and `id=` appears only after `VolumeResolver` at an in-process terminal (the viewer path gets a file path). `03` states it correctly. Borderline — the sentence can be read as the leaf as the DLL receives it. | `design/README.md:70-71` | `src/Boletus.Grasshopper/MeshToVolumeComponent.cs:79`, `src/Boletus.Core/Volume.cs:32`, `src/Boletus.Core/VolumeResolver.cs:38` | handed to the next session |
| 3 | (a) | "DualC's current header also declares the ABI 0.4.0 diagnostics twins; they are additive (the originals are forwarders)" — since 2026-10-02 the header also declares the 0.5.0 progress twins and four cancel-token entry points, which are new functions, not forwarders: twenty declarations against the eleven bound. The deferral the sentence cites (D-37) is the 0.4.0 one; the 0.5.0 side is D-30, now NEXT. | `design/01-native-interop.md:45-47` | `../DualC/capi/dualc_c.h:252,281,305,314-317`; `src/Boletus.Core/NativeMethods.cs` (11 `DllImport`) | handed to the Phase C session, which re-vendors |
| 4 | (a) | The ABI-owned `DualcMesh` "is released in a `finally` on every path" — `Native.Check` runs before the `try`, so a non-OK `dualc_field_contour` throws and skips the release; the header does not say whether an error leaves a mesh to release, so "every path" is an overstatement, not a leak the code proves. Minor. | `design/01-native-interop.md:25-26` | `src/Boletus.Core/DualcField.cs:122-132`; `../DualC/capi/dualc_c.h:226-233` | handed to the next session |
| 5 | (a) | Strings "come back through `Marshal.ReadByte` (`ReadUtf8`)" — only the `IntPtr` overload (used by `Version()`) does; every error message comes back through the `byte[]` overload, `Encoding.UTF8.GetString` over the buffer. Minor. | `design/01-native-interop.md:72-73` | `src/Boletus.Core/Native.cs:45,54,71` | handed to the next session |
| 6 | (a) | "`#21`–`#28` are block 09's" and the `rg -n '#2[1-8]'` idiom — the block's items run to `#31` (`#29`–`#30` post-close 2026-10-02, `#31` 2026-10-03); the idiom misses three. Contradicted by the record, not the code. | `design/08-glossary.md:63-64` | `roadmap/09-docs-layers/README.md:49,62`, `roadmap/README.md:100` | handed to the next session |
| 7 | (a) | Tile depth "defaults to `depth − 2`" — the planner floors it at 1 (`Math.Max(1, depth - 2)`); `05` states the floor. Minor. | `design/08-glossary.md:46` | `src/Boletus.Core/Export/ExportPlan.cs:114` | handed to the next session |
| 8 | (a) | "Consolidated" is glossed as "one component with a mode dropdown (`TPMS`, `Primitive`, `Boolean`, `Strut Lattice`)" — `Segment Primitive` and `Axial Primitive` (this commit) are the same pattern, and so were `Mesh → Volume` (Kind) and `Displace` (Function) before it; nothing marks the list as examples. | `design/08-glossary.md:50` | `src/Boletus.Grasshopper/PrimitiveFamilyComponent.cs:42`, `MeshToVolumeComponent.cs:47-49`, `DisplaceComponent.cs:39-41` | handed to the next session |
| 9 | (a) | The illustrative lists predate the catalog components: the Remark examples name `Plane` and `Strut Lattice` as the infinite-extent sources (InfiniteCylinder / InfiniteCone now post the same remark), and the dropdown-legend examples stop at the displace functions (seven, five and sixteen primitive types now). Examples, not claims — minor. | `design/06-conventions.md:64,79` | `src/Boletus.Grasshopper/PrimitiveFamilyComponent.cs:104`, `PrimitiveComponent.cs:31-37` | handed to the next session |
| 10 | (a) | "The C ABI's export calls take no cancellation token and no progress callback" and "both lift only when DualC exposes a per-tile progress/cancel callback" — DualC exposed it on 2026-10-02 (ABI 0.5.0); the sentences are true of the **pinned** ABI only and should say so, since the invariant now lifts on the Boletus side (D-30, NEXT), not on an upstream event. | `design/07-invariants-and-limits.md:84,88` | `../DualC/capi/dualc_c.h:252,281,305,314-317`; `decisions/README.md` D-30 | handed to the Phase C session |
| 11 | (b) | `02` is behind `2963349`: its "The files" table names eight files of `FieldGraph/` and `PrimitiveCatalog.cs` is the ninth; and its pinning note — the flat builders' positional order "pinned by reading, not by a test" — is now half true: for the twenty-one catalog shapes the slot → position order is pinned by `PrimitiveCatalogTests` against the builders, the builders' own argument names still by reading. | `design/02-field-graph-model.md:15-26,80` | `src/Boletus.Core/FieldGraph/PrimitiveCatalog.cs`, `tests/Boletus.Core.Tests/PrimitiveCatalogTests.cs:61,70` | handed to the next session |
| 12 | (e) | D-10 — carried from run 03, finding 4: the trigger ("the next re-vendoring, done by the script instead of by hand") stays met in substance since `f8e4a47` vendored the Linux `.so` by hand; no session has reopened the row. The Phase C re-vendor (NEXT) is the moment to do it by script or to drop the decision. | `decisions/README.md:30` | `native/README.md` (the manual recipe) | handed to the Phase C session |

**(a) — 10 findings** (1–10), none in `03` or `05`; four minor (4, 5, 7, 9) and one borderline
(2). Three of them (3, 10, 12) converge on the same event — DualC's ABI 0.5.0 of 2026-10-02 —
and are best fixed together by the Phase C session that re-vendors.

**(b) — 1 finding** (11). The newest code commit is also the newest design commit, so the
only candidates were the two pages whose named files it touched; `07` holds.

**(c) — 0 findings.** The seed grep (`today|currently|at present`) over `roadmap/` and
`command_reference/` hits only the lines runs 01–02 dispositioned (the dated notes they left,
roadmap `01:92`'s `normals` sentence, the index READMEs' "how the plugin *is* today" pointers)
and the lint runs' own quotations; no new hit since run 03. The new record page
[05/08](../../05-phase3-grasshopper-components/08-broader-primitive-set.md) is a dated entry
whose "What was built" bullets describe the increment, the precedent of every increment; the
new `command_reference/01` sections prescribe (what the user sees and wires). One observation,
not a finding: the catalog's *index-is-an-ID* invariant — the constructor throws if a shape
sits anywhere but at its index, so a shape is only ever appended — lives in the code
(`PrimitiveCatalog.cs:101`) and in the record, and no design page states it; `04`'s families
bullet is where it belongs.

**(d) — 0 findings.** The scan found one page cited by nothing but its folder README:
[run 03](03-2026-10-03-linux-port-run.md), born in the previous run — exempt as born since the
last run, and cited by this one from here on. Every other non-README page under `docs/` has a
citation outside its index.

**(e) — 1 finding** (12, D-10 carried). The other DEFERRED rows were read against the code and
against § Current focus and stay unmet: `raw/` holds 6 files / 319 KB (D-38: 50 files or
1 MB); no command-reference default was found wrong against the source this session, so D-39's
trigger stands; D-04, D-05, D-06, D-09, D-19, D-23, D-27, D-32, D-45 have no canvas, consumer
or export behind them. Two observations handed on: **D-08** — DualC's version string is
`"dualc 0.5.0"` at `2fcd19f` against the pinned `"dualc 0.3.0"`, so the first re-vendor makes a
startup version assert meaningful for the first time (run 03 read the bumps as ABI, not
vocabulary, changes; that reading holds); **D-37** — the 0.4.0 re-vendor is subsumed by the
0.5.0 one Phase C needs, so its row lands or is dropped the day Phase C starts.

## What this run leaves

Twelve dispositions, all handed on: nine to the next ordinary session (1, 2, 4–9, 11 — design
prose, the `02` files table, the glossary's item range and list), three to the Phase C session
(3, 10, 12), which re-vendors the DLL at 0.5.0 and is the natural owner of every sentence
about what the ABI does and does not expose. The dated run-03 observation on the status line
stays where run 03 left it.

---

← Back to the [semantic-lint runs](README.md) · the [Docs layers index](../README.md) · the [Roadmap index](../../README.md).
