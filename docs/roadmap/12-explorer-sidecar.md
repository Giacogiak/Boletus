# 12 — The explorer side-car: DualCEditor bundled, `dualc_field_view` the fallback

**PARTIAL: DONE (2026-10-23, #39, #40); #41 PLANNED, #42 PLANNED, #43 PLANNED.** The record of
replacing the keyboard-and-console GPU viewer `Live Preview` launches with the owner's Qt 6
app **DualCEditor** (`https://github.com/Giacogiak/DualCEditor`, MIT, the plan P00–P07 that
built it done 2026-10-14) used as an **exploration-only** side-car: Grasshopper stays the
single input, the window explores and never edits. The assessment that chose this over the
lighter paths is the plan as drafted ([raw/2026-10-22](../raw/2026-10-22-dualceditor-sidecar-plan.md)),
approved by the owner on 2026-10-22; the decisions are
[D-52, D-54](../decisions/02-settled-continued.md) and [D-53, D-55](../decisions/README.md); the usage
contract [cmdref 04 § Live Preview](../command_reference/04-terminals/03-live-preview.md); the
mechanism [design 04 § Live Preview](../design/04-grasshopper-plugin/07-live-preview.md); the
upstream ask [07 § 11](07-upstream-coordination/04-dualceditor-explorer.md#11-dualceditor-as-the-explorer-side-car--ask).
Headings are frozen at ID + title; status, date and evidence live on the body lines.

**Why the editor, and why not less.** DualC decided (DualC D-66, 2026-10-19) that every
interactive UI concern is DualCEditor's and `dualc_field_view` stays its reference viewer;
it closed the uniform-push channel Boletus deferred (D-27, DualC D-25) as delivered inside
the editor's own process, so the exploration controls the owner wants — a section toolbar,
visibility, instant parameter tweaks, a log, timings, a window that never freezes on a
compile — cannot grow in the viewer Boletus ships, and a Boletus-side control panel has no
channel to it. What made the editor fit as a side-car without redesign: it already watches a
graph file *and its folder* (so the write-`.tmp`-then-rename swap is seen), debounces 100 ms,
reloads only on a byte change, keeps the camera and the section across a reload, never writes
back to the file, and gives the reloaded object the file's parameter values — a dock tweak is
overwritten by the next solve, the one-way semantics wanted, for free. What it lacked:
a read-only mode, a way to receive bounds (a field with no finite bounds is refused, so a
bare TPMS could not open), a Windows deploy step for Qt, a LICENSE file — the four asks of
07 § 11. The alternatives weighed and dropped (D-53): a prebuilt editor artifact or an
external install (the editor's embedded DualC would be at *its* pin, not Boletus's — the
`porous` op would not parse the day it landed), and a separate thin explorer executable (the
watch, reload, section and status machinery all live in the editor's main window, so it would
be refactored, not reused; an exploration switch is a few dozen lines and one test).

## #39 Core: the launcher abstraction, the `view` block, the `Viewer` dropdown

**DONE — 2026-10-23.** `Boletus.Core.LivePreview` is the Rhino-free half of the terminal:
`ViewerKind` (0 Field view, 1 Editor — the dropdown's numbers), `ViewBounds` (six finite
numbers, `min < max` per axis; the `--bounds` flag value), `ViewerLocator` (`dualc_field_view.exe`
beside the `.gha`, `editor/DualCEditor.exe` or `BOLETUS_EDITOR_EXE`; the one fallback rule —
the editor asked for and absent gives way to the field view, flagged), `ViewerArguments`
(the field view's `<graph> [--bounds …]`, the editor's `<graph> [--explore] [--title …]`, no
bounds flag) and `LivePreviewDocument.Compose` — `FieldGraphSerializer.Serialize` plus, with
bounds, one spliced top-level line `"view": { "bounds": [ … ] },`. The serializer is
untouched: its `--dump-json` byte-match stands, and `Compose` with no bounds *is* its text.
DualC's parser reads `root` only and ignores other top-level keys, so the same file feeds the
C ABI and `dualc_field_view` unchanged; the key rides in the one atomically swapped file, so a
`Min` / `Max` change is a text change and reaches a running editor live — the launch-only
bounds gap of the old component closed for the editor (the field view keeps `--bounds` at
launch, as before).

`LivePreviewComponent` gained the `Viewer` input (`Param_Integer` with named values, default
Field view until #42), the fallback warning, a kill-and-relaunch on a viewer change, and
the editor's exit path: no blocking wait (Qt start-up can outlast a solve; the editor opens
even on a bad graph and never exits by itself), `Exited` on a non-zero code keeps a report
that the next solve posts — scheduled from the UI thread with the `Write to File` idiom — and
does not relaunch until an input changes (a missing Qt DLL is one error, not a crash loop);
`CreateNoWindow` for the editor, the console kept for the field view. Hygiene for both
viewers: `mesh_*.obj` files a solve stops referencing are deleted *one solve later* (after
the viewers' debounce and reload, which re-reads meshes), a validator Error leaves the
window on its last good graph, the stale "vendored" wording went.

Tests (`LivePreviewTests`, the gate floor 196 → 213): `Compose` without bounds byte-equal to
`Serialize`; with bounds one `view` line and nothing else changed; invalid bounds rejected;
the C ABI contours the viewed and the plain document to the same counts with a view box
that is *not* the field's (the key never reaches the engine); `--dump-json` of both equal
and free of `view` (skips without the CLI); the two command lines; the locator with and
without the override; the fallback rule over an empty, a field-view-only and a complete
folder. The process and GL path stay a manual Rhino check (#43), as before.

**The docs this session reshaped (2026-10-23).** The terminals usage page was at the cap
and became the folder `command_reference/04-terminals/` (README + one page per terminal, the
`01-sources/` precedent, every in-repo link retargeted, the two folders added to the gate's
`index_roots`). The settled-decisions table tripped the cap with D-52 and D-54 and was
**continued, not split**: `decisions/02-settled-continued.md` takes the rows from D-52 on,
because four immutable `docs/raw/` files link `01-settled.md` by name and a same-numbered
folder would have broken links the gate reads and nobody may edit. The roadmap snapshot was
brought back under the cap by rule (D-43): the restructuring paragraph compressed to one
line per claim, the lint-run history left to [09/06](09-docs-layers/06-semantic-lint/README.md).

## #40 The hand-off to DualCEditor

**DONE — 2026-10-23 (filed; open upstream).** The four asks, each precise enough to
implement, are [07 § 11](07-upstream-coordination/04-dualceditor-explorer.md#11-dualceditor-as-the-explorer-side-car--ask):
an `--explore` switch (what it hides, what it keeps, the ctest), the `view.bounds` key read
before `root` goes to DualC and used as the trace box, a deploy step and a LICENSE file, and
the deferred layout persistence (D-55). Boletus writes the `view` block from #39 on — harmless
to both viewers until the editor reads it — and passes `--explore` / `--title` only at the
pin that has them (#42; gated by the pin, never probed — the version-trap rule).

## #41 Build, package, CI: the second submodule, Qt, the `editor/` folder in the `.yak`

**PLANNED.** `scripts/build_native.py` learnt the editor on 2026-10-23 — `--editor` /
`--no-editor` (default: on when Windows, Qt 6 found by `QT_ROOT_DIR` / `Qt6_DIR` /
`CMAKE_PREFIX_PATH` / `qmake` on PATH, and the editor tree checked out), `--editor-dir` /
`DUALCEDITOR_ROOT` with the `--dualc` precedence; **one CMake tree** when the editor is on
(`build-native-editor/`): the editor's `CMakeLists.txt` is the top level and adds DualC from
`-DDUALCEDITOR_DUALC_DIR=external/DualC`, so the C ABI, `dualc_field`, the viewer and the
editor compile from the same pin in one pass; then `windeployqt` (release, no translations,
no software GL, no D3D compiler, no VC runtime) into `native/x64/editor/`, the plugin folders
a Widgets window never loads pruned, `LICENSES/` (Qt's LGPL texts when the installation has
them, DualCEditor's when its tree has one, a `THIRD-PARTY.md` notice always), `DualCEditor
--version` as the smoke, the editor commit, Qt version and folder size in `BUILD-INFO.txt`.
The csproj copies the folder (Exists-conditioned on `DualCEditor.exe`); CI's Windows job
installs Qt 6.8 (`jurplel/install-qt-action`, cached — the editor's own gate recipe), keys
the native cache on both pins, stages `editor/` whole and asserts its four landmark files,
prints the folder's and the `.yak`'s size. **Owed:** `git submodule add
https://github.com/Giacogiak/DualCEditor.git external/DualCEditor` (the session's tooling
refused the clone; the pin to record is the editor's `74023cf`, the commit DualC's hand-off
cites), the first green Windows run, and its measured `.yak` size written here (the estimate:
≈ 25 MB on disk with Qt, against 0.9 MB before).

*(2026-10-23, later the same day: the submodule is in — `external/DualCEditor` at `74023cf`,
added at the owner's word; `.gitmodules` has its two entries. What remains owed is the first
green Windows run with Qt and the measured `.yak` size.)*

*(2026-10-23, evening — the first Windows run, `07cd2f1`: red at the native step after 39 s,
i.e. in the editor tree's configure, the annotations holding only the exit code. Proven on
Linux the same evening, the same one-tree configure and build (Ninja, Qt 6 from the
distribution, `-DDUALCEDITOR_DUALC_DIR` at the pin, the viewer on and off): the editor at
`74023cf` compiles and links against DualC `4f02b51`, the outputs land where the script
looks (`_dualc/capi/`, `_dualc/examples/`, `bin/`), `DualCEditor --version` answers. So the
break is Windows-only and before the compile. Hardened without the log: the CMake-side
paths the script passes (`-DDUALCEDITOR_DUALC_DIR`, `-DCMAKE_PREFIX_PATH`) are now forward-slash
(`as_posix`), the script prints its python and cmake versions, and CI keeps the native
step's output and turns its error lines into annotations on failure — the gate step's
idiom — so the next red run is readable from the public API.)*

*(2026-10-23, later — the cause, read from the second run's annotations (`19f3593`): the
configure died in geometry-central's fetch of Eigen, `fatal: remote error: GitLab is
currently unable to handle this request due to load` — a gitlab.com outage, hit twice
because each run started from an empty cache (a failed job saves none). Not a fault of
the editor tree: the same configure passed on Linux. CI now retries the native step up to
three times, 60 s apart, only when the log shows a fetch failure, and both jobs' native
cache carries `restore-keys` so a recipe or editor-pin edit reuses the last tree at the
same DualC pin, fetched sources included. The first green Windows run and the `.yak`
size stay owed.)*

*(2026-10-23, the third run, `f417878` — the retry carried two fetch failures and the
configure went through; the build then died in MSBuild, `MSB1009: Project file does not
exist`. The one-tree recipe is wrong on Windows by construction: the editor's CMakeLists
adds DualC `EXCLUDE_FROM_ALL`, and the Visual Studio generator writes an excluded target
into the solution only when a solution target depends on it — the editor's host libraries
are in, `dualc_capi`, `dualc_field`, `dualc_c_demo` and `dualc_field_view` are not, so
`cmake --build --target` has no project to hand MSBuild. Ninja builds any target in
`build.ninja`, which is why Linux never showed it. **Dropped for two trees**: DualC top-level
in `build-native/` exactly as before (the recipe proven since 10 #33), the editor in
`build-native-editor/` from the same submodule with only `DualCEditor` built there; DualC's
host libraries compile twice, the cache absorbs it, the one-pin rule holds (both trees
read the one gitlink). Rejected: `msbuild` on the `.vcxproj` files directly (needs MSBuild
on PATH, which `cmake --build` does not), the `_dualc/capi/dualc_capi` path trick in
`--target` (an undocumented branch of CMake's VS build driver), and a
`CMAKE_PROJECT_dualc_INCLUDE` hook flipping the directory's `EXCLUDE_FROM_ALL` (documented,
but clever, and a change to the editor's build from outside it).)*

## #42 The explore switch at the new pin; the default flipped

**PLANNED — waits on 07 § 11 landing upstream and a DualCEditor pin bump.** Pass `--explore`
and `--title "Live Preview <nickname>"` (`EditorExplore` / `EditorTitle` in the component),
flip the `Viewer` default to Editor, rewrite the usage page's "until the pinned editor has
its exploration switch" sentence and the design page. Rule (D-52): a DualC pin bump and a
DualCEditor pin bump land in **one commit**, the editor's own CI at that DualC commit the
pre-check — its renderer patches DualC's trace shader by string match and fails loudly on a
DualC change.

## #43 The Rhino smoke test

**PLANNED.** On the CI `.yak` of a green run, Rhino 8 on Windows: launch with `Viewer` =
Editor; a slider drag reloads live with the camera kept; `X` / `Y` / `Z` section; a
Parameters-dock tweak overwritten by the next solve; `On` = false closes; two `Live Preview`
components at once (two windows, two temp folders); a bare gyroid with `Min` / `Max` traces
and a `Min` / `Max` nudge reframes live; a mesh volume (the temp OBJ beside the graph) and
no `mesh_*.obj` pile-up after twenty solves; a porous volume parses in the explorer (the
single-pin proof); `Viewer` = Field view still works; the `editor/` folder deleted → the
fallback warning; Rhino closed → no orphan window. Also the check that Rhino 8 ships no Qt
beside `Rhino.exe` that the editor's own DLLs could collide with (a separate process with
its DLLs beside it; none expected).

---

← Back to the [Roadmap index](README.md).
