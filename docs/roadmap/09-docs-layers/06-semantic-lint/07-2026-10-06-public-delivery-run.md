# Semantic-lint run 07 — after the publication

Run on 2026-10-06, the seventh run of [`/docs-semantic-lint`](README.md) on Boletus, owed by
the publication session ([roadmap 10](../../10-public-delivery/README.md)): the DualC submodule in
place of the vendored binaries, the build script, the CI workflow and five `design/` pages
rewritten in the preparation commit `fd8ede7` of the old history — the commit the fresh root
`407738d` carries as its tree. Gate green before reading (`python3 scripts/check.py --fast`,
27 passed; the full gate green at the root and in a fresh clone from GitHub).

| Commit | What | Date |
| --- | --- | --- |
| `902556b` | last code change (`scripts/check_data.json`; the code itself is the root `407738d`) | 2026-10-06 |
| `407738d` | last `docs/design/` change (`README`, `01`, `04`, `06`, `08`) | 2026-10-06 |
| DualC `2fcd19f` | the pin — the gitlink of `external/DualC` | 2026-10-03 |

**Pages read:** the five design pages the session rewrote, each sentence that names a file
read against it — `Boletus.Core.csproj`, `Boletus.Grasshopper.csproj`, the tests project,
`scripts/build_native.py`, `scripts/check.py` (`native-built`, `yak-version`,
`dualc_roots`), `.gitignore`, `.gitmodules`, `yak/manifest.yml`, `LivePreviewComponent.cs`,
`BoletusPriority.cs` — plus a sweep of every design page for the vendored era's words
(`vendored`, `Windows DLL`, `d6b2808`, `behind the pin`, `re-vendor`). Verified on the way:
the gitlink at `2fcd19f`; `native/x64/` and `native/linux-x64/` ignored; the `Exists` and
platform conditions on every native item; the `native-built` message; `dualc_roots` with the
submodule first; the glossary's `#32`–`#34`; the viewer-missing string equal on the code and
on `command_reference/00` and `04`.

## Findings

| # | Class | Finding | Docs | Code / record | Disposition |
| --- | --- | --- | --- | --- | --- |
| 1 | (a) | "The shipped payload is exactly `Boletus.Grasshopper.gha`, `Boletus.Core.dll`, `dualc_capi.dll` and `dualc_field_view.exe`" — the session had put the `dualc_field` copy item in `Boletus.Core.csproj`, so the CLI flowed to every consumer's output, the `.gha`'s folder included: a fifth file beside the plugin that Rhino never needs. The code was wrong, not the sentence. | `design/04-grasshopper-plugin.md:19-20`; `STRUCTURE.md` § Build targets | `src/Boletus.Core/Boletus.Core.csproj` (the two `dualc_field` items) | fixed in this run's commit — the items moved to `tests/Boletus.Core.Tests/Boletus.Core.Tests.csproj`, the only consumer; the test output still holds the CLI, the `.gha` folder is the four files again |
| 2 | (a) | "the Windows DLL that predates 0.4.0 still serves every original entry point" — there is no such DLL any more; both platforms build from the one gitlink. The generalization that holds is about any library built from an older commit. | `design/01-native-interop.md:80` | `external/DualC` (gitlink), `scripts/build_native.py` | fixed in this run's commit |
| 3 | (a) | "the DLL is pinned and refreshed by commit … reaching a new DualC op always means re-vendoring the DLL" — a page the session did not rewrite, still describing the committed-binary mechanism; the pin is the gitlink and the step is a bump plus a rebuild. | `design/07-invariants-and-limits.md:109-111` | `.gitmodules`, `scripts/build_native.py`, `native/README.md` § The pin | fixed in this run's commit |

**(a) — 3 findings** (1–3), all fixed in this run's commit; one of them (1) in the code.
**(b) — 0.** The newest code commit (`902556b`, the gate's data file) is newer than the
design pages by one commit that touched no file a design page names; the code the pages
describe is the root's tree, the same commit as the pages.
**(c) — 0.** The seed search over `roadmap/` and `command_reference/` hits only the roadmap
index's own pointer sentence ("for how the plugin *is* today see design"), which is a link,
not a description. The new block 10 and the new rewritable sentences in `native/README.md`
describe the mechanism in the present tense where the design layer links them
(`design/01` § Where the DLL is found, `design/06` § Provenance by commit).
**(d) — 0.** `10-public-delivery/README.md` is cited by eight pages, the raw plan by block 10 and
its index row; no page under `docs/` lost its last citation.
**(e) — 0 met.** D-10 (carried from runs 03–06) is settled: `scripts/build_native.py` is the
script the trigger asked for. D-05's trigger — "before the first Yak intended for a machine
other than a dev box" — is approaching, not met: the `.yak` exists as a CI artifact and its
first target is the owner's own Windows machine (the Rhino smoke tests, § Next up); the
zero-prerequisite build stays DualC-side. D-09's trigger (a third project consuming the DLL)
did not fire: the test project copies the CLI, not the library. D-48 is new and its trigger
is a user who has not appeared.

The next run is due by 2026-11-06, or after the next session that edits both `src/` and
`docs/design/` — the Windows smoke tests on the CI `.yak`, if they change either.

---

← Back to the [semantic-lint runs](README.md) · the [docs-layers record](../README.md).
