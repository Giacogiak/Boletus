# DualCEditor as the Live Preview side-car — assessment and plan

## Context

`Live Preview` launches DualC's `dualc_field_view.exe` (keyboard + console UI, no on-screen
widgets) on a temp `graph.json`; DualC's mtime file-watch is the whole update channel
(D-26). The owner's new Qt 6 app **DualCEditor** (`~/Documents/DualCEditor`, public, MIT,
46 commits, plan P00–P07 done 2026-10-14) has the exploration UI the viewer lacks: a section
toolbar (X/Y/Z, −/+), an Objects dock (visibility), a Parameters dock (per-uniform spin
boxes, no recompile), a Log dock, a status bar with compile/frame timing, async shader
compile (window never freezes), adaptive preview. DualC itself decided (DualC D-66,
2026-10-19) that **all UI is DualCEditor's** and `dualc_field_view` stays a reference
viewer, and closed the Boletus uniform-push ask (DualC D-25 / Boletus D-27) as "delivered
in the editor's own process; a channel Boletus needs would be a new item". So the UI the
user wants cannot grow in the current viewer; it lives in the editor.

Goal: use DualCEditor as an **exploration-only** side-car (Grasshopper is the single input,
the editor never edits or persists the model), bundled in the `.yak` in the most convenient
way, with an honest assessment of lighter alternatives.

