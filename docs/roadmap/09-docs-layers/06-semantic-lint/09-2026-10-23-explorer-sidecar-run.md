# Semantic-lint run 09 — after the explorer side-car

Run on 2026-10-23, the ninth run of [`/docs-semantic-lint`](README.md) on Boletus, owed by
the explorer side-car session ([roadmap 12](../../12-explorer-sidecar.md)): the Rhino-free
launcher half `src/Boletus.Core/LivePreview/` (five files), `LivePreviewComponent.cs`
rewritten, `build_native.py`, the csproj and CI, and four `design/` pages rewritten — the
`README` layering bullets, `04/01` the project, `04/07` the live-preview process model,
`08` the glossary. The session's work is in the working tree, not yet committed, so the run
reads the tree. Gate green before reading (`python3 scripts/check.py --docs --strict`, 26
passed, one informational line owed by the porous session's index row; the full gate green
with 213 tests).

| Commit | What | Date |
| --- | --- | --- |
| `4f4db6d` | last committed code change (`src/`, `tests/`) | 2026-10-22 |
| `5655049` | last committed `docs/design/` change | 2026-10-23 |
| working tree | this session's code and design edits, together | 2026-10-23 |
| DualC `4f02b51` | the pin — the gitlink of `external/DualC` | 2026-10-22 |

**Pages read:** `04/07` whole, against `LivePreviewComponent.cs` (the inputs, the `Viewer`
dropdown's values and default, the fallback warning, the atomic write and the
skip-if-unchanged, the kill-and-relaunch on a viewer change, the quarter-second wait for the
field view only, the editor's `Exited` handler and the UI-thread schedule, the no-relaunch
after a reported exit, the one-solve-late OBJ sweep, `RemovedFromDocument` / `ProcessExit`)
and against `ViewerLocator.cs`, `ViewerArguments.cs`, `ViewBounds.cs`,
`LivePreviewDocument.cs` (the spliced `view` line after the opening brace, the serializer
untouched, the invariant-culture flag value); `README` § the layering, the two side-car
bullets, against `build_native.py` (the editor built from the second tree against the DualC
pin, `windeployqt`, `native/x64/editor/`) and the csproj; `04/01` against the csproj's items;
`08` the rows Side-car, Explorer, Field view, View block, File-watch, Uniform push, The
native side, The pin. Plus the new usage page `command_reference/04-terminals/03-live-preview.md`,
whose inputs table and message strings were read against `LivePreviewComponent.cs`, and
`00-shared-behaviour.md`'s `Live Preview` rows. The DualCEditor facts the pages restate
(the folder watch, the 100 ms debounce, the camera kept, the file's values winning on
reload, no write-back) were verified against `~/Documents/DualCEditor` at `74023cf` during
the session's survey and are not re-read here.

## Findings

| # | Class | Finding | Docs | Code | Disposition |
| --- | --- | --- | --- | --- | --- |
| 1 | (a) | The glossary's **The pin** row, the design README's second side-car bullet and `04/01` speak of the second submodule `external/DualCEditor` as present and built; the gitlink is not in the tree — the `git submodule add` was refused by the session's tooling and is the owed first step of 12 #41. `STRUCTURE.md` and the `.gitmodules` row say the same. Until that one command runs, `build_native.py` prints its skip line and the `.gha` falls back to the field view, exactly as the pages describe for a machine without the editor. | `docs/design/08-glossary.md` row The pin · `docs/design/README.md` § the layering · `docs/design/04-grasshopper-plugin/01-the-project.md` | `.gitmodules` (one entry) · `scripts/build_native.py` `source_tree(... EDITOR_SUBMODULE ...)` | handed to [12 #41](../../12-explorer-sidecar.md#41-build-package-ci-the-second-submodule-qt-the-editor-folder-in-the-yak) — the submodule add makes the sentences true; no rewrite |
| 2 | (c) | A present-tense "today" about DualCEditor's state in the record (`the editor has no QSettings today`): a fact of the external repository at a commit, so it is pinned to that commit instead of dated by the reader's clock. | `docs/roadmap/07-upstream-coordination/04-dualceditor-explorer.md` § 11 item 4 | DualCEditor `74023cf` (`rg QSettings src/` empty) | fixed this session, in the working tree |

**(b) 0.** The code and the design pages were edited in the same working tree in the same
session; the last committed code change (`4f4db6d`) is older than the last committed design
change (`5655049`), so no committed page is behind committed code.

**(d) 0.** `02-proxy-preview.md` has no link outside its folder's README, but it was born
this session from the split of the terminals page (its heading is linked from the
command-reference README's task table through the folder); `03-live-preview.md`,
`12-explorer-sidecar.md`, `04-dualceditor-explorer.md` and `02-settled-continued.md` are
each linked from two or more pages.

**(e) 0.** D-05 (the `/MT` runtime: "before the first Yak intended for a machine other than a
dev box") — the `.yak` is still a CI artifact, and the editor's Qt runtime adds a second
dynamic runtime to the same question when that day comes; D-27 (slider scrubbing) — no such
workflow reported, the editor's dock gives the hand-driven tier; D-48 (single-file `.gha`) —
no request, and the folder is now ~25 MB; D-50, D-51 (porous build time) — no canvas over
2 s; D-55 (layout persistence) — born this session; D-38, D-39 — not met.

Count: (a) 1 · (b) 0 · (c) 1 · (d) 0 · (e) 0.

*(2026-10-23, later the same day — disposition of finding 1: closed. The owner ran the
`git submodule add`; `external/DualCEditor` is in the tree at `74023cf` and the sentences
the finding named are true as written.)*

---

← Back to the [semantic-lint runs](README.md) · the [Docs layers index](../README.md) · the [Roadmap index](../../README.md).
