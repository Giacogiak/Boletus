# 05 — Phase 3b: The async, manual `Write to File` exporter

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`; every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4).

## Implemented — async, manual `Write to File` exporter (2026-07-05)

### Why (the UX defect)

Manual Rhino testing surfaced the top UX defect of the old dual-purpose `Contour / Export` terminal:
on a heavy TPMS/lattice part the tiled-STL write ran **synchronously on the Grasshopper solve
thread** — a single blocking native P/Invoke (`dualc_field_export_tiled_stl`) — so the **entire
Rhino/GH UI froze for minutes**. Worse, because it was a normal terminal it wrote **automatically on
connect and on every slider nudge**, so an accidental heavy write was one drag away, and there was no
way to say "not yet — write when I'm ready". Grasshopper components are not meant to hold a
minutes-long blocking job on the solve thread.

The five concrete requirements (from the user) and how each is met:

| # | Requirement | How it's addressed (Phase A) |
|---|---|---|
| 1 | The write must be **async** — not freeze GH. | Runs on a dedicated background thread; `SolveInstance` returns immediately. |
| 2 | Show a **progress / readable running message**. | A "Writing… Ns" ticker under the component + in `Info`. (A true **%** bar is Phase C.) |
| 3 | **Manual launch** — never auto-run on connect / slider. | An on-canvas **Write** button is the *only* thing that launches; `SolveInstance` never does. |
| 4 | If busy and inputs change: report **BUSY**, offer abort/restart or wait. | BUSY detection + single-flight **queued restart** (wait-only; true mid-flight cancel is Phase C). |
| 5 | **Path mandatory**; bare names get an extension; **Format** (STL/3MF) + **Mode** (mono/tiled) selectors. | Mandatory Path; `Format`/`Mode` integer inputs; extension forced from Format. |

### Phased plan & status

The hard constraint that shapes the whole design: **the native export call cannot be interrupted from
.NET** — `Thread.Abort` is gone in net7, and the C ABI (`dualc_field_export` /
`dualc_field_export_tiled_stl`) takes **no cancellation token and no progress callback**. So a true
mid-flight abort and a real progress % cannot come from the managed side alone. The work was therefore
split into phases:

| Phase | Scope | Status |
|---|---|---|
| **A** | Boletus-only: async background write, manual Write button, "Writing… Ns" status, mandatory Path + Format/Mode selectors, BUSY detection + single-flight queued restart. No upstream change. | **DONE 2026-07-05** (code + unit tests; manual Rhino smoke pending — see Verification). |
| **C** | Real progress **%** + true cooperative **cancel**, via a small **upstream DualC** per-tile progress/cancel callback on the tiled writer; then a "Cancel ■" button state. Only tiled STL gets fine progress/cancel. | **DEFERRED** — full spec in [07 §7](../07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c). |

(There is no separate "Phase B" — a child-process exporter was considered and rejected: it would
reintroduce temp-OBJ mesh materialization, vendor a second native binary, and *still* need the same
upstream callback for progress. Phase C is strictly cheaper for the same benefit.)

### Architecture (Phase A)

**Two layers, the project's usual split.** The deterministic, Rhino-free half is
`Boletus.Core/Export/ExportPlan.cs` (unit-tested on the plain runner); the component
(`src/Boletus.Grasshopper/WriteToFileComponent.cs`) owns only the Grasshopper glue, the threading, and
the (unchanged) native calls.

**`ExportPlan.Resolve(path, format, mode, depth, tileDepth?)` → `ExportPlanResult`** — pure:
- **Extension from Format** (the source of truth): `Path.ChangeExtension(path, ".stl" | ".3mf")`. A
  bare name gains the extension; a conflicting typed extension (`part.3mf` with Format = STL) is
  replaced and a warning recorded.
- **Strategy table** → `ExportStrategy` (`TiledStl` | `Monolithic`):
  STL + Tiled → `TiledStl`; STL + Monolithic → `Monolithic`; **3MF + anything → `Monolithic`** (warns
  if Mode was Tiled — 3MF has no streaming writer).
- **Effective tile depth** = `tileDepth ?? max(1, depth − 2)`; warns if `≥ depth` (tiling degenerates
  to a single pass).
- **Hard errors** (throw → the component shows an error and refuses to write): empty path, unknown
  format/mode, parent directory does not exist.

**Component state machine** — one instance holds a small state machine guarded by a lock:

| State | Meaning | Leaves via |
|---|---|---|
| `Idle` | Nothing written yet this session (or reset). | Write click → `Running`. |
| `Running` | A background write is in flight. | Worker completion → `Done`/`Failed`. |
| `Done` | Last write succeeded. | Write click → `Running`; input change → still `Done` but status notes "inputs changed since the last write". |
| `Failed` | Last write threw. | Write click → `Running`. |

**Control flow — the key invariant is that `SolveInstance` NEVER launches work.** On every solve it
reads inputs, runs `FieldGraphValidator`, plans via `ExportPlan`, serializes the resolved graph
(`VolumeResolver.Resolve` → `ToJson`), and computes an **input hash**. Then:
- If `Running` and the hash differs from the launch-time snapshot → emit **BUSY** warning, publish the
  live "Writing… Ns" status, and **return without launching**.
- If a Write click is pending (`_runRequested`) and not `Running` → capture the snapshot, flip to
  `Running`, and launch the worker.
- Otherwise publish the last result (`Done`/`Failed`/`Idle`).

Only **`RequestRun()`** (called by the button's `RespondToMouseDown`) sets `_runRequested` /
`_restartRequested` and calls `ExpireSolution(true)` to trigger the solve that actually launches.

**Threading & re-entry.** The write runs on a **dedicated long-running thread**
(`Task.Factory.StartNew(…, TaskCreationOptions.LongRunning)`, *not* a pool thread — a minutes-long
blocking call would otherwise starve the ThreadPool). The **whole `DualcField` lifetime — create →
`Export`/`ExportTiledStl` → dispose — lives on that worker**, honoring the single-threaded-handle
gotcha; the managed `MeshBuffer`s handed to it are safe to cross threads. On completion the worker
marshals back with `RhinoApp.InvokeOnUiThread` → `OnPingDocument().ScheduleSolution(5, _ =>
ExpireSolution(false))` (the known-good Speckle async-component re-entry pattern) so the next solve
flips `Writing → Done` and publishes `Info`/`File`.

**Live status.** A UI-thread `System.Windows.Forms.Timer` ticks every second, updating the component's
`Message` to `Writing… Ns` and calling `Instances.RedrawCanvas()` — a cheap label redraw, **no
re-solve**. Stopped on completion.

**BUSY & single-flight (the "abort" story).** Because the native call can't be hard-cancelled, "abort
the previous run and launch a new one" is honored as a **single-flight queued restart**: a Write click
while `Running` sets `_restartRequested`; on completion the component immediately relaunches with the
then-current inputs. This guarantees **never two concurrent native writes** (which would risk OOM and
a half-written file) — the honest wait-then-restart semantics. A double-click, or a restart request
whose inputs are identical to what's already writing, is dropped.

**On-canvas button.** `WriteToFileAttributes : GH_ComponentAttributes` reserves a button strip under
the capsule (`Layout`), draws a `GH_Capsule` labelled **"Write ▶"** (→ "Writing…" while busy, via the
`Warning` palette) in `Render`, and hit-tests a left-click in `RespondToMouseDown` → `RequestRun()`.
Momentary (not a toggle) is what makes reqs #3/#4 fall out for free.

**Lifecycle.** `RemovedFromDocument` sets a `_removed` flag and stops the ticker; a native write
already in flight finishes harmlessly in the background and its completion callback no-ops (no
`ScheduleSolution` on a deleted component).

### Inputs / outputs (also the in-UI descriptions)

The inputs table (eight inputs, their defaults — Depth 6, Format 0, Mode 0, Tile depth
Depth − 2 — and the two outputs `Info` / `File`) is the usage contract and lives on the
component's command-reference page,
[04 § Write to File](../../command_reference/04-terminals.md#write-to-file). *(2026-09-21,
roadmap 09 Phase 4: the two tables that stood here moved there, where they are kept true
against the source.)*

Every input Description is **self-contained** and spells out its full option set (the project's
`gh-multichoice-input-docs` rule); the component's own description explains the button / async / BUSY
behavior.

### Constraints & known limitations (Phase A)

- **No true mid-write cancel** — a running native write always finishes; "abort" is wait-then-restart.
  (Phase C.)
- **Indeterminate progress only** — "Writing… Ns" elapsed time, not a %. (Phase C.)
- **Tiling is STL-only** — 3MF and monolithic STL hold the whole mesh in RAM.
- **Same-instance re-entrancy assumed** — GH does not re-enter one component instance concurrently; the
  single-flight guard + lock cover the rest.
- **New GUID `…033`** (not the old `…030`): an old canvas shows a loud "unrecognized component"
  placeholder rather than silently remapping wires onto the reordered inputs — including
  `examples/demo.gh`, which needs a manual rewire to the new component.

### Files changed (async Write to File)

| File | Change |
|---|---|
| `src/Boletus.Grasshopper/WriteToFileComponent.cs` | **new** — replaces `ContourExportComponent.cs` (**deleted**). The component + `WriteToFileAttributes` button + the Idle/Running/Done/Failed state machine + async worker + status ticker; fully self-documenting input/output descriptions. |
| `src/Boletus.Core/Export/ExportPlan.cs` | **new** — pure `ExportPlan.Resolve` (path/extension/strategy/tile planning) + `ExportStrategy` + `ExportPlanResult`. |
| `tests/Boletus.Core.Tests/ExportPlanTests.cs` | **new** — 12 tests for the planning table (extension override, strategy table, tile default + `≥Depth` warning, empty-path / missing-dir / unknown-format errors). |
| `docs/roadmap/05`, `07` (new §7), `roadmap.md`, `CLAUDE.md`, `STRUCTURE.md`, `docs/components.md` | recorded the increment; current-state test count 92 → **104**. |

No change to `Boletus.Core`'s native surface — `DualcField.Export` / `ExportTiledStl` and the
underlying P/Invokes are used unchanged; only the new pure `ExportPlan` was added to Core.

### Verification (async Write to File)

- **Automated:** `dotnet build Boletus.sln` → 0/0; `dotnet test tests/Boletus.Core.Tests` → **104/104**
  (was 92). The 12 new tests cover the pure `ExportPlan` planning **only** — extension resolution,
  the strategy table, tile-depth defaulting, and the error guards.
- **Pending manual Rhino smoke (the real acceptance gate — no unit test covers the thread/button/GH
  path, the same precedent as `LivePreviewComponent`):** on a heavy TPMS lattice, click **Write ▶** and
  confirm:
  1. the file writes while `Info` / the button tick **"Writing… Ns"**;
  2. dragging an **unrelated slider mid-write** keeps GH responsive (req #1);
  3. **editing the volume mid-write** shows **BUSY** and does *not* auto-restart (req #4);
  4. **Write-while-busy** queues exactly **one** restart that fires on completion (req #4);
  5. completion reaches **"Done → …"** — **not** a hang (this proves the `ScheduleSolution` re-solve
     fires; a hang here, with the file written fine on disk, is the one make-or-break failure mode);
  6. `Format = 3MF` + `Mode = Tiled` warns and writes a monolithic `.3mf`; an empty `Path` errors and
     writes nothing.


**Phase C unblocked (2026-10-02).** The upstream progress/cancel hook is available in DualC
ABI 0.5.0 (DualC #48); D-30 is PLANNED. What it changes and the Boletus steps are in
[07 § 7](../07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c).

### Phase C — cooperative cancel and a real percentage

**DONE (2026-10-05)** — code, Core tests, docs; the manual Rhino smoke test pending, on a
Windows machine that has first rebuilt the DLL at the pin (the re-vendor and the inventory of
what else the pinned DualC offers are 07 § 7 and § 9). The phase table above reads "Only tiled STL gets fine progress/cancel": that caveat did not
survive the upstream shape — every strategy cancels up to the end of its contour.

**What was built.** The usage contract is command reference 04 and the mechanism design 04,
both linked from the entry above; the record is what changed and why:

- **Core:** `DualcCancelTokenHandle` (a `SafeHandle` over DualC's token), `DualcProgress` /
  `DualcStage`, three `*_with_progress` overloads on `DualcField` over one `WithToken`
  helper that owns the token's lifetime rule, and `SupportsProgress`, the entry-point probe
  that makes the component run on the old DLL and the new library alike.
- **Component:** the `Cancelled` state, the `Cancel ■` / `Cancelling…` button, the
  per-stage label, cancel on removal, the Remark and the Phase-A fallback on an old
  library. The requirement table's row 4 ("offer abort/restart or wait") is met by a real
  abort; row 2 by the engine's percentage.
- **Chosen against the sketch:** cancel does not relaunch (one button, one meaning); the
  progress report carries its stage instead of a flat `(done, total)`. Both in
  [07 § 7 § Deviations](../07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c).
  The files are `STRUCTURE.md`'s rows and the commit.

**Verification (Phase C).**

- *Automated, Linux, the vendored `2fcd19f` library:* `dotnet build Boletus.sln` 0 warnings;
  `dotnet test` **171/171** (was 162; the nine new tests all run through the library). With `DUALC_FIELD_EXE` the `2fcd19f` CLI and `DUALC_SAMPLES_DIR` its samples the
  parity, round-trip and fixture tests ran for real — proven by sabotage, `DUALC_FIELD_EXE=
  /usr/bin/false` fails 49 of the 171. The golden counts held on the new library, so neither
  the marshaling nor the contour changed between `d6b2808` and `2fcd19f` for the analytic
  graph. The nine tests and their measurements: 07 § 7's dated entry.
- *Pending manual Rhino smoke (the acceptance gate for the thread/button path, as in Phase
  A), on Windows after the DLL rebuild:* (1) `Write ▶` → `Cancel ■`, the label ticking
  `tile i/T (p%)`; (2) `Cancel ■` ends `Cancelled` within a second, nothing at the path, no
  `.part`, a previous file untouched; (3) a 3MF export shows `sampling` → `contouring` →
  `writing file` and cancels during the first two; (4) an input change mid-write posts the
  new BUSY text, and a cancel does not restart; (5) deleting the component mid-write leaves
  nothing on disk; (6) on the *old* DLL, before the rebuild: the Remark, the `Writing…`
  button and the Phase-A queued restart — the one case testable on Windows today.

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