**Pre-existing gate failure (not this plan's):** the working tree carries today's
uncommitted porous-shell session; `docs/roadmap/11-porous-shell.md` is not yet linked from
`docs/roadmap/README.md` (`indexes` check fails). Whoever closes that session links it.
This plan's numbering starts after it: block **12**, items **#39+**, decisions **D-52+**.

## Facts the plan rests on (verified in the three repos)

- **Channel already compatible.** The editor watches a `.json`/`.fld` file *and its folder*
  (`QFileSystemWatcher`, 100 ms debounce, reload only on byte change, camera and section
  state kept) — Boletus's write-`.tmp`-then-`File.Move` is exactly the pattern it handles.
  No IPC exists (no sockets, pipes, stdin). `.dcscene` and mesh files are not watched.
- **Never writes back.** The editor writes only on explicit Save Scene (`.dcscene`) or
  Export; no autosave, no QSettings. On reload "the reloaded object takes the file's
  parameter values" — a Parameters-dock tweak is overwritten by the next Grasshopper solve.
  That is the one-way semantics we want, for free.
- **No read-only mode.** Edit menu, Operations menu, Transform dock, gizmo (G/R/S),
  Rename/Delete, drag-and-drop, Save, Export are always on
  (`src/main_window.cpp:126-331`). Docks have `objectName`s and `toggleViewAction`s; panels
  only emit signals; all edits go through `SceneDocument`'s `QUndoStack`.
- **No bounds channel, and unbounded fields fail.** `traceBounds`
  (`src/graph_scene.cpp:23-32`) throws "the field has no finite bounds" — a bare TPMS (the
  Min/Max case) cannot open in the editor today. No `--bounds` flag exists.
- **Not the C ABI.** The editor links DualC's C++ example libs (`dualc_examples_fieldglsl`)
  by FetchContent at DualC `2fcb6ee` (109 commits behind Boletus's pin, no `porous` op) or
  by `-DDUALCEDITOR_DUALC_DIR=<tree>` (add_subdirectory). Its renderer is a port of
  `raymarch_gl`; it patches DualC's trace shader colour line by string match and fails
  loudly on a pin bump that changes it.
- **Deployment.** Qt 6 Widgets (Core, Gui, Widgets, OpenGL, OpenGLWidgets), LGPL, dynamic,
  `find_package` only. No `windeployqt`/`install()`/CPack in its CMake; no LICENSE file
  (README says MIT). Binary 3.6 MB; with Qt ≈ 25 MB on disk (≈ 10 MB zipped, to measure)
  if `opengl32sw.dll` (~20 MB) and `d3dcompiler` are excluded. Today's `.yak` is 0.9 MB.
- **DualC's parser ignores unknown top-level JSON keys** (only `root` is read; unknown keys
  *inside a node* are rejected). The C ABI and `dualc_field_view` both consume the same file.
- Boletus today: `LivePreviewComponent.cs` (257 lines), known gaps — Min/Max only reach
  the viewer at launch (unchanged JSON skips the write), `mesh_*.obj` pile up per solve,
  "vendored" wording stale.

## Assessment — options and the recommendation

| Option | Verdict | Why |
| --- | --- | --- |
| **A. Bundle**: submodule `external/DualCEditor`, built on Windows against Boletus's DualC pin, `windeployqt`, shipped as `editor/` inside the `.yak` | **Recommended** | One DualC per `.yak` (the `porous` proof); one install for the user; the pin is a gitlink like D-47. Costs: Qt in the Windows CI job (~3 min cold, <1 min cached), a ~25 MB `.yak`, four small upstream changes in DualCEditor. |
| B. Fetch a prebuilt editor artifact per tag | Rejected | Its embedded DualC is at *its* pin → graph-dialect drift exactly when a new source lands (porous today). A second provenance mechanism beside the submodule. |
| C. Optional external install (user installs the editor; Boletus finds it on PATH) | Rejected as the product path; **kept as a dev override** (`BOLETUS_EDITOR_EXE`) | Same drift as B; two installs. The override costs nothing and serves co-development, like `DUALC_ROOT`. |
| D. Stay on `dualc_field_view`, improve it | Rejected for UI; **its hygiene fixes land anyway** | DualC D-66 blocks UI growth there; a Boletus-side control panel (Eto/WinForms) has no channel to the viewer — it would need the stdin/`--command-port` protocol DualC closed. |
| E. A separate thin `dualc_explore` exe composed from `dualceditor_core` without the editing docks | Rejected | A second `main()`, a second dock composition, a second deploy; the watch/reload/section/status machinery all lives in `MainWindow`, so it would be refactored, not reused. An `--explore` flag is ~60 lines + one ctest. |

**Recommendation: A, with an `--explore` flag in DualCEditor's `MainWindow`, `dualc_field_view`
kept shipped as the fallback/alternate viewer** (zero regression; DualC keeps it as the
reference viewer; dropped only after the smoke test passes on two machines — a later
decision).

**Input policy (the boundary rule, to be written as D-52):** Grasshopper is the only input.
The editor receives one file per `Live Preview` instance and nothing flows back: no Save,
no Export, no scene; Parameters-dock tweaks and the section plane are transient and are
overwritten (parameters) or kept (camera, section) across the next solve's reload. Bounds
ride *in the file* (a `view` block), so a Min/Max change is live too.

**Licensing:** Qt LGPL v3 linked dynamically, DLLs shipped unmodified beside the exe, with
the LGPL text and a notice (`editor/LICENSES/`); Boletus's own code stays MIT. DualCEditor
must add its LICENSE file.

## Part 1 — The DualCEditor hand-off (upstream; Boletus never edits that repo)

Filed in Boletus as roadmap **07 § 11** on a new child page
`docs/roadmap/07-upstream-coordination/04-dualceditor-explorer.md` (the 07 README is near
the cap), cross-linking DualC's `external/DualC/docs/roadmap/22-dualceditor-handoff/README.md`.
The ask, precise enough for the owner to implement in `~/Documents/DualCEditor`:

1. **`--explore` flag** (`QCommandLineOption("explore", …)` in `src/main.cpp`; `MainWindow`
   ctor gains `bool explore`). Not created when set: File ▸ Open / Add / Save Scene / Save
   As / Export Selected / Export Visible (Quit stays); the whole Edit menu; the Operations
   menu; the Transform dock; `renameAction_` / `deleteAction_`; `ObjectsPanel::setEditable(false)`
   (no F2, no inline edit, no delete); `Viewport::setGizmoEnabled(false)`;
   `setAcceptDrops(false)`. Kept: camera (orbit/pan/dolly/F/click-select), Objects dock
   visibility, Parameters dock retitled *"Parameters (tweaks — reset by the next Grasshopper
   solve)"*, Log dock, Section toolbar + View ▸ Section, status bar, View dock toggles. A
   `.dcscene` positional exits 1 in this mode. Optional `--title "<text>"` for the window
   title (default `"<file> — DualCEditor (explore)"`). ctest `main_window_explore`: menus
   and docks absent, a synthetic drop ignored, a rewrite still reloads with the camera
   kept, a parameter edit never touches the file.
2. **Bounds channel — the `view` block, not a flag.** An optional top-level
   `"view": {"bounds": [x0,y0,z0,x1,y1,z1]}` in the graph JSON, read in
   `src/graph_file.cpp` before `root` goes to DualC, carried into the object and used by
   `traceBounds` *instead of* the field's padded bounds when present (this is also what makes
   a bare TPMS traceable). Six finite numbers, min < max per axis, else `view.bounds
   ignored: …` in the Log and the fallback. Live on reload (closes the launch-only gap),
   same atomic file, no second watcher. ctest: a bare gyroid + `view.bounds` renders; a
   reload changing only `view.bounds` reframes. Why not `--bounds`: launch-only.
