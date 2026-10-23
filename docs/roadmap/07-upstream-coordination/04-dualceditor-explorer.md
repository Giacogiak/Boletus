# 07/04 — DualCEditor as the explorer side-car

Part of [07 — Upstream coordination](README.md). The one section Boletus asks of the
**DualCEditor** repository (`https://github.com/Giacogiak/DualCEditor`, the owner's; Boletus
never edits it from here), the way the other sections ask of DualC. The Boletus side is
[roadmap 12](../12-explorer-sidecar.md); DualC's own record of the editor and of the channel
it did not carry for Boletus is its hand-off page
([DualC 22](../../../../DualC/docs/roadmap/22-dualceditor-handoff/README.md): "a channel
that Boletus needs would be a new item" — this section is that item, addressed to the editor).

## 11. DualCEditor as the explorer side-car — *ask*

**PLANNED — asked 2026-10-23, open upstream.** Four changes in DualCEditor make it the
exploration-only window `Live Preview` launches; Boletus's side of each is in
[12 #39](../12-explorer-sidecar.md#39-core-the-launcher-abstraction-the-view-block-the-viewer-dropdown)
and lands or waits as noted.

**1. An `--explore` switch** (`QCommandLineOption("explore", …)` in `src/main.cpp`; the main
window takes a `bool explore`). When set, not created: File ▸ Open / Add / Save Scene / Save
As / Export Selected / Export Visible (Quit stays); the whole Edit menu (Undo / Redo too — a
reload would otherwise be an undo step the user can revert); the Operations menu; the
Transform dock; Rename and Delete (the Objects dock made non-editable: no F2, no inline
edit, no delete); the gizmo keys; drag-and-drop onto the window. Kept: the camera (orbit,
pan, dolly, `F`, click-select), the Objects dock's visibility toggles, the Parameters dock
retitled *"Parameters (tweaks — reset by the next Grasshopper solve)"*, the Log dock, the
Section toolbar and View ▸ Section, the status bar, the View menu's dock toggles. A
`.dcscene` positional exits 1 in this mode (nothing to persist). An optional `--title
"<text>"` sets the window title (default `"<file> — DualCEditor (explore)"`). One ctest
(`main_window_explore`, GL-skippable like `main_window`): the menus and docks absent, a
synthetic drop ignored, a file rewrite still reloads with the camera kept, a parameter edit
never touches the file. **Boletus waits** for this one before passing the two flags
([12 #42](../12-explorer-sidecar.md#42-the-explore-switch-at-the-new-pin-the-default-flipped)).

**2. Bounds through the graph document, not a flag.** An optional top-level
`"view": { "bounds": [ x0, y0, z0, x1, y1, z1 ] }` in the graph JSON, read in
`src/graph_file.cpp` before `root` goes to DualC's parser (which ignores unknown top-level
keys), carried with the object and used by `traceBounds` (`src/graph_scene.cpp`) **in place
of** the field's own padded bounds when present — which is also what lets a field with no
finite bounds (a bare TPMS) open at all. Six finite numbers, `min < max` per axis; otherwise
`view.bounds ignored: …` in the Log and the usual bounds. Live on every reload, in the same
atomically swapped file, no second watcher. One ctest: a bare gyroid with `view.bounds`
renders; a reload that changes only `view.bounds` reframes. Why not `--bounds` as
`dualc_field_view` has: launch-only, the gap Boletus had. **Boletus writes the key from
#39 on**; both viewers ignore it until the editor reads it.

**3. A deploy step and a licence file.** `qt_generate_deploy_app_script(TARGET DualCEditor …
NO_TRANSLATIONS NO_COMPILER_RUNTIME)` plus `install(TARGETS DualCEditor)`, so `cmake
--install` yields a runnable folder — or a documented `windeployqt` line; Boletus's
`build_native.py` calls `windeployqt` itself meanwhile
([12 #41](../12-explorer-sidecar.md#41-build-package-ci-the-second-submodule-qt-the-editor-folder-in-the-yak)).
A `LICENSE` file (MIT, as the README says); Boletus ships it under `editor/LICENSES/`
beside Qt's LGPL texts.

**4. Deferred: dock-layout persistence** — a `--profile <name>` under `QSettings` so the
explorer remembers its docks and window across launches (the editor at `74023cf` has no
`QSettings` and resets its layout every start). **DEFERRED** — the row is D-55 in the
[decisions index](../../decisions/README.md); trigger: a user who re-docks the explorer on
every launch.

**What Boletus does not ask.** A uniform-push channel from the canvas (D-27 stays DEFERRED:
the editor's dock gives in-process tweaks; a Grasshopper-driven push would be a new protocol
on both sides, and DualC D-66 placed every such channel in the editor), a GPU id buffer, a
colour hook — the editor's own open questions to DualC ([its questions page at the pin](https://github.com/Giacogiak/DualCEditor/blob/74023cf4e88d2696db8bd7a30eeb25dca5cd2eaf/docs/roadmap/06-dualc-questions.md), Q1–Q9).

---

← Back to the [Upstream coordination index](README.md) · the [Roadmap index](../README.md).
