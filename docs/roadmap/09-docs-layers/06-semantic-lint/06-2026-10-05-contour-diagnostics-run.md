# Semantic-lint run 06 — after the contour diagnostics binding

Run on 2026-10-05, the sixth run of [`/docs-semantic-lint`](README.md) on Boletus, owed by
`2ce3d98` (the `DualcDiagnostics` binding: `src/`, `tests/`, and four `design/` pages
rewritten in the same commit — the second code + design commit of the day, after
[run 05](05-2026-10-05-phase-c-run.md)). Gate green before reading (`python3 scripts/check.py
--docs --strict`, 25 passed, one index-staleness line report-only; the full gate, 28 checks,
green at the commit).

| Commit | What | Date |
| --- | --- | --- |
| `2ce3d98` | last code change (`src/`, `tests/`, `scripts/check_data.json`) | 2026-10-05 |
| `2ce3d98` | last `docs/design/` change (`01`, `04`, `06`, `07`) | 2026-10-05 |
| DualC `2fcd19f` | the pin; the sibling checkout at `6da2e6c` differs from it in docs and comments only | 2026-10-03 |

**Pages read:** every `docs/design/` page against the files each names, by two read-only
agents whose findings were each re-verified on both sides before entering the table — one on
the four pages the commit rewrote (`01`, `04`, `06`, `07`), an independent read of what the
session itself had written; one on the five it did not (`README`, `02`, `03`, `05`, `08`),
asked first whether the commit had made anything on them stale. Verified on the way: 21
`[DllImport]`s for 20 distinct exports plus the probe alias, the same 20 `dualc_` symbols
`nm -D` lists in the vendored `.so`; `DualcDiagnosticsNative` at 80 bytes in the header's
field order; the overload set (plain, `out`, progress, progress + `out` for `Contour` and
`Export`; plain and progress for `ExportTiledStl`); every quoted message string and its
level; `MaxProxyDepth` 7; the test floor 178 against 93 facts + 85 theory rows.

## Findings

