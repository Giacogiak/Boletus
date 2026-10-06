# Semantic lint — the Linux-port run

Run on 2026-10-03, the third run of [`/docs-semantic-lint`](README.md) on Boletus, owed because
`f8e4a47` edited both code and `docs/design/` (the Linux build, roadmap
[09/10](../10-linux-native.md)). The first run's full read of the nine design pages stands at
`663f2d2`, and the second run verified its fixes. Since then the code moved once, so this run
reads only what moved: the design passages edited since `ee91a5a` (the second run's HEAD),
every design page that names a file `f8e4a47` touched, the record and usage prose added since,
and every DEFERRED row. Docs and code line numbers are as read at HEAD `f8e4a47`. The gate was
green before the read (`check.py --docs`: 26 checks, 25 passed, 0 failed, 0 skipped; the 26th is
the report-only `index-staleness` note). It was also green after the read (`dualc-links`: 0
missing, 6 build outputs unbuilt).

**Inputs.** Last code change: `f8e4a47` (2026-10-03), which touched
`src/Boletus.Core/Boletus.Core.csproj` (the Linux `<None>` item) and
`tests/Boletus.Core.Tests/FieldGraphTests.cs` (`SamplesDir`). The only code commit before it,
since the first run, is `c4a1bfc`, read there. Last design change: also `f8e4a47`
(`design/01`). Before it, `cb138ad` (2026-10-02, 02 and 03, the loss audit's restored facts),
and `ee64c2d` (`design/README`). `doc-lag`: code 1 commit / 0.0 days ahead of `docs/` (the
same commit carries both).

**Pages read for (a):** the three design diffs since `ee91a5a`, each read against the code it names.
- `design/01:42-44`: one bare-named `[DllImport]` per entry point, probed as `.dll` / `.so`
  by OS. Against `src/Boletus.Core/NativeMethods.cs:70` (`Dll = "dualc_capi"`) and the 117
  tests passing on Linux with only the `.so` beside the assembly.
- `design/01:102-105`: the Linux copy, no resolver. Against `Boletus.Core.csproj:27-32`
  (the `<None>` item under `IsOSPlatform('Linux')`).
- `design/02:23`: `Ops.Tokens` lists every registered token. Against
  `src/Boletus.Core/FieldGraph/Ops.cs:32` (`Tokens => Registry.Keys`).
- `design/03:41-43`: `ContentHash` is a 64-bit FNV-1a over the two lengths, the vertex floats
  and the triangle indices, not the normals. Against `src/Boletus.Core/MeshBuffer.cs:49-67`
  (the FNV offset and prime; `MixInt` of both lengths, then the vertices, then the triangles;
  `Normals` never mixed).

All four hold.

## Findings

| # | Class | Finding | Docs `file:line` | Code `file:line` | Disposition |
| --- | --- | --- | --- | --- | --- |
| 1 | (b) | The layering diagram and the `dualc_capi.dll` bullet name only the Windows binary, under `native/x64/`, with VC++/UCRT as its only runtime dependencies. Since `f8e4a47` the same ABI is also vendored for Linux, with libstdc++/libc. | `docs/design/README.md:29`, `:43-46` | `native/linux-x64/libdualc_capi.so`; `src/Boletus.Core/Boletus.Core.csproj:27-32` | open — design rewrite in place |
| 2 | (b) | The glossary's **Vendored** row enumerates `dualc_capi.dll` and `dualc_field_view.exe` under `native/x64/` and nothing else. | `docs/design/08-glossary.md:52` | `native/linux-x64/libdualc_capi.so` | open — design rewrite in place |
| 3 | (b) | "The CLI-gated tests **skip, not fail**, when `dualc_field.exe` is absent" is incomplete. The CLI is found through `DUALC_FIELD_EXE` before the `D:\` default, and is named `dualc_field` on Linux. The sample-graph tests also return early when the samples folder is absent, and now take `DUALC_SAMPLES_DIR`. | `docs/design/07-invariants-and-limits.md:46-48` | `tests/Boletus.Core.Tests/CliParityTests.cs:21-27`; `tests/Boletus.Core.Tests/FieldGraphTests.cs:85-90` | open — design rewrite in place |
| 4 | (e) | D-10's trigger is "the next re-vendoring of the DLL, done by the script instead of by hand". `f8e4a47` vendored a native binary by hand: the Linux `.so`, with a nine-line recipe in `native/README.md`. That is not the DLL itself, so the trigger is met in substance and not in letter. The next re-vendor is Phase C's (D-30, PLANNED), and it is now two platforms' worth of manual steps. | `docs/decisions/README.md:30` | `native/README.md:81-97` | handed to the owner: reopen D-10 for both platforms, or record that the `.so` did not fire it |

**(a) — 0 findings.** The four passages above.

**(b) — 3 findings.** `f8e4a47` touched `native/`, `Boletus.Core.csproj` and
`FieldGraphTests.cs`. These design pages name them and are older than it: `README` (`ee64c2d`),
`07` and `08` (`990776c`). `06:93-97` (the DLL pinned by commit) still holds, because the `.so`
is pinned to the same commit. `07:95-99` (the version trap; a version bump or capability query
is an open upstream ask) still holds: DualC's header at HEAD declares no op-list or capability
query, and its 0.4.0 and 0.5.0 bumps were ABI changes, not vocabulary changes. `01` was
rewritten in the same commit and reads true (above). `04:13` names the Grasshopper csproj,
which `f8e4a47` did not touch.

**(c) — 0 findings.** Prose added since `ee91a5a`:
- the 09 post-close records `08`, `09` and `10`, and the Linux rows of the 09 README and the
  roadmap index: dated record and status;
- `command_reference/02` (`Bend`'s and `Displace`'s "where" lines) and `04` (Phase C waits on
  Boletus's binding of the upstream ABI): usage guidance and a status pointer, each linking
  D-30;
- `05/06`'s and `07/03 § 7`'s dated 2026-10-02 appends: record.

The seed grep hits only lines the first run dispositioned, plus `07/03:11` ("two things it
*cannot* do today"). That line is inside the 2026-07-05 entry's premise, the same reading run
01 gave `07/02:7`.

**(d) — 0 findings.** Pages that no non-README page links:
- `09/01-record-phase-0.md` and `09/02-record-phase-1.md`: the Phase records, read the same way
  by the first two runs;
- `09/09-loss-audit.md`: born 2026-10-02, after the last run, and linked by both indexes.

**(e) — 1 finding**, D-10 above. The other DEFERRED rows were read against the code and
§ Current focus:
- **D-04:** DualC has no tags or release artifacts. The Linux `.so` is a second build of the same
  consumer, not a second consumer.
- **D-08:** no vocabulary-keyed bump and no capability query, as under (b).
- **D-09:** the `.so` is copied by the same Core csproj; there is still no third consuming project.
- **D-37:** no Boletus feature yet needs a 0.4.0 entry point.
- **D-39:** this run found no wrong command-reference default.
- **D-05, D-06, D-19, D-23, D-25, D-27, D-32, D-38:** unchanged inputs.

D-30 is PLANNED, not DEFERRED.

**Observation, outside the five classes**, handed on: the first body line of `07/03 § 7`
(`docs/roadmap/07-upstream-coordination/03-export-callback-and-strut-sync.md:7`) still reads
**DEFERRED**. Its own 2026-10-02 append moves D-30 to PLANNED, and the decisions row says
PLANNED. `status-sync` does not compare `status_vocab_only` tables, so the gate cannot see the
disagreement.

| Class | (a) | (b) | (c) | (d) | (e) |
| --- | --- | --- | --- | --- | --- |
| Findings | 0 | 3 | 0 | 0 | 1 |

## Dispositions

*(2026-10-03, the same session, after the run's commit `f9cc204`:)* findings 1–3 are fixed in
place in `design/README.md:29,46-49`, `design/08-glossary.md:52` and
`design/07-invariants-and-limits.md:46-50`. The README edit also re-reads its index rows for
01–03 against those pages: they still hold, since the edits added facts inside a row's scope.
Finding 4 (D-10) and the § 7 status-line observation stay with the owner.

---

← Back to the [semantic-lint runs](README.md) · the [Docs layers index](../README.md).
