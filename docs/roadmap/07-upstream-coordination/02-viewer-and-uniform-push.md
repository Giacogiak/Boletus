# 07 — Upstream coordination: The viewer binary and the uniform push

Part of [07 — Upstream coordination with DualC](README.md); every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4). Section numbers are the stable addresses.

## 5. `dualc_field_view` viewer binary — *gates the raymarch preview phase*

**Today:** only `dualc_capi.dll` is vendored. The live preview is **not** part of the C ABI — it
is the standalone GPU executable `dualc_field_view` (OpenGL 3.3 / GLFW / glad), which takes the
same field-graph (`.fld`/`.json`/`--expr`/stdin) and renders it interactively (with `--snapshot`
for a headless PNG). See `D:\DualC/docs/command_reference/12-dualc_field_view/README.md`.
*(2026-09-21, roadmap 09 Phase 5, the first semantic lint: the "Today" is the ask's premise as written; the viewer
has been vendored since 5a, its provenance in [`native/README.md`](../../../native/README.md).)*

**Want:** to ship the **raymarch preview side-car phase** (roadmap Phase 5), Boletus needs the
`dualc_field_view.exe` binary (+ its GPU runtime deps) vendored alongside `dualc_capi.dll`, under
the same **local-only / never-publish** rule as the native engine. Integration is a
**separate-process launch**: serialize the current `Volume` to a temp `.json`, run the viewer.

**Boletus side:** a `Live Preview` component (Phase 5) that writes the graph and launches the
viewer; no new ABI. Caveat: GPU-dependent, so it cannot run headless/CI.

