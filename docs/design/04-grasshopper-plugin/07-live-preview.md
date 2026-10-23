# `Live Preview` — the process model

`LivePreviewComponent.cs` launches one of two side-car windows as a separate child process on
a file — the true-fidelity preview, because both sphere-trace the exact field with RAM bounded
by the window rather than the lattice. The `Viewer` input picks **`dualc_field_view.exe`**
(DualC's reference viewer, beside the `.gha`) or **`editor\DualCEditor.exe`** (DualCEditor
with its Qt runtime, in the `editor/` folder beside the `.gha`); `BOLETUS_EDITOR_EXE` points
at an editor elsewhere for co-development. The Rhino-free half is `Boletus.Core.LivePreview`:
`ViewerLocator` finds the two with the assembly-directory idiom the DLL resolver uses and
applies the one fallback rule — the editor asked for but absent gives way to the field view,
with a warning — `ViewerArguments` builds each command line, `ViewBounds` validates the
`Min` / `Max` box, and `LivePreviewDocument.Compose` writes the file's text.

**The file is the whole channel, one way.** Each solve serializes the volume — through
`MeshMaterializingResolver` into a **per-instance temp directory**
(`%TEMP%\Boletus\LivePreview\<InstanceGuid>`, so two components never collide) — as the
canonical graph plus, when `Min` / `Max` are set, one top-level `"view": { "bounds": [ … ] }`
line spliced after the opening brace; `FieldGraphSerializer` itself is not touched, and
without bounds the text *is* its output. DualC's parser reads `root` only and ignores other
top-level keys, so the C ABI and `dualc_field_view` take the same file unchanged; the editor
reads the key as its trace box. The file is written **atomically** (a `.tmp` then `File.Move`
with overwrite, so a watcher never sees a half-written file) and skipped when the text is
unchanged — a `Min` / `Max` change is a text change, so bounds are live for the editor, while
the field view gets them as `--bounds` at launch only. Both viewers watch the file (the
field view polls its mtime, DualC's
[12/03 § Disk file-watch](../../../../DualC/docs/roadmap/12-field-graph-and-app/03-raymarch-app.md#disk-file-watch-auto-reload);
the editor a `QFileSystemWatcher` on the file and its folder with a 100 ms debounce) and
reload on change keeping their camera — no keypress, no IPC. Nothing flows back: the editor
never writes to the file, and the values its Parameters dock changes are the shader's
uniforms, overwritten by the next reload. Every change pays a shader recompile; the
per-parameter push that would avoid it is a deferred optimisation
([decisions](../../decisions/README.md)). The editor's exploration switch and title are
constants in the component, passed only at the pin that has them, never probed.

**Lifetime.** `On` toggles the window; a change of `Viewer` kills and relaunches; the field
view's immediate exit (a parse error, an unbounded field without `Min` / `Max`) is caught by
a quarter-second wait and reported from its stderr; the editor gets no wait — it opens even
on a graph it cannot load and never exits by itself, so a non-zero exit (a missing Qt DLL)
is kept by its `Exited` handler and posted on the next solve, scheduled from the UI thread,
which does not relaunch until an input changes. A validator Error leaves a running window on
its last good graph. Temp OBJs a solve stops referencing are deleted one solve later, after
the viewers' debounce and reload. `RemovedFromDocument` kills the process and deletes the
temp directory, best-effort, while `AppDomain.ProcessExit` only kills the process. The
command lines, the document, the locator and the fallback rule are unit-tested; no test
covers the process or GL path.

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
