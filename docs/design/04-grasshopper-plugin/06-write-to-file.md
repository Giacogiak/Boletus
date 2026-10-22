# `Write to File` — the threading model

`WriteToFileComponent.cs` is **export-only** (outputs: a status string and the written path;
no mesh) and **never writes on auto-solve**. The deterministic half — path, extension,
strategy, tile depth — is `ExportPlan` in Core ([05](../05-export-planning.md)); the component
owns the Grasshopper glue, the threading and the native calls — the `DualcField.Export` /
`ExportTiledStl` overloads with a progress sink and a cancellation token when the library
supports them (`DualcField.SupportsProgress`), the plain ones otherwise
([01 § Cancel and progress](../01-native-interop.md#cancel-and-progress--the-050-twins)). The
two monolithic paths also take the engine's `DualcDiagnostics` when the library has them —
through the 0.5.0 twin on the cancellable path, through the 0.4.0 twin otherwise when
`DualcField.SupportsDiagnostics`: a completed write whose contour was empty — the file
holds the engine's one placeholder triangle — carries a warning into the next solve's `Done`
state, beside the status string; the tiled writer has no diagnostics in the ABI and reports
nothing, and the warning exists only on success, so cancel and restart never see it.

- **State machine.** One instance holds `RunState` — `Idle`, `Running`, `Done`, `Failed`,
  `Cancelled` — behind a lock, with the input hash captured when the running write
  launched and, on the cancellable path, the running write's `CancellationTokenSource`.
- **`SolveInstance` never launches work.** On every solve it reads the inputs, runs the
  validator, plans, resolves the graph (`VolumeResolver` → `ToJson`) and hashes it. If
  `Running` and the hash differs from the launch snapshot it posts **BUSY** and returns; if
  the button set `_runRequested` and nothing is running, it snapshots, flips to `Running`
  and launches; otherwise it publishes the last result. On a library without the cancel
  ABI it also posts a Remark saying so, on every solve.
- **The button.** `WriteToFileAttributes : GH_ComponentAttributes` reserves a strip under
  the capsule, draws a `GH_Capsule` and hit-tests a left click into `RequestRun()`.
  Momentary, not a toggle. Idle it reads `Write ▶` and a click sets `_runRequested` and
  expires the solution; while `Running` it reads `Cancel ■` (`Cancelling…` once clicked) and
  a click cancels the token source — the registration inside Core forwards that into the
  native token from the UI thread — and only redraws, because the completion callback
  schedules the solve. On the older library it reads `Writing…` and a click sets
  `_restartRequested`.
- **The worker.** A **dedicated long-running thread** (`Task.Factory.StartNew` with
  `TaskCreationOptions.LongRunning`, not a pool thread — a minutes-long blocking call would
  starve the pool) runs the whole create → export → dispose lifetime. DualC delivers progress
  on that thread (the one that made the P/Invoke), and the component's sink turns each
  report into one string in a volatile field — `tile i/T (p%)`, `sampling p%`,
  `contouring p%`, `writing file` — with no marshaling. On completion the worker marshals
  back with `RhinoApp.InvokeOnUiThread` → `OnPingDocument().ScheduleSolution(…,
  ExpireSolution(false))`, so the next solve flips `Running` → `Done` / `Failed` /
  `Cancelled` and publishes the result; an `OperationCanceledException` is the `Cancelled`
  outcome, with nothing on disk.
- **The ticker.** A UI-thread `System.Windows.Forms.Timer` updates the component `Message`
  once a second — `Writing… <progress> · Ns`, or `Writing… Ns` before the first report, or
  `Cancelling… …` after a cancel click — and redraws the canvas: a label redraw, no re-solve.
- **Single flight and cancel.** Never two concurrent native writes on one component
  ([07](../07-invariants-and-limits.md#single-flight-writes-and-the-cancel-constraint)). A
  cancel is a cancel: the write ends `Cancelled` and the user clicks `Write ▶` again for a
  fresh one with the current inputs — no automatic relaunch, so a click meant to stop a
  runaway write never starts another. On the older library "abort" is the **queued
  restart**: a click while `Running` sets `_restartRequested` and the completion callback
  relaunches with the then-current inputs; a restart whose inputs equal the running snapshot
  is dropped.
- **Lifecycle.** `RemovedFromDocument` sets `_removed`, stops the ticker and cancels the
  running write, which leaves nothing at the path; on the older library the write finishes
  harmlessly. Either way its callback no-ops.

No unit test covers the thread, button or Grasshopper path — Rhino cannot run headless — so
the acceptance gate is a manual Rhino smoke test, the same precedent as `Live Preview`.

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