**Boletus side — 5a DONE (2026-07-03, verified live in Rhino):**
1. ✅ Vendored `dualc_field_view.exe` into `native/x64/` (self-contained — GLFW/glad static, no
   companion DLLs; provenance in [`native/README.md`](../../../native/README.md)); csproj copy-item +
   `.gitignore` un-ignore mirror the DLL. **Never publish** (same local-only rule).
   *(2026-10-06, roadmap 10: the viewer is built from the DualC submodule by
   `scripts/build_native.py` on Windows and in CI, never committed; the local-only rule is
   reversed by D-46 — [10 #33](../10-public-delivery/02-submodule.md#33-dualc-as-a-git-submodule--the-binaries-built-never-committed).)*
2. ✅ `MeshMaterializingResolver` (Rhino-free, in `Boletus.Core`): the viewer twin of `VolumeResolver`
   — because a **separate process can't read the in-RAM mesh buffers** (`*_with_meshes` reaches only
   the in-process DLL), it writes each in-memory `mesh`/`winding` leaf to a temp OBJ and rewrites
   `mem://<id>` → `path=<temp .obj>` (mesh-free graphs pass through unchanged). Gated by
   `MeshMaterializingResolverTests` (92/92 Core).
3. ✅ `LivePreviewComponent` (Terminals): serialize → atomic temp `.json`, launch/track the viewer as
   a child process (located beside the `.gha`), optional `Min`/`Max` → `--bounds`, skip-if-unchanged,
   lifecycle cleanup on `RemovedFromDocument` / app exit. Independent viewer camera (no Rhino bind).

**5b — upstream file-watch — DONE 2026-07-03 (confirmed live in Rhino).**
`D:/DualC/examples/dualc_field_view.cpp` now polls its input file's mtime in the render loop
(throttled ~7 Hz via `std::filesystem::last_write_time`) and triggers the existing structural reload
on any change (file input only; `l` still forces manual),
**and recomputes `bounds`/`bext`/`diag` inside the reload block** so a size-changing live edit keeps
the raymarch box + section planes framed (they were captured at startup). Rebuilt
`-DDUALC_BUILD_FIELD_VIEW=ON` and re-vendored the exe (`native/x64/dualc_field_view.exe`,
[`native/README.md`](../../../native/README.md) provenance bumped). No Boletus code change beyond dropping
the now-obsolete "press `l`" remark — 5a already rewrites `graph.json` atomically each solve, so the
window updates on its own. **Confirmed live in Rhino:** dragging a GH slider on an open Live Preview
window refreshes the viewport with no keypress, and a size-changing nudge keeps the framing / section
planes sane (the recompute-bounds path). The reason for the DualC change is recorded upstream in
`D:/DualC/docs/roadmap/12-field-graph-and-app/03-raymarch-app.md` (§D) (Disk file-watch) + `command_reference/12`.
**Deferred (not this phase):** per-parameter uniform push — real-time slider scrubbing without a
per-change shader recompile — needs a real command channel (socket/stdin IPC), a different mechanism
from the file-watch. Full write-up: [§6](#6-real-time-parameter-push-uniform-ipc-channel--deferred-performance-optimization).

**Scope decision (2026-06-19): TPMS/lattice visualization belongs *exclusively* to this side-car.**
The in-Rhino `Contour`/`Proxy preview` mesh is only a **capped LOD** — a coarse drawable aid held
under a hard **depth ceiling** (`Proxy preview` clamps to `MaxProxyDepth = 7`; cap-mechanism decision
2026-06-20, **DONE** — see [05 § Implemented 3b.3 (capped proxy preview)](../05-phase3-grasshopper-components/04-proxy-preview-and-icons.md#implemented-3b3-partial--capped-proxy-preview-2026-06-20))
so it can never OOM Rhino — **not** the surface for judging lattice fidelity (a coarse contour of a
dense lattice drops thin walls). Fidelity is the raymarcher's job. This is *why* the Rhino proxy is
allowed to stay coarse/capped.

**Trigger:** when Phase 5 (live preview) starts.

## 6. Real-time parameter push (uniform IPC channel) — *deferred performance optimization*

**DEFERRED** — trigger at the end of the section; upstream it is DualC D-25. *(2026-09-21, roadmap 09 Phase 3: status line added as the first body line so the `decisions-index` gate finds this entry; the row is D-27 in the [decisions index](../../decisions/README.md).)*

**Status:** deferred. The Phase-5b file-watch (§5) already gives correct live update for *every* kind
of edit; this item only makes the **number-only** case (dragging a slider) smoother. It is a
latency/polish optimization, **not** a capability the side-car currently lacks — everything you can
preview today, you can preview. Recorded here so a future performance requirement has the full
rationale.

**Background — the viewer's two update tiers.** When `dualc_field_view` loads a graph it does two
things: (1) **compiles the whole field-graph to a GPU shader** (the field→GLSL codegen + a `glCompile`/
`glLink` — the expensive step), and (2) **sets each knob as a *uniform*** — a single value the compiled
shader reads (a gyroid's `wavelength`, an `onion`'s `thickness`, a sphere's `radius`, a smooth
boolean's `k`, …). The viewer's keyboard exposes both: **`l`** re-does step 1 (full **recompile** — for
when the *shape* of the graph changed), while **`[` / `]`** re-does step 2 for one number (**one
uniform upload, no recompile → instant**). This is the "binding table" design documented in DualC
[`command_reference/12` › Why two tiers](../../../../DualC/docs/command_reference/12-dualc_field_view/01-controls.md#why-two-tiers)
and [`roadmap/12 §D`](../../../../DualC/docs/roadmap/12-field-graph-and-app/03-raymarch-app.md).

**What we shipped (§5) drives the *recompile* tier for everything.** The file-watch rewrites the graph
file on each GH solve and the viewer runs the **structural reload** — a full shader recompile —
regardless of whether you added a node or merely nudged a slider. That is simple and universal (it
handles any change) and needed **zero** new protocol (Boletus just overwrites a file). The cost: every
change pays a recompile, ~tens-to-hundreds of ms depending on graph complexity. In practice: nudge a
value, the window catches up a beat later. Perfectly usable (and what Phase 5 confirmed live).

**What uniform push would add.** For the case where **only a number changed** (a wavelength slider, not
a restructured graph), recompiling the entire shader is wasteful — the *code* is identical, only one
value moved. Uniform push would let Boletus send a tiny message like `set wavelength 0.42` straight to
the running viewer, which updates that one uniform — **exactly the instant `[` / `]` path, driven by a
GH slider instead of the keyboard.**

**The concrete improvement is latency:**

| | Today (file-watch → recompile) | With uniform push |
|---|---|---|
| Scrub a slider | window updates in small **hitches** (a recompile per step) | lattice **morphs fluidly at frame rate (60 fps)**, no hitch |
| Feel | "updates a beat after each change" | "updates continuously *as you drag*" — like a video-game control |

**Why it is deferred (and stays minor):**
- **It needs real IPC.** A socket or stdin command channel between the plugin and the viewer process,
  with a small protocol — new plumbing on **both** sides (the DualC viewer must *listen*; Boletus must
  *send*). The file-watch needed none of this.
- **It only helps number-only changes.** Add/remove a node or swap an op and you **still** need a full
  recompile — so Boletus would have to detect "only a scalar moved vs. the structure changed" and route
  to the right channel (uniform push vs. rewrite-the-file), and reference params by the same keys the
  viewer's codegen assigns to its binding table. Extra complexity on both sides.
- **The current path is already fine for most work.** The recompile hitch only becomes noticeable when
  **continuously scrubbing** a slider and wanting buttery real-time morphing.

**Upstream (DualC) shape if picked up:** teach `dualc_field_view` to accept a command stream (e.g.
`--command-port N` or a non-blocking stdin protocol) carrying `set <paramKey> <value>` lines that map
to `setBinding()`/`glUniform` with **no** recompile (and, for structure changes, a `reload` command or
fall back to the file-watch). Boletus side: a diff of consecutive `Volume`s to classify scalar-only vs.
structural, and the sender. Boletus is the *driver* (the reason to do it); the channel lives in DualC.

**Trigger:** a user workflow that leans on **continuous slider scrubbing** of a dense lattice and finds
the per-change recompile hitch limiting. Until then, the file-watch (§5) is sufficient.

---

← Back to the [Upstream coordination index](README.md) · the [Roadmap index](../README.md).
