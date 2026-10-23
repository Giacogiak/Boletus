# Write to File

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
  ([design 07 § Single-flight writes](../../design/07-invariants-and-limits.md#single-flight-writes-and-the-cancel-constraint)).
  Deleting the component cancels its write too.
- **Change the definition while a write runs** and the component shows `BUSY`, keeps writing
  the *old* geometry and does not restart: cancel, then click `Write ▶` again.
- **With a DualC library older than ABI 0.5.0** (the Windows DLL until it is rebuilt at the
  pin — [`native/README.md`](../../../native/README.md)) the component posts a Remark and runs
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
[design 05](../../design/05-export-planning.md); the threading model and the cancel path:
[design 04 § Write to File](../../design/04-grasshopper-plugin/06-write-to-file.md#write-to-file--the-threading-model).

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
mesh ([design 07 § Tiled equals monolithic](../../design/07-invariants-and-limits.md#tiled-equals-monolithic))
at bounded RAM, and it is the mode with the finest cancel and progress (per tile); a tiled
3MF needs an engine entry point ([D-32](../../decisions/README.md)).

# Proxy preview

*Nickname `Proxy`.* Draws a **coarse, depth-capped** proxy of the volume directly in the

---

← Back to [Family 4: Terminals](README.md) · the [command reference](../README.md)
