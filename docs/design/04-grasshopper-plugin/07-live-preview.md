# `Live Preview` — the process model

`LivePreviewComponent.cs` launches **`dualc_field_view.exe`**, built beside the `.gha`, as a separate
child process on a file: the true-fidelity preview, because the viewer sphere-traces the exact
field with RAM bounded by the window rather than the lattice. The exe is found beside the
`.gha` with the same assembly-directory idiom the DLL resolver uses. Each solve serializes
the volume — through `MeshMaterializingResolver` into a **per-instance temp directory**
(`%TEMP%\Boletus\LivePreview\<InstanceGuid>`, so two components never collide) — and writes
`graph.json` **atomically** (a `.tmp` then `File.Move` with overwrite, so the watcher never
sees a half-written file), skipping the write when the JSON is unchanged. The viewer polls
the file's mtime and reloads on change — DualC's own feature, recorded in its
[12/03 § Disk file-watch](../../../../DualC/docs/roadmap/12-field-graph-and-app/03-raymarch-app.md#disk-file-watch-auto-reload)
— so every canvas change reaches the window with **no keypress and no IPC**; every change
pays a shader recompile, and the per-parameter uniform push that would avoid it is a deferred
optimisation ([decisions](../../decisions/README.md)). Optional Min / Max become `--bounds`. The
viewer keeps its **own** orbit/dolly camera; Rhino's camera is not bound. `On` toggles the
window; `RemovedFromDocument` kills the process and deletes the temp directory, best-effort,
while `AppDomain.ProcessExit` only kills the process. No unit test covers the process or GL path.

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
