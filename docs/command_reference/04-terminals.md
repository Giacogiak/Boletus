# Family 4: Terminals

The three components under **Boletus › Terminals** are the only ones that hand a graph to
DualC or to the viewer — where the field graph is finally evaluated. All three auto-fit
bounds on a finite field and take `Min` / `Max` only for an unbounded one
([00 § Bounds](00-shared-behaviour.md#bounds--min--max-on-a-terminal)), and all three run
the validator first and post its findings ([00 § Troubleshooting](00-shared-behaviour.md#troubleshooting)).

## Write to File

*Nickname `Write`.* Exports a volume to an **STL** or **3MF** file. It is an **on-demand,
background** exporter that never writes on its own:

- **Click the `Write ▶` button** under the component to launch. The write runs on a
  background thread, so Grasshopper stays responsive even when a heavy part takes minutes;
  the button turns into `Cancel ■` and the `Info` output ticks the engine's progress —
  `Writing… tile 3/27 (11%) · 12s` for a tiled STL, `sampling 40%` / `contouring 80%` /
  `writing file` for a monolithic write.
- **Click `Cancel ■`** to stop the write at the engine's next checkpoint (well under a
  second): the component ends `Cancelled` and **nothing is left on disk** — a file already
  at the path is untouched, because DualC writes `.part` and renames only on success. A
  cancel during the final write phase of a monolithic export can still complete the file
  ([design 07 § Single-flight writes](../design/07-invariants-and-limits.md#single-flight-writes-and-the-cancel-constraint)).
  Deleting the component cancels its write too.
- **Change the definition while a write runs** and the component shows `BUSY`, keeps writing
  the *old* geometry and does not restart: cancel, then click `Write ▶` again.
- **With a DualC library older than ABI 0.5.0** (the Windows DLL until it is rebuilt at the
  pin — [`native/README.md`](../../native/README.md)) the component posts a Remark and runs
  its older behaviour: the button reads `Writing…`, `Info` ticks `Writing… Ns` with no
  percentage, a running write cannot be stopped, and a click while running queues **one**
  restart with the new inputs that starts when the current write finishes.
- It makes **no mesh** — `Proxy preview` and `Live Preview` are for seeing the volume.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | The field graph to contour and write; imported mesh leaves reach DualC in memory. |
| Depth (`D`) | Integer | 6 | Maximum octree depth (mesh resolution). Exponential on a lattice; above 8 the component warns. |
| Path (`P`) | Text | **required** | Output path. A bare name is fine — the extension comes from `Format`, and a conflicting typed extension is replaced with a warning. The parent folder must exist. `C:\tmp\lattice` with Format 0 writes `C:\tmp\lattice.stl`. |
| Format (`F`) | Integer | 0 | **0 = STL** (binary; the default) · **1 = 3MF** (1 unit = 1 mm). Decides the file extension. |
| Mode (`Md`) | Integer | 0 | **0 = Tiled** — streams the mesh to disk tile by tile, peak RAM about one tile; STL only · **1 = Monolithic** — the whole mesh in RAM, then one write. 3MF is always Monolithic. |
| Min (`Min`) | Point | *(optional)* | Bounds minimum corner, only for an unbounded field; both corners or neither. |
| Max (`Max`) | Point | *(optional)* | Bounds maximum corner. |
| Tile depth (`T`) | Integer | *(optional; Depth − 2)* | Sub-grid depth of the tiled STL writer (Format 0 + Mode 0 only). Lower means smaller tiles, less peak RAM, more CPU; 4 or more is recommended. At or above `Depth` the tiling degenerates to a single pass and the component warns. |

| Output | Type | Meaning |
| --- | --- | --- |
| Info (`I`) | Text | `Idle — click Write to export.` · `Writing… Ns` (before the first report, or on an older library) · `Writing… tile i/T (p%) · Ns` · `Writing… sampling p% · Ns` · `Writing… contouring p% · Ns` · `Writing… writing file · Ns` · `Cancelling… … · Ns` · `Done → file.stl (… MB, …s)` · `Cancelled — nothing written; file.stl untouched.` · `Inputs changed since the last write (file.stl). Click Write to re-export.` · `Failed (unbounded field)` · `Failed`. |
| File (`F`) | Text | The resolved absolute path of the written file, after a successful write only; empty after a cancel or a failure. |

The component's own `Message` label shows the same running text as `Info`, then `Done`,
`Cancelled`, `Ready` (the inputs changed after a write) or `Failed`; the button reads
`Write ▶`, `Cancel ■`, `Cancelling…`, or `Writing…` on an older library. Which native writer
runs, how the extension and the tile depth are decided and what each warning means:
[design 05](../design/05-export-planning.md); the threading model and the cancel path:
[design 04 § Write to File](../design/04-grasshopper-plugin.md#write-to-file--the-threading-model).

Messages — Remark: `The vendored DualC library predates ABI 0.5.0: no progress or cancel — a
running write always finishes; a Write click while running queues one restart.` Warning:
`Depth … on a lattice can be very expensive (exponential per level).`
(Depth > 8) · `Provide both Min and Max to set bounds; ignoring the single corner
(auto-bounds).` · `Path extension '…' overridden by the Format selector → '…'.` · `3MF has
no tiled writer; writing monolithically.` · `Tile depth … ≥ Depth …: tiling falls back to a
single pass (no RAM benefit).` · `BUSY — a write is running with older inputs. Click Cancel ■
to stop it, then Write ▶ to re-export with the new inputs, or wait for it to finish.` (on an
older library: `BUSY — a write is running with older inputs. Click Write to queue a restart
with the new inputs, or wait for it to finish.`) · `No surface in the sampled region — …
holds only a placeholder triangle. Check Min/Max against the volume's position, or the
volume's size.` (after a completed monolithic write whose contour was empty — the file holds
the engine's one placeholder triangle; the tiled writer has no such report, and neither has a
DualC library older than ABI 0.4.0) Error: `A file path is
required.` · `Unknown format … (expected 0 = STL, 1 = 3MF).` · `Unknown mode … (expected
0 = Tiled, 1 = Monolithic).` · `Directory does not exist: …` · `Failed to serialize volume:
…` · `Field is unbounded — connect a boundary (Onion/Boolean) or set Min/Max.` · the native
error message on any other failure (`Export failed.` when it has none).

Leave `Mode` on Tiled for a heavy lattice: the tiled STL is bit-identical to the monolithic
mesh ([design 07 § Tiled equals monolithic](../design/07-invariants-and-limits.md#tiled-equals-monolithic))
at bounded RAM, and it is the mode with the finest cancel and progress (per tile); a tiled
3MF needs an engine entry point ([D-32](../decisions/README.md)).

## Proxy preview

*Nickname `Proxy`.* Draws a **coarse, depth-capped** proxy of the volume directly in the
viewport — **preview only, no output, nothing to bake**. Its reason to exist is a preview
that cannot exhaust memory: the depth is clamped to a hard ceiling of **7**, however high it
is dialled ([design 07 § `MaxProxyDepth`](../design/07-invariants-and-limits.md#maxproxydepth)).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to preview. |
| Depth (`D`) | Integer | 5 | Maximum octree depth; clamped to 1…7, with a remark when the clamp bites. |
| Min (`Min`) | Point | *(optional)* | Bounds minimum corner, only for an unbounded field. |
| Max (`Max`) | Point | *(optional)* | Bounds maximum corner. |

No outputs: it draws into the viewport, and Zoom Extents frames it.

It contours **once** per solve and caches, so pan and zoom stay smooth
([design 04 § Proxy preview](../design/04-grasshopper-plugin.md#proxy-preview--the-capped-viewport-lod)).
A coarse proxy of a dense lattice is lossy — thin walls drop out — so it is never the surface
for judging fidelity; `Write to File` gives the real mesh and `Live Preview` the exact field.

Messages — Remark: `Proxy depth capped at 7 (preview is a coarse LOD; use the raymarch
side-car for fidelity).` · `Proxy mesh is open (… boundary, … non-manifold edges) — expected
where Min/Max cut through the surface; otherwise the volume itself is open.` Warning:
`Provide both Min and Max to set bounds; ignoring the single corner (auto-bounds).` · `No
surface in the sampled region — the volume crosses zero nowhere inside the bounds, so there is
nothing to draw. Check Min/Max against the volume's position, or the volume's size.` (nothing
is drawn; this and the open-mesh remark need a DualC library at ABI 0.4.0 or later — on an
older DLL the preview draws the plain contour and says nothing) Error: `Field is unbounded —
connect a boundary (e.g. clip via Onion/Boolean) or set Min/Max.` · `DualC error (…): …`.

## Live Preview

*Nickname `LivePreview`.* Launches an external **GPU raymarch window** — DualC's
`dualc_field_view`, vendored beside the `.gha` — that sphere-traces the **exact** field: the
true-fidelity preview the coarse proxy cannot give, its RAM bounded by the window rather than
the lattice, so it renders densities that would exhaust the contourer. The window has its
**own** camera — orbit with left-drag, dolly with the scroll wheel — and is not tied to the
Rhino viewport; inside it `l` reloads, `x` / `y` / `z` toggle section planes, the arrows slide
them, `Esc` quits (the full keyboard map: [DualC command reference § GPU viewer keyboard map](../../../DualC/docs/command_reference/README.md#gpu-viewer-keyboard-map-dualc_raymarch--dualc_field_view)).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to raymarch. |
| On (`On`) | Boolean | false | Launch and keep the window open (true) or close it (false). Off by default so the window opens only when asked. |
| Min (`Min`) | Point | *(optional)* | Bounds minimum corner — required for an unbounded field (a bare TPMS, plane or repeat). |
| Max (`Max`) | Point | *(optional)* | Bounds maximum corner. |

No outputs: the preview is the external window.

Each solve serializes the volume to a temp `.json` and the viewer watches that file, so the
window **refreshes on its own** whenever a parameter changes — no keypress, no IPC. An
imported mesh (`Mesh → Volume`) is written to a temp `.obj` for the viewer automatically, so
mesh-based parts preview too. Toggle `On` off, or delete the component, to close the window
([design 04 § Live Preview](../design/04-grasshopper-plugin.md#live-preview--the-process-model)).
Every change pays a shader recompile; the instant per-parameter push that would avoid it is a
deferred optimisation ([D-27](../decisions/README.md)).

Messages — Remark: `Off — connect a volume and set On to launch the viewer.` · `Side-car
viewer running — updates live as you edit the graph. Orbit: left-drag · dolly: scroll · Esc
quit.` Warning: `Provide both Min and Max to set bounds; ignoring the single corner
(auto-bounds).` Error: `dualc_field_view.exe not found next to the .gha — the viewer is
built, never committed: run scripts/build_native.py or install the CI .yak.` ·
`Failed to serialize volume: …` · `Failed to write graph file: …` ·
`Viewer exited immediately: …` (the viewer's stderr, or `the field may be unbounded — set
Min/Max (a bare TPMS/plane/repeat has no finite bounds).`) · `Failed to launch viewer: …`.

## Recipes

| # | Goal | Wiring |
| --- | --- | --- |
| T1 | A tiled STL of a heavy lattice | `Write to File`: Path = `C:\tmp\lattice`, Format 0, Mode 0, Depth 6 → click `Write ▶`; keep working while it runs |
| T2 | A 3MF for a slicer | Format 1 (Mode is ignored: monolithic); the file declares millimetres |
| T3 | A finer export at bounded RAM | Depth 8, Mode 0, Tile depth 5 |
| T4 | Export an unbounded lattice | connect `Min` and `Max` — or clip it on the canvas first |
| T5 | A quick look while wiring | `Proxy preview`, Depth 5 (the default) |
| T6 | The densest lattice the machine can show | `Live Preview`, `On` = true; `Min` / `Max` if the field is bare |
| T7 | Re-export after a change | `Info` reads `Inputs changed since the last write …` — click `Write ▶` again |

---

← Back to the [command reference](README.md) · the [docs index](../README.md)