3. **Deploy + licence**: `qt_generate_deploy_app_script(TARGET DualCEditor … NO_TRANSLATIONS
   NO_COMPILER_RUNTIME)` + `install(TARGETS DualCEditor)` so `cmake --install` yields a
   runnable folder (or a documented `windeployqt` line — Boletus can call it itself);
   a `LICENSE` file (MIT).
4. **Deferred (D-55)**: `--profile <name>` QSettings dock-layout persistence; trigger: a user
   who re-docks on every launch.

**Sequencing:** Boletus Part 2 steps 1–4 and Part 3 land *before* any upstream change (the
editor without `--explore` is the transitional state: the full editor, which still never
writes back — documented as such, `Viewer` default stays `Field view`). Step 5 (passing
`--explore`/`--title`, default flipped to `Editor`) waits for the editor pin that has items
1–2; the `.yak` folder needs only item 3 (or Boletus's own `windeployqt` call).

## Part 2 — Boletus code (order of landing)

**1. Core, Rhino-free, Linux-tested** — new folder `src/Boletus.Core/LivePreview/`:
- `ViewerKind.cs`: `enum ViewerKind { FieldView = 0, Editor = 1 }`.
- `ViewBounds.cs`: six doubles, `IsValid` (finite, min < max).
- `ViewerLocator.cs`: `Locate(pluginDir, editorOverride)` → `FieldViewExe =
  <dir>/dualc_field_view.exe`, `EditorExe = override ?? <dir>/editor/DualCEditor.exe`, with
  `Exists` flags; the override is `BOLETUS_EDITOR_EXE`.
- `ViewerArguments.cs`: `Build(kind, graphPath, bounds, title, explore)` — FieldView:
  `[graph, "--bounds", "x0,…,z1"]` (unchanged); Editor: `[graph, "--explore"?, "--title",
  t?]` (bounds ride in the file). Invariant culture.
- `LivePreviewDocument.cs`: `Compose(FieldNode root, ViewBounds? bounds)` — calls
  `FieldGraphSerializer.Serialize(root)` (`src/Boletus.Core/FieldGraph/FieldGraphSerializer.cs:27`)
  and, only with bounds, splices one line `  "view": { "bounds": [ … ] },` after the opening
  brace. **`FieldGraphSerializer` is untouched**: its byte-match with DualC's `--dump-json`
  is an invariant.

**2. Tests** `tests/Boletus.Core.Tests/LivePreviewTests.cs` (raise the floor in
`scripts/check_data.json`): `Compose` without bounds == `Serialize` byte for byte; with
bounds the `view` line appears once and the rest is byte-equal; the C ABI accepts the
composed text (`DualcField` create-from-JSON through the existing wrapper) and the field's
own bounds are unchanged (the key did not leak); `dualc_field --dump-json` round-trip drops
`view` (skip if the CLI is absent, the existing idiom); `ViewerArguments` for both kinds;
`ViewerLocator` with/without override; invalid `ViewBounds` rejected.

**3. `src/Boletus.Grasshopper/LivePreviewComponent.cs`:**
- New input 4 `Viewer` (`Vw`), Integer, `AddNamedValue("Field view", 0)`, `("Editor", 1)`
  — the `Write to File` `Format` idiom; default **0** until Part 2 step 5. Out of range →
  Error `Unknown viewer … (expected 0 = Field view, 1 = Editor).`
- Editor chosen but missing → Warning `DualCEditor not found under <dir>\editor\ — falling
  back to dualc_field_view.exe.`; neither present → the existing not-found Error reworded
  to name both.
- `json = LivePreviewDocument.Compose(root, bounds)` — the skip-if-unchanged now sees a
  Min/Max change (live bounds for the editor; the field view keeps launch-only `--bounds`,
  documented). A `Viewer` change while running kills and relaunches.
- **Exit handling:** keep `WaitForExit(250)` for the field view only (its parse failure is
  immediate). For the editor no blocking wait — it opens even on a bad graph and never
  exits by itself, so an exit is a launch failure (a missing Qt DLL exits `0xC0000135`):
  `EnableRaisingEvents = true`, on `Exited` capture code + drained stderr and, **marshalled
  via `RhinoApp.InvokeOnUiThread`**, `OnPingDocument()?.ScheduleSolution(50, _ =>
  ExpireSolution(false))` so the next solve posts `Viewer exited (code 0x…): <stderr>`.
  `CreateNoWindow = true` for the editor (GUI app), `false` kept for the field view (its
  console is its UI).
- Remark for the editor: `Explorer running — updates live as you edit the graph; parameter
  tweaks in its dock reset on the next solve. Orbit: left-drag · dolly: scroll · F frame.`
- Hygiene (any viewer): delete `mesh_*.obj` not referenced by the new JSON **on the
  following solve** (never the same solve — the debounce and the reload re-read meshes);
  a validator Error keeps the window on the last good graph (document; `On=false` closes);
  fix the "vendored" XML doc at `LivePreviewComponent.cs:27`.

**4. Build, package, CI** (land with step 3; the folder is Exists-conditioned so nothing
breaks without it):
- `git submodule add https://github.com/Giacogiak/DualCEditor external/DualCEditor`
  (`native/x64/editor/` is already under the gitignored `native/x64/`).
- `scripts/build_native.py`: `--editor` / `--no-editor` (default on when Windows **and** Qt
  discoverable via `Qt6_DIR` / `CMAKE_PREFIX_PATH`, else off with a one-line notice);
  `--editor-dir` / `$DUALCEDITOR_ROOT` with the `--dualc` precedence; `die("Qt 6 not found:
  set Qt6_DIR or pass --no-editor")` when forced on. **One CMake tree when the editor is on:**
  configure `external/DualCEditor` as the top level with
  `-DDUALCEDITOR_DUALC_DIR=<dualc> -DDUALC_BUILD_C_ABI=ON -DDUALC_BUILD_FIELD_VIEW=ON
  -DDUALCEDITOR_BUILD_TESTS=OFF` and build targets `dualc_capi dualc_field dualc_c_demo
  dualc_field_view DualCEditor` — DualC and geometry-central compile once (outputs under
  `build-native/_dualc/capi/Release/`, `_dualc/examples/Release/`, `bin/Release/`). With the
  editor off, the current DualC-top-level configure is unchanged. Then `windeployqt --release
  --no-translations --no-opengl-sw --no-system-d3d-compiler --no-compiler-runtime
  --skip-plugin-types imageformats,iconengines,networkinformation,tls,generic` into
  `native/x64/editor/`, copy the Qt LGPL text + DualCEditor LICENSE into
  `editor/LICENSES/`, run `DualCEditor --version` as the smoke, append editor commit, Qt
  version and folder size to `BUILD-INFO.txt`.
- `Boletus.Grasshopper.csproj`: a recursive item `..\..\native\x64\editor\**\*` →
  `editor\%(RecursiveDir)%(Filename)%(Extension)`, `Exists('…\editor\DualCEditor.exe')`.
- `.github/workflows/ci.yml` Windows job: `submodules: recursive`; second pin output
  `editor=$(git rev-parse HEAD:external/DualCEditor)`; `jurplel/install-qt-action@v4`
  (`6.8.*`, `win64_msvc2022_64`, `cache: true` — the editor's own `gate.yml` recipe); cache
  key adds the editor pin; staging `Copy-Item -Recurse "$out/editor" dist/boletus/editor`
  and asserts `DualCEditor.exe` + `Qt6Core.dll`; record the `.yak` size. Linux job unchanged
  (`--no-editor` by default; `native-built` and `dualc-links` untouched).
- Rule (in D-52): a DualC pin bump and the editor pin bump land in **one commit**; the
  editor's own CI at that DualC commit is the pre-check (its string-match shader patch is
  the known break).

**5. At the editor pin with `--explore` + `view`:** pass `--explore` and `--title "Live
Preview <nickname>"` (gated by the pin, never probed — the version-trap rule), flip the
`Viewer` default to `Editor`, update the reference page and the design page.

## Part 3 — Docs routing (AGENTS.md principles)

- `docs/raw/2026-10-22-dualceditor-sidecar-plan.md`: this plan as drafted.
- **Roadmap block 12** `docs/roadmap/12-explorer-sidecar.md` — "12 — The explorer side-car:
  DualCEditor bundled, `dualc_field_view` the fallback". Items: **#39** Core launcher
  abstraction, `view` block, `Viewer` dropdown, hygiene (Boletus-only, first); **#40** the
  hand-off (points at 07 § 11); **#41** submodule, `build_native.py --editor`, windeployqt,
  csproj folder copy, CI with Qt, the `.yak` with `editor/` (measured size); **#42**
  `--explore`/`--title` wired at the new pin, default flipped; **#43** the Rhino smoke test.
  `PARTIAL` as it lands; split into a folder if it outgrows the cap.
- **Decisions**: **D-52** (settled, DONE by choice): DualCEditor in explore mode is the
  Live Preview side-car, bundled as the submodule built against Boletus's DualC pin, Qt
  dynamic with its LGPL text; Grasshopper the single input; both pins bump in one commit.
  **D-53** (DROPPED): prebuilt-artifact fetch and external-install-only — dialect drift.
  **D-54** (settled): `dualc_field_view` stays shipped as `Field view` and the automatic
  fallback. **D-55** (DEFERRED, trigger above): layout persistence. **D-27**: row kept, a
  dated note that the editor's dock gives in-process tweaks and a GH-driven push is still
  a new channel (DualC D-66). **D-48** note: a single-file `.gha` would now embed ~25 MB.
- **Command reference** `docs/command_reference/04-terminals.md` § Live Preview: inputs
  table gains `Viewer (Vw) | Integer | 0 | 0 = Field view (dualc_field_view) · 1 = Editor
  (DualCEditor, explore mode)`; every new/changed Remark/Warning/Error string verbatim; the
  explorer's controls linked to DualCEditor's `docs/command_reference/01-viewport.md`,
  never restated; recipe T6 updated, a T8 "section a lattice". The page is at ~12.5 KB of
  15.4 KB — expect to split it into `04-terminals/` (the `01-sources/` precedent),
  headings verbatim.
- **Design**: `04-grasshopper-plugin/07-live-preview.md` rewritten for two viewers, the
  `view` block, the explore boundary, the exit handling; `design/README.md:55`;
  `08-glossary.md` (Side-car → the explorer; add Explorer, Field view, View block).
- **Structure/native**: `STRUCTURE.md` rows for `external/DualCEditor/`,
  `native/x64/editor/`, `src/Boletus.Core/LivePreview/*`, `LivePreviewTests.cs`, the
  build-targets paragraph; `native/README.md` output table + Qt prerequisite + `--editor`,
  `DUALCEDITOR_ROOT`, `BOLETUS_EDITOR_EXE`, the second pin; `yak/manifest.yml` comment and
  description; `docs/roadmap/06-phase4-distribution-and-packaging.md` package contents
  (dated note; already stale on the viewer exe).
- **Roadmap README**: § Current focus paragraph for 12; § Next up adds #43 to the Rhino
  smoke item and the pin-bump rule; Principal blocks row 12; Recent milestones (rolling five);
  **07 README** pages-table row for `04-dualceditor-explorer.md` and its status line.

## Verification

1. Linux: `dotnet test` with `LivePreviewTests` (floor raised); `python3 scripts/check.py`
   and `--docs --strict` green with block 12 linked (and block 11, owed by its session).
2. Windows CI: `boletus-yak` contains `editor/DualCEditor.exe`, the five Qt DLLs,
   `platforms/qwindows.dll`, `LICENSES/`; `DualCEditor --version` ran in the build step;
   size recorded in #41.
3. Manual Rhino smoke (#43) on the CI `.yak`: launch with `Viewer = Editor`; a slider drag
   reloads live with the camera kept; X/Y/Z section; a Parameters tweak overwritten by the
   next solve; `On=false` closes; two `Live Preview` components at once; bare gyroid with
   Min/Max traces and a Min/Max nudge reframes live; mesh volume and no `mesh_*.obj`
   pile-up after 20 solves; **porous volume parses in the explorer** (the single-pin proof);
   `Viewer = Field view` still works; editor folder deleted → the fallback Warning; Rhino
   closed → no orphan window.

## Risks

- Pin coupling (the editor's string-match shader patch) — mitigated by the one-commit rule
  and the editor's CI; `--no-editor` and the Exists-conditioned copy keep local builds alive.
- Qt install flakiness in CI — cache on; never `continue-on-error` (an editor-less `.yak`
  must be red, not silent).
- Footprint — measure; if the `.yak` passes ~30 MB drop `styles/` and revisit.
- Rhino 8 ships no Qt beside `Rhino.exe` and the editor is a separate process with its own
  DLLs — no DLL conflict expected; on the smoke list.

## Decisions taken in this plan (defaults the owner may overrule)

1. Bundle (A) rather than an optional external install (C) — convenience and one DualC pin.
2. `--explore` flag in `MainWindow` rather than a separate explorer executable (E).
3. Keep `dualc_field_view` shipped as `Field view` + fallback; drop it only after #43 on two
   machines.
4. Bounds via the `view` JSON block (live) rather than a `--bounds` flag (launch-only).
