# 07 — Upstream coordination: The export callback and the strut-lattice sync

Part of [07 — Upstream coordination with DualC](README.md); every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4). Section numbers are the stable addresses.

## 7. Export progress + cancel callback — *unlocks true abort & a real progress bar (Phase C)*

**DEFERRED** — trigger at the end of the section. *(2026-09-21, roadmap 09 Phase 3: status line added as the first body line so the `decisions-index` gate finds this entry; the row is D-30 in the [decisions index](../../decisions/README.md).)*

**Status:** deferred. The `Write to File` terminal (2026-07-05) already runs the export **off the GH
solve thread** on a dedicated long-running thread, so a heavy tiled-STL no longer freezes Grasshopper,
and it shows an **indeterminate "Writing… Ns"** status. Two things it *cannot* do today, both blocked
on the same missing native hook:

- **A real progress bar.** `dualc_field_export_tiled_stl` / `dualc_field_export` are single blocking
  calls with **no progress callback** — the component can only show elapsed seconds, not a percentage.
- **A true mid-flight abort.** .NET cannot interrupt a blocking P/Invoke (no `Thread.Abort` in net7),
  and the C ABI takes **no cancellation token**. So "abort" degrades to **single-flight + queued
  restart**: a Write click during a running write is honored only *after* the current one finishes
  (never two concurrent native writes → no OOM / file contention). The user accepted this for the
  first cut.

**Want (upstream DualC):** a per-tile **progress + cancel** callback on the tiled writer (the natural
checkpoint — the tiled export already loops over sub-grid tiles), e.g.

```c
/* return non-zero to request cancellation; DualC unwinds at the next tile boundary */
typedef int (*dualc_progress_cb)(void* user, uint32_t tilesDone, uint32_t tilesTotal);

int dualc_field_export_tiled_stl_cb(DualcField*, const char* path, const ContourParams*,
    int tileDepth, dualc_progress_cb cb, void* user, char* err, int errlen);
```

returning a distinct status (e.g. `DUALC_CANCELLED`) when cancelled so a partial file is cleaned up.

**Boletus side (when it lands):**
1. Re-vendor `dualc_capi.dll` + bump [`native/README.md`](../../../native/README.md) provenance / version.
2. `Boletus.Core`: an `ExportTiledStl(path, params, tileDepth, IProgress<(int done,int total)>,
   CancellationToken)` overload marshaling the delegate (kept alive for the call; `ct` → non-zero
   return). No change to the diskless in-RAM mesh path.
3. `WriteToFileComponent`: the on-canvas button becomes **"Cancel ■"** while running, a real **%** in
   the `Message`, and a Write-while-busy **cancels at the next tile** then relaunches (replacing the
   queued-restart stopgap).

**Caveat:** only **tiled STL** gets fine progress/cancel (it has the per-tile loop); 3MF / monolithic
stay async-but-indeterminate unless upstream also instruments `dualc_field_export`.

**Trigger:** when the "Writing…" indeterminate status or the wait-only abort becomes a real workflow
pain on very heavy exports.

**Upstream DONE (2026-10-02) — the callback this section waits on is available.** DualC tracked
the ask as its item #48 ([DualC 14/05](../../../../DualC/docs/roadmap/14-c-abi/05-progress-and-cancel.md)): DONE on 2026-09-22, merged into DualC `main` on
2026-10-02 (`a6e11f6`), **ABI 0.5.0**. The owner confirmed on 2026-10-02 that it is the callback
D-30 waits on. Its shape differs from the sketch above, and is better for a .NET host:

- **Cancel is a token object, not the callback's return value** (DualC D-47):
  `dualc_cancel_token_create` / `_request` / `_is_requested` / `_destroy`. The host requests
  it from any thread, which maps one-to-one to `CancellationToken.Register`, and no managed
  delegate is called from a native worker.