| # | Class | Finding | Docs | Code / record | Disposition |
| --- | --- | --- | --- | --- | --- |
| 1 | (a) | The glossary's `DualcField` row names `SupportsProgress` and "an `IProgress` + `CancellationToken` overload" of each call, and neither `SupportsDiagnostics` nor the four `out DualcDiagnostics` overloads the commit added; `Version` was missing before it. The commit's spillover onto a page it did not rewrite. | `design/08-glossary.md:33` | `src/Boletus.Core/DualcField.cs:29,78,223,259,302,341` | fixed in this run's commit; a `DualcDiagnostics` row added |
| 2 | (a) | "The 0.5.0 twins take a real `DualcDiagnosticsNative` out-struct" — two of the three do; `dualc_field_export_tiled_stl_with_progress` has no `diag` in the ABI. A sentence the commit itself wrote. | `design/01-native-interop.md:75-77` | `src/Boletus.Core/NativeMethods.cs:220-224`; DualC `capi/dualc_c.h` (the tiled twin's comment) | fixed in this run's commit |
| 3 | (a) | "A monolithic call reports the first three stages" — a monolithic contour reports `Sample` then `Contour`; only a monolithic export adds `Write`. | `design/01-native-interop.md:124-125` | `src/Boletus.Core/DualcProgress.cs:5-8`, `DualcField.cs` (the contour overloads) | fixed in this run's commit |
| 4 | (a) | "`[DllImport("dualc_capi.dll")]`" — the import name has no extension (`"dualc_capi"`), which the same page's § The native-DLL resolver and design 01 both state. | `design/04-grasshopper-plugin.md:30-31` | `src/Boletus.Core/NativeMethods.cs:112`; `src/Boletus.Grasshopper/BoletusPriority.cs:44-46` | fixed in this run's commit |
| 5 | (a) | "A component creates one `DualcField` per solve, inside `SolveInstance`" — true of `Proxy preview` only; `Write to File` creates and disposes its field on the worker thread, on launch, as the same page's § Write to File says. | `design/04-grasshopper-plugin.md:48-50` | `src/Boletus.Grasshopper/WriteToFileComponent.cs:392-465`, `ProxyPreviewComponent.cs:102-166` | fixed in this run's commit |
| 6 | (a) | "Every test and every golden count runs at `maxDepth = 6`" — the catalog shapes contour at 5, and so does the new empty-contour case; the golden counts are all at 6. | `design/07-invariants-and-limits.md:57-58` | `tests/Boletus.Core.Tests/PrimitiveCatalogTests.cs:98`, `DiagnosticsTests.cs:36` | fixed in this run's commit |
| 7 | (a) | "`DualcField.Contour` is atomic — no mid-run abort" — the plain overload the proxy calls is; the progress overload cancels at the engine's checkpoints. | `design/07-invariants-and-limits.md:68-69` | `src/Boletus.Core/DualcField.cs:293-324` | fixed in this run's commit |
| 8 | (e) | D-10 — carried from runs 03, 04 and 05: the trigger ("the next re-vendoring, done by the script") fired twice by hand; the row says the owner decides at the Windows rebuild, which has not happened. Not resolved by the lint. | `decisions/README.md` D-10 | `native/README.md` § How to refresh | handed to the owner, at the Windows rebuild |

**(a) — 7 findings** (1–7), all fixed in this run's commit. One (2) is a sentence the commit
wrote; one (1) is the commit's spillover onto the glossary; the other five predate it and
survived run 05 — three of them (3, 4, 5) generalizations that were true before `Write to
File` and the progress overloads existed. Borderline, fixed with them because the fix is a
clause: `01` said the `out DualcDiagnostics` overloads throw `NotSupportedException` when
`SupportsDiagnostics` is false — the progress ones are gated by `SupportsProgress`; `01`'s
struct row said DualC's zero-on-entry is "the only zeroing" — the runtime zeroes the local
too, as the header asks of the host; `01`'s probe "writes nothing" — nothing but the error
text; `04`'s "open proxy" — the remark fires on `OutputWatertight` false, non-manifold edges
included; `04` named `SupportsDiagnostics` as the writer's gate where the cancellable path
takes the struct from the 0.5.0 twin; `05`'s "the three warnings" and "the component owns
only the threading and the native calls" read as complete sets without the engine's
empty-contour advisory; `07` listed where the golden counts are asserted without the two
newer test files. Borderline, left as read: `03`'s "absolute … path" (the resolver only
normalizes slashes; the caller's temp directory is absolute); the design README's rows for
`01`, `04` and `07` summarize fewer sections than the pages hold; `06`'s level lists are
examples, not enumerations (`Live Preview`'s two remarks and `Mesh → Volume`'s empty-mesh
error are not in them); `07`'s "skip, not fail" for the CLI-gated tests, which `return` and
so count as passed; `07`'s "a remark when the clamp bites" — the lower clamp to 1 is silent;
`04`'s "`SolveInstance` never launches work", true of auto-solve and qualified by its own body.

**(b) — 0 findings.** The newest code commit is also the newest design commit. The pages it
did not rewrite name no file it touched, except the glossary (finding 1).

**(c) — 0 findings.** The seed grep over `roadmap/` and `command_reference/` hits only the
lines runs 01–05 dispositioned; the new record [05/09](../../05-phase3-grasshopper-components/09-contour-diagnostics.md)
and the new reference strings add none. `07 § 9`'s rewritten row prescribes nothing — it
names the record.

**(d) — 0 findings.** The scan found one page cited by nothing but its folder README:
[run 05](05-2026-10-05-phase-c-run.md), born since the last run — exempt, and cited by this
one from here on.

**(e) — 1 finding** (8, D-10 carried). The other DEFERRED rows were read against the code and
against § Current focus and stay unmet: D-37 left the table today — its binding half landed
([05/09](../../05-phase3-grasshopper-components/09-contour-diagnostics.md)), its row is in
`01-settled.md`; D-08's trigger stands (the version string moves with the ABI, not the
vocabulary — the new probe is the second one by entry point); `raw/` holds 6 files (D-38); no
command-reference default was found wrong against the source (D-39); D-04, D-05, D-06, D-09,
D-19, D-23, D-27, D-32, D-45 have no canvas, consumer or export behind them. The D-44
observation of run 05 (the `.so` at the pin, the DLL behind it) stands until the Windows
rebuild.

## What this run leaves

Eight dispositions: seven fixed in this run's commit, one handed to the owner (D-10). Two
observations for the record, not findings: the same-day pair of runs 05 and 06 shows the
pattern principle 1 expects — a session that rewrites design pages against its own code
still owes an independent read, and that read found a sentence of the session's own (2) and
three older generalizations the rewrite did not reach (3, 4, 5); and the commit's own
`Reconstructed` / `DRIFT-PENDING` debt is zero. The next run is due by 2026-11-05.

---

← Back to the [semantic-lint runs](README.md) · the [Docs layers index](../README.md) · the [Roadmap index](../../README.md).
