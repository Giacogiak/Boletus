# Glossary

One line per term as the code and the docs use it, with the page that explains it where one
exists. Engine terms — SDF, Hermite data, QEF, watertight, the sampler and the contourer —
are DualC's and live in its [glossary](../../../DualC/docs/design/11-glossary.md); only the
ones a Boletus page leans on are repeated in one line. The second half is the **ID
vocabulary**: what `#N`, `D-NN`, `DualC #N`, `DualC D-NN` and `NN-slug` refer to, because
they look alike and are not.

## Terms

| Term | Meaning |
| --- | --- |
| **Field graph** | The immutable tree of ops — sources → decorators / domain ops → booleans — that DualC contours or compiles to GLSL; canonical JSON or the `--expr` shorthand ([02](02-field-graph-model.md)). |
| **The graph string is the API** | DualC's C ABI has no per-primitive factories: a host builds a field by sending one string and gets back a proxy mesh or a file ([README](README.md)). |
| **`FieldNode` / `FieldValue`** | The managed node (op token, parameter bag, ordered children) and its parameter value type (scalar, vector, text). |
| **`Ops` / `OpSchema`** | The pinned registry, one schema per op: token, child arity, category, parameter specs. |
| **`Field` builders** | The typed static builders (`Field.Gyroid`, `Field.Normalize`, `Field.Onion`, `Field.Bcc`, …) a component calls so it never hand-writes JSON. |
| **Canonical JSON** | The one serialization: `op` first, parameters ASCII-ordinal, `in` last, forced `.0` on integral doubles — byte-identical to `dualc_field --dump-json` ([02 § The byte-match rules](02-field-graph-model.md#the-byte-match-rules)). |
| **`--dump-json` round-trip** | The per-op oracle: the emitted JSON must equal DualC's canonicalisation of the equivalent `--expr` ([07](07-invariants-and-limits.md#serializer-output-equals---dump-json)). |
| **Metric / non-metric** | A field whose value is true distance (gradient ≈ 1) versus one that is only sign-correct (a raw TPMS, `winding`); `IsMetric` decides, `normalize` converts ([06](06-conventions.md#metric-by-default-sources-and-the-normalize-rule)). |
| **`MetricSource`** | The `OpCategory` of a true-SDF source: every analytic primitive, `mesh`, the strut crystals — and, by a recorded judgement call, `triangle` / `quad`. |
| **The Normalize rule** | A non-metric source gets a `Normalize` before `Onion`, `Graded Onion`, `Graded Offset` or a smooth boolean; every source but a raw `TPMS` is metric by default. |
| **Onion before clip** | The boundary is an input on `Onion` / `Graded Onion`, intersected *after* the shell so cut faces stay clean ([06](06-conventions.md#onion-before-clip)). |
| **`Volume`** | The runtime currency on every wire: a `FieldNode` plus the in-memory `MeshBuffer`s of its input-mesh leaves; meshless, never a contoured result ([03](03-volume-and-resolvers.md)). |
| **`VolumeGoo` / `VolumeParameter`** | The Grasshopper wrappers of `Volume`: the wire payload and the hidden, non-persistent parameter. |
| **Input-mesh leaf vs output mesh** | A user's imported triangles carried as `mesh(id=…)` / `winding(id=…)` — never contoured — versus the one dual-contoured mesh a terminal produces. |
| **`MeshBuffer`** | The Rhino-free triangle soup: `float[]` vertices, `int[]` indices, a content-hash id, an OBJ writer. |
| **`mem://`** | `Volume.MemoryScheme`: the placeholder in a mesh leaf's `path` that names an in-memory buffer until a resolver rewrites it. |
| **Resolver** | A `FieldTree.Rewrite` over a `Volume`: `VolumeResolver` (`mem://` → `id=`, buffers in RAM, the in-process path) or `MeshMaterializingResolver` (`mem://` → `path=`, temp OBJ, the side-car path). |
| **Diskless** | The in-process path: input meshes reach the DLL as pinned RAM buffers through the `*_with_meshes` create calls; no temp file. |
| **Pinned for the call only** | The managed buffers are `GCHandle`-pinned only for the duration of the create call, because DualC copies them during it ([01](01-native-interop.md#buffers-are-pinned-for-the-call-only)). |
| **`DualcField`** | The managed field handle: `Version`, `FromExpr` / `FromJson`, `Contour`, `Export`, `ExportTiledStl` (each also with an `IProgress` + `CancellationToken` overload; `Contour` and `Export` also with a trailing `out DualcDiagnostics`, plain and with progress), the two entry-point probes `SupportsProgress` / `SupportsDiagnostics`, `Dispose`; single-threaded, one per solve ([01](01-native-interop.md)). |
| **`DualcDiagnostics`** | What the engine degraded silently in a contour or an export (ABI 0.4.0): `EmptyContour` — the one way to tell the engine's one-triangle placeholder for a field with no surface in the sampled region from real geometry — the output's counts and `OutputWatertight`, `AnyIssue` folding them; built only from a call that returned OK ([01](01-native-interop.md#the-public-surface--dualcfield)). |
| **`DualcContourParams`** | The idiomatic mirror of the ABI's flat parameter struct; `MaxDepth` is the only proxy/export lever. |
| **`DualcStatus` / `DualcException`** | The ABI's status codes as an enum, and the exception a non-OK return becomes, carrying the native message and locator. |
| **Locator** | The position DualC reports with a graph error: a JSON pointer such as `/root/in/0/radius`, or a character offset for `--expr`. |
| **Golden counts** | The exact vertex / triangle counts of two reference graphs at depth 6 that the tests assert as the marshaling gate ([07](07-invariants-and-limits.md#the-golden-contour-counts)). |
| **Proxy** | A coarse-depth contour used as a drawable stand-in; lossy on a lattice. `Proxy preview` is the capped viewport-only one ([04](04-grasshopper-plugin.md#proxy-preview--the-capped-viewport-lod)). |
| **`MaxProxyDepth`** | The hard depth ceiling of `Proxy preview`, the only mechanism that prevents rather than detects an out-of-memory ([07](07-invariants-and-limits.md#maxproxydepth)). |
| **Side-car** | DualC's GPU viewer `dualc_field_view.exe`, launched by `Live Preview` as a separate process on a temp file; the true-fidelity lattice preview, with its own camera ([04](04-grasshopper-plugin.md#live-preview--the-process-model)). |
| **File-watch** | The viewer's own polling of its input file's mtime, which makes rewriting the temp file the whole update channel: no keypress, no IPC. |
| **Uniform push** | The deferred instant-scrub tier: sending one changed number to the running viewer without a shader recompile; needs a real IPC channel. |
| **Terminal** | A component that hands a graph to the engine or the viewer: `Write to File`, `Proxy preview`, `Live Preview`. Everything upstream is tree surgery. |
| **Tiled STL** | Streaming binary-STL export, `ExportTiledStl`: tile by tile, peak RAM about one tile, bit-identical to the monolithic mesh ([05](05-export-planning.md)). |
| **Monolithic** | The whole mesh built in RAM and written once (`Export`); the only strategy for 3MF. |
| **Tile depth** | The sub-grid depth of the tiled writer; defaults to `depth − 2`, floored at 1, gains nothing at `≥ depth`. |
| **`ExportPlan`** | The pure planner: path and extension from the format selector, the strategy table, the effective tile depth, the advisories ([05](05-export-planning.md)). |
| **Single flight / BUSY** | `Write to File`'s guarantee of never two concurrent native writes: an input change mid-write is reported as BUSY, never auto-run; a click while running cancels (ABI 0.5.0) or, on an older library, queues one restart ([07](07-invariants-and-limits.md#single-flight-writes-and-the-cancel-constraint)). |
| **Write button** | The momentary on-canvas capsule (`Write ▶`; `Cancel ■`, then `Cancelling…`, while running; `Writing…` on a library without the cancel ABI) that is the only thing that launches or stops a write. |
| **Cancel token** | DualC's host-owned sticky flag (`DualcCancelTokenHandle`), requested from any thread, polled by the engine at its checkpoints; a cancelled export leaves nothing at the path because every export writes `.part` and renames on success ([01](01-native-interop.md#cancel-and-progress--the-050-twins)). |
| **Progress stage** | One of `Sample`, `Contour`, `Write`, `Tile` (`DualcStage`): the coarse unit DualC reports `done / total` for, on the calling thread only. |
| **Standalone vs consolidated** | A component per op (`Onion`, `Normalize`, the warps) versus one component with a type or kind dropdown among its inputs (`TPMS`, `Primitive`, `Boolean`, `Strut Lattice`, `Mesh → Volume`, `Displace`, the primitive families). |
| **Remark / Warning / Error** | Grasshopper's message levels, each with one meaning here ([06 § Message levels](06-conventions.md#message-levels)). |
| **The native side** | `dualc_capi.dll` / `libdualc_capi.so`, `dualc_field` and `dualc_field_view.exe`, built from the submodule by `scripts/build_native.py` into the gitignored `native/<rid>/`; never committed. "Vendored" in a dated record names the earlier committed-binary era. |
| **The pin** | The one DualC commit Boletus targets: the gitlink of the submodule `external/DualC`, the same for every platform. |
| **The version trap** | The same `dualc_version()` string on libraries with different parsers, so a vocabulary gap is undetectable at runtime and the pin is a commit ([07](07-invariants-and-limits.md#the-version-trap)). |
| **Pin bump** | Moving the submodule to a newer DualC commit and rebuilding the native side — the only way to reach a new DualC op; "re-vendor" in a dated record is the same step in the committed-binary era. |
| **Gate** | `scripts/check.py`, the one command CI and the developer both run: the docs contract, the native side present, the build, the test floor. |

## The ID vocabulary

Five numbering schemes coexist in the docs and are written differently on purpose; prose
always uses the qualified form.

- **`#N` — a Boletus tracked item.** One sequence across every roadmap block, assigned at
  birth and never reused: `#20` is the strut-lattice item of block 08, `#21`–`#31` are block
  09's (the last three its post-close children), `#32`–`#34` block 10's.
  `rg -n '#(2[1-9]|3[0-4])' docs/` finds every citation of an item.
- **`D-NN` — a Boletus decision**, a row of [`docs/decisions/`](../decisions/README.md):
  one sequence given by decision date, never reused; a decision that lands or is dropped
  keeps its row and its ID, only the status cell changes.
- **`DualC #N` / `DualC D-NN` — DualC's items and decisions**, always written with the
  prefix: a bare `#17` is a Boletus item, `DualC #17` is DualC's strut-lattice item, and
  `DualC D-12` is its DAG-ref decision. Boletus never restates DualC's status; it links.
- **`NN-slug` — a file or folder number** in every docs tree, and **letter IDs** for the
  recipes on a command-reference page; both assigned at birth and never renumbered
  ([`docs/README.md` § Conventions](../README.md#conventions)). A roadmap page cited as
  `07 § 6` is the section of that number on file `07`; `09/02` is the second child of the
  `09-docs-layers/` folder.
- **Phases** — `Phase 2`, `3a`, `3b`, `5` are the *project* phases of the roadmap index;
  `Phase A` / `Phase C` are the phases of the `Write to File` plan inside block 05; `Phase
  0`–`5` of block 09 are the docs restructuring's. A phase is always named with its block
  when the block is not obvious.

---

← Back to the [design index](README.md) · the [docs index](../README.md)
