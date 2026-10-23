# Live Preview

*Nickname `LivePreview`.* Launches an external **GPU raymarch window** as a separate process
that sphere-traces the **exact** field: the true-fidelity preview the coarse proxy cannot
give, its RAM bounded by the window rather than the lattice, so it renders densities that
would exhaust the contourer. `Viewer` picks the window: **DualCEditor** in exploration mode —
a section toolbar, Objects / Parameters / Log docks, a status bar with compile and frame
times, the shader compiled off the UI thread so the window never freezes — or DualC's
reference viewer **`dualc_field_view`**, keyboard and console controls only. Both have their
**own** camera and are not tied to the Rhino viewport. **Grasshopper is the only input**: the
window explores the volume and never edits it; nothing done in it reaches the canvas or the
graph file.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to raymarch. |
| On (`On`) | Boolean | false | Launch and keep the window open (true) or close it (false). Off by default so the window opens only when asked. |
| Min (`Min`) | Point | *(optional)* | Bounds minimum corner — required for an unbounded field (a bare TPMS, plane or repeat). |
| Max (`Max`) | Point | *(optional)* | Bounds maximum corner. |
| Viewer (`Vw`) | Integer | 0 | **0 = Field view** — `dualc_field_view.exe` beside the `.gha`; `Min` / `Max` reach it at launch only · **1 = Editor** — `editor\DualCEditor.exe` beside the `.gha`, with its Qt runtime; `Min` / `Max` are live. Editor asked for but not shipped: the field view runs instead, with a warning. |

No outputs: the preview is the external window.

Each solve serializes the volume to a temp `graph.json` — the canonical graph plus, when
`Min` / `Max` are set, one top-level `view` key carrying them — and both viewers watch that
file, so the window **refreshes on its own** whenever a parameter changes, keeping its camera
— no keypress, no IPC. An imported mesh (`Mesh → Volume`) is written to a temp `.obj` beside
it automatically, so mesh-based parts preview too. Toggle `On` off, or delete the component,
to close the window
([design 04 § Live Preview](../../design/04-grasshopper-plugin/07-live-preview.md#live-preview--the-process-model)).
Every change pays a shader recompile; a per-parameter push from the canvas that would avoid
it is a deferred optimisation ([D-27](../../decisions/README.md)).

**In the editor** the mouse orbits, pans and dollies and `F` frames the bounds; the section
toolbar's `X` / `Y` / `Z` toggle one axis-aligned cut and `−` / `+` move it; the Objects dock
shows or hides the object; the Parameters dock edits the shader's numbers — a tweak shows
instantly, without a recompile, and is **overwritten by the next solve's reload** (the
file's values win); the Log dock and the status bar carry load errors and timings. The full
key map is DualCEditor's own
([its viewport reference](https://github.com/Giacogiak/DualCEditor/blob/main/docs/command_reference/01-viewport.md)).
Until the pinned editor has its exploration switch, the window also shows its editing menus;
whatever is done there stays in the window and is discarded at the next reload. **In the
field view** `l` reloads, `x` / `y` / `z` toggle section planes, the arrows slide them, `Esc`
quits (the full keyboard map:
[DualC command reference § GPU viewer keyboard map](../../../../DualC/docs/command_reference/README.md#gpu-viewer-keyboard-map-dualc_raymarch--dualc_field_view)).

Messages — Remark: `Off — connect a volume and set On to launch the viewer.` · `Side-car
viewer running — updates live as you edit the graph. Orbit: left-drag · dolly: scroll · Esc
quit.` (the field view) · `Explorer running — updates live as you edit the graph; parameter
tweaks in its dock reset on the next solve. Orbit: left-drag · dolly: scroll · F frame.` (the
editor) Warning: `Provide both Min and Max to set bounds; ignoring the single corner
(auto-bounds).` · `Min/Max do not form a box (each Max must exceed its Min); ignoring them
(auto-bounds).` · `DualCEditor not found under …\editor\ — falling back to
dualc_field_view.exe.` Error: `Unknown viewer … (expected 0 = Field view, 1 = Editor).` ·
`No viewer found beside the .gha (dualc_field_view.exe, or editor\DualCEditor.exe) — the
viewers are built, never committed: run scripts/build_native.py or install the CI .yak.` ·
`Failed to serialize volume: …` · `Failed to write graph file: …` · `Viewer exited
immediately: …` (the field view died on launch — its stderr, or `the field may be unbounded —
set Min/Max (a bare TPMS/plane/repeat has no finite bounds).`) · `Viewer exited (code 0x…):
…` (the editor died after launch — its stderr, or `no message — a missing Qt DLL beside
DualCEditor.exe is the usual cause; rebuild with scripts/build_native.py.`; posted on the
next solve, which does not relaunch until an input changes) · `Failed to launch viewer: …`.

---

← Back to [Family 4: Terminals](README.md) · the [command reference](../README.md)