- **Progress is a coarse callback on the calling thread only.**
- **`*_with_progress` twins** for contour, monolithic export and tiled-STL export. So the
  caveat above shrinks: 3MF and monolithic STL cancel too, up to the end of the contour.
  DualC D-45 defers checkpoints inside the monolithic writers.
- **A cancelled call returns rc 3 and leaves nothing at `path`.** Every export writes
  `path.part` and renames on success (DualC D-46), which is the partial-file cleanup asked
  for above.

The Boletus side, steps 1–3, is not started; step 2 takes the token shape. The
[decisions index](../../decisions/README.md) row D-30 moves from DEFERRED to **PLANNED**.

**Boletus side DONE (2026-10-05) — Phase C shipped; D-30 settled.** The three steps, as
they landed against the token shape (the record of the component is
[05/06 § Phase C](../05-phase3-grasshopper-components/06-write-to-file.md#implemented--async-manual-write-to-file-exporter-2026-07-05)):

1. **The pin moved to DualC `2fcd19f`** (ABI 0.5.0, the sibling checkout's `main`, three
   commits past the public root). On this Linux machine only the `.so` could be rebuilt:
   `native/linux-x64/libdualc_capi.so` is the `2fcd19f` build (20 exports, provenance in
   [`native/README.md`](../../../native/README.md)); **the Windows DLL and the viewer stay
   at `d6b2808` and owe a rebuild on a Windows machine** — no MSVC, MinGW or Wine here. The
   "same commit on both platforms" rule of D-44 is suspended until that rebuild, the first
   Next-up step.
2. **`Boletus.Core`** binds the four token functions and the three `*_with_progress` twins
   (18 of the 20 exports; the 0.4.0 diagnostics twins stay unbound, D-37) and exposes
   `Contour` / `Export` / `ExportTiledStl` overloads taking `IProgress<DualcProgress>` +
   `CancellationToken`, over a `DualcCancelTokenHandle` (`SafeHandle`) and one `WithToken`
   helper that keeps DualC's lifetime rule — registration disposed before the token, delegate
   rooted for the call, consumer exceptions swallowed before native code. **`SupportsProgress`
   probes the entry point once** (an `EntryPointNotFoundException` on the old DLL), so one
   managed build runs on both libraries — the pragmatic answer to the Windows gap in step 1,
   and the first real capability check the version trap had ruled out (D-08).
3. **`WriteToFileComponent`:** `Cancel ■` while running, the engine's percentage in the
   label and `Info`, a `Cancelled` state, and `RemovedFromDocument` cancels the in-flight
   write. On the old DLL it posts a Remark and keeps the Phase-A queued restart.

**Deviations from the sketch above, and why.** (a) *Cancel does not relaunch*: the sketch's
"Write-while-busy cancels at the next tile then relaunches" conflated two intents; a button
that reads `Cancel ■` must only cancel, or a click meant to stop a runaway write starts
another. A re-export is a second click on `Write ▶`. (b) *3MF and monolithic STL cancel too*,
up to the end of the contour (DualC D-45 defers checkpoints inside the writers), so the
"only tiled STL" caveat above is gone; the Write stage shows as `writing file` with no
percentage. (c) No `IProgress<(int done, int total)>` tuple: the report carries the stage,
because the monolithic stages are not comparable to tiles.

**Measured (2026-10-05, Linux, the Core tests):** a cancel requested from another thread
150 ms into a depth-8 monolithic export of the analytic graph is honoured in **under 70 ms**
(the whole test, its 150 ms wait included, ran in 220 ms; the assertion bound is 10 s); a
pre-cancelled token returns
before any tile is finished; a cancel from inside the progress callback at the second tile
leaves no file and no `.part`; the hooked contour and the hooked tiled export hit the same
golden counts as the plain calls; every stage ends at `done == total` on the calling thread;
a consumer that throws from `Report` does not crash the process. DualC's own
`dualc_c_demo … cancel` passes against the vendored build.

**Trigger review:** the section's trigger ("the wait-only abort becomes a real workflow
pain") never fired on the Boletus side; the upstream landing fired D-30 instead.

## 9. DualC at the pin — what `2fcd19f` offers that Boletus does not use

The inventory taken when the pin moved from `d6b2808` to `2fcd19f`, read from DualC's
header, `capi/README.md`, its roadmap index and decisions; each item names its Boletus home
so it is not rediscovered. Vocabulary: **no gap** — every op DualC's node reference lists is
in `Ops.cs`, and the round-trip tests ran against the `2fcd19f` CLI this session.

| DualC feature (its record) | Boletus status | Home |
| --- | --- | --- |
| ABI 0.4.0 `*_with_diagnostics` twins + `DualcDiagnostics` (DualC [14/04](../../../../DualC/docs/roadmap/14-c-abi/04-abi-0-4-0.md)) — tells an empty contour (a one-triangle placeholder returned as OK) from real geometry, reports input/output watertightness | **Bound (2026-10-05)** — `Proxy preview` warns on `emptyContour`, `Write to File` after a monolithic write; the record is [05/09](../05-phase3-grasshopper-components/09-contour-diagnostics.md). | D-37 settled |
| Unknown parameter keys rejected at create (DualC 14/04) | **Verified harmless**: the whole suite, the vocabulary round-trips and the fixture byte-matches pass on `2fcd19f`. | — |
| Every export writes `path.part` and renames (DualC D-46) | **Adopted for free** — a failed or killed write no longer leaves a bad file; stated in design 05. | design 05 |
| `dualc_version()` now moves with the ABI (`0.5.0`) | **Not asserted**; the ABI level is probed by entry point instead. A version bump per vocabulary change is still the ask. | D-08 |
| Viewer: the mesh-preview correctness sweep (DualC [12/07 § H](../../../../DualC/docs/roadmap/12-field-graph-and-app/07-mesh-preview-sweep.md#h-mesh-preview-correctness-sweep)), `dualc_view` and Polyscope retired (DualC #50) | **Pending the Windows rebuild** of `dualc_field_view.exe`; no Boletus code change (the side-car launches whatever exe is vendored). | § 5, `native/README.md` |
| Viewer: section-plane UI sliders (DualC D-17, PLANNED upstream since the public release) | **Nothing to adopt yet**; keyboard section planes are the viewer's own. | § 5 |
| CLI-only: tiled / welded 3MF, `--mem`, `--decimate` / `--simplify` (DualC D-21, D-26) | **Still not ABI-reachable**; unchanged from the asks of § 8. | D-32 (tiled 3MF) |
| Linux as a build host (DualC #49) and geometry-central fetched at configure (DualC #47) | **Adopted** — the Linux refresh recipe lost its `enable_language` workaround and its sibling `geometry-central`; the PIC flag is still Boletus's, the handoff row stands. | `native/README.md`, 09 § DualC handoff |

## 8. Strut-lattice vocabulary sync (`d6b2808`) — *DONE (Boletus side, 2026-07-10)*

> **Pinned contract to re-read first (from the sync handoff §0):** `D:\DualC\capi\CSHARP_WRAPPER_HANDOFF.md`
> and `D:\DualC\capi\dualc_c.h` are the authoritative, self-contained C-ABI spec the wrapper targets.
> They are unchanged from `e345bf3` — the sync is purely a graph-vocabulary + DLL-rebuild event.

**What landed upstream (DualC `d6b2808`, 2026-07-09):** four strut-lattice sources
(`sc`/`bcc`/`fcc`/`octet` + optional `nodeRadius` taper), the two-child `graded-offset` decorator,
and the three-child `mix` value-lerp morph — all pure **field-graph vocabulary**, reachable through
the `dualc_field_create_from_{json,expr}` calls Boletus already binds. **No ABI change**
(`git log e345bf3..d6b2808 -- capi/` is empty). The full node/param/compose reference lives in
[08 — Strut lattices](../08-strut-lattices.md) (the folded-in strut-nodes reference).

**Boletus side — DONE (2026-07-10):** re-vendored `dualc_capi.dll` (+ the viewer exe) from
`d6b2808`, taught `Ops.cs`/`Field.cs` the new nodes, and shipped `Strut Lattice`/`Graded Offset`/`Mix`
GH components. Full write-up: [05 § Implemented — strut lattices](../05-phase3-grasshopper-components/07-strut-lattice-components.md#implemented--strut-lattices-strut-lattice--graded-offset--mix-2026-07-10).

**Viewer re-vendored too (sync handoff §4).** `dualc_field_view.exe` was rebuilt from `d6b2808`,
gaining **adaptive render-scale + a metric-SDF fast-path** (~7× on exact-SDF graphs — strut lattices
included) and **discrete-GPU auto-select** on Optimus/PowerXpress laptops. No Boletus code change (the
Live Preview side-car just launches the newer exe); provenance in [`native/README.md`](../../../native/README.md).

**The one sharp edge — the parser lives *inside* the DLL.** Although the ABI symbols are unchanged,
the field-graph **parser is compiled into `dualc_capi.dll`** (`capi/CMakeLists.txt` links
`dualc_examples_fieldgraph` PRIVATE), so the old `e345bf3` DLL rejects the new ops with
`DUALC_ERR_GRAPH`. **Re-vendoring the DLL is mandatory to reach any new vocabulary** — a plain
managed update is not enough. This makes §4's version-check ask more urgent (next).

**New upstream asks (filed 2026-07-10, from the sync handoff):**

1. **Escape the version trap (extends §4).** `dualc_version()` returns `"dualc 0.3.0"` for **both**
   `e345bf3` and `d6b2808`, so a graph using a new op **cannot be feature-detected at runtime** — the
   DLL is pinned only by commit/date. Ask DualC to **bump the version** on a vocabulary change and/or
   expose a **capability / op-list query** (e.g. `dualc_op_supported(const char* token)` or a
   machine-readable `--list`) so Boletus can fail a `bcc(…)` graph with a clear "your DLL is too old"
   message instead of an opaque `DUALC_ERR_GRAPH`.
2. **`dualc_field_export_tiled_3mf` (tiled/streaming 3MF via the ABI).** Boletus writes 3MF
   **monolithically** (the `Write to File` `Tiled + 3MF` combo falls back to a single pass with a
   warning). DualC's **CLI** now has streaming tiled 3MF (`dualc_field --tile-depth -o .3mf`, plus
   `--weld` for a single manifold object), but it is **not exposed in the C ABI** — only
   `dualc_field_export_tiled_stl` is. A `dualc_field_export_tiled_3mf` entry point is the natural
   companion to the §7 progress/cancel ask and would give bounded-RAM 3MF export.

**Not ABI-reachable — no binding to wait for (host-side if wanted):** `--decimate R` / `--simplify E`
(QEM decimation) and `--mem BUDGET` (auto tile-depth) are **CLI-only** conveniences. Decimation, if
ever wanted, is applied host-side on the `dualc_field_contour` result; the tile depth is already an
explicit ABI param, so Boletus computes/passes its own (`ExportPlan`).

**Upstream verification bar (sync handoff §6):** DualC's GPU parity is now **69/69** (was 62/62); the
four new nodes above are GPU-verified upstream. On the Boletus side the four nodes are gated by the
`--dump-json` round-trips + the three clipped-contour acceptance tests (117/117 Core), so both the
emission and the meshing are pinned — see [05 § Verification (strut lattices)](../05-phase3-grasshopper-components/07-strut-lattice-components.md#verification-strut-lattices).

**Status:** DONE (Boletus side). The two asks above are **open upstream** — treat exports as blocking
and pin the DLL by commit until they land.

---

---

← Back to the [Upstream coordination index](README.md) · the [Roadmap index](../README.md).
