# Boletus — Design

The compiled description of the plugin as it is: *what the code does and why*, rewritten
freely whenever the code changes and dated nowhere. How it got this way — what changed
when, the measurements, the rejected approaches — is [`docs/roadmap/`](../roadmap/README.md);
what was decided, dropped or deferred is [`docs/decisions/`](../decisions/README.md); how to
drive each component is [`docs/command_reference/`](../command_reference/README.md) (the usage contract). An engine fact — an
op token, an ABI signature, a contouring invariant — is DualC's and is linked to DualC's own
pages (`external/DualC/docs/README.md` says which), never restated here. That these pages still
describe the code is checked by the owner's `/docs-semantic-lint` procedure — the gate proves
shape, the lint proves meaning — whose dated runs are
[roadmap 09/06](../roadmap/09-docs-layers/06-semantic-lint/README.md).

Boletus is the .NET / Rhino 8 / Grasshopper front-end for **DualC**, the C++ implicit-field
engine (public at `https://github.com/Giacogiak/DualC`, consumed as the git submodule
`external/DualC`). Grasshopper components compose a field graph on the canvas; the one
JSON string it serializes to is the construction API of DualC's C ABI (`dualc_capi.dll`),
which contours it to a proxy mesh or writes it to a file. There are **no per-primitive
factories** to wrap: the field-graph *string* is the API, so the native-interop layer is thin
and the real work is the managed field-graph model the components feed.

## The layering

```
Boletus.Grasshopper (.gha, net7.0-windows)     ← the component palette + Rhino glue
        │ project reference
Boletus.Core (netstandard2.0, no Rhino dep)    ← P/Invoke wrapper + field-graph model and serializer
   = DualcField / FieldGraph/ / Volume            + the Rhino-free Volume datatype and its resolvers
        │ P/Invoke (Cdecl, x64)
dualc_capi.dll / libdualc_capi.so (C ABI)     ← DualC, built from external/DualC into native/x64/ (Windows), native/linux-x64/
        │ statically links
libdualc + field-graph parser + STL/3MF/OBJ writers
```

- **`Boletus.Core`** is deliberately **Rhino-free**: it unit-tests on a plain runner and is
  reusable by any non-Rhino consumer. It holds the P/Invoke wrapper
  ([01](01-native-interop.md)), the field-graph model and canonical-JSON serializer
  ([02](02-field-graph-model.md)), the `Volume` datatype with its two resolvers
  ([03](03-volume-and-resolvers.md)) and the pure export planner
  ([05](05-export-planning.md)). Logic goes here; the `.gha` stays a thin shell.
- **`Boletus.Grasshopper`** is the `.gha`: the components, the `Volume` wire type, the
  native-DLL resolver installed at plugin load, Rhino↔DualC mesh conversion, the threading
  of the file writer and the process model of the live preview ([04](04-grasshopper-plugin.md)).
- **`dualc_capi.dll`** is the native bridge: DualC's C ABI, built from the submodule by
  `scripts/build_native.py` into the gitignored `native/x64/` — never committed, on the
  developer's machine and in CI alike ([`native/README.md`](../../native/README.md)). It is
  self-contained — `libdualc`, geometry-central, the field-graph parser and the export
  writers are statically linked — so its only runtime dependencies are the VC++ runtime and
  the Windows UCRT. Its Linux twin, `libdualc_capi.so`, is the same C ABI built from the same
  commit into `native/linux-x64/`, depending on libstdc++, libm, libgcc_s and libc only; it
  serves the Core tests (no Rhino runs on Linux)
  ([01](01-native-interop.md#where-the-dll-is-found)). The pin is the submodule's commit. The
  field-graph *parser* is compiled inside the library, which is why the pin is a commit and
  never a version string ([07](07-invariants-and-limits.md#the-version-trap)).
- **`dualc_field_view.exe`**, built beside the DLL by the same script, is DualC's GPU
  raymarch viewer: the live-preview side-car, launched as a separate process, not part of
  the C ABI.

## The `Volume` contract

Every wire between components carries a **`Volume`** — the runtime, in-memory, **meshless**
representation:

```
Volume = { FieldNode Core,  IReadOnlyDictionary<string, MeshBuffer> Meshes }
```

DualC's native "volume" *is* the field expression tree — symbolic and lightweight — and
Boletus mirrors it as a `FieldNode`, so passing a `Volume` between components is free: a
`Boolean`, `Onion` or `Transform` component is pure tree surgery, and **nothing is baked, no
mesh is generated and no disk is touched** until a terminal consumes the graph. Two senses of
"mesh" are kept apart. The **output mesh** — the heavy dual-contoured result — is produced
once, at a terminal. An **input-mesh leaf** — a user's imported clip body or boolean
operand — is the user's own raw triangles, carried by reference as a Rhino-free `MeshBuffer`
keyed by content hash and carried in the graph as a `mem://<hash>` leaf that a resolver
rewrites to `mesh(id=…)` / `winding(id=…)` at an in-process terminal ([03](03-volume-and-resolvers.md)); it is
never contoured and never a DualC product. The boundary clip is **not** part of the type: it is
an explicit `intersection` emitted by the `Onion` / `Graded Onion` / `Boolean` components,
so the onion-before-clip order is visible on the canvas
([06](06-conventions.md#onion-before-clip)). How an input-mesh leaf reaches DualC — in RAM
for the in-process DLL, as a temp OBJ for the out-of-process viewer — is
[03](03-volume-and-resolvers.md).

## The pages of this layer

| Page | Holds |
| --- | --- |
| [01-native-interop.md](01-native-interop.md) | `DualcField`, the two entry-point probes and the `DualcDiagnostics` overloads, the `SafeHandle`s, the blittable structs, UTF-8 marshaling, status → exception, the entry points as a link to DualC, the single-threaded rule, where the built library is found |
| [02-field-graph-model.md](02-field-graph-model.md) | `FieldNode` / `FieldValue`, the serializer's byte-match rules, `Ops` / `OpSchema`, the `Field` builders, the validator, metric vs non-metric, tree-only |
| [03-volume-and-resolvers.md](03-volume-and-resolvers.md) | `Volume`, `MeshBuffer`, `FieldTree.Rewrite`, `VolumeResolver` (`mem://` → `id=`), `MeshMaterializingResolver` (`mem://` → `path=`), the pinned-for-the-call-only rule |
| [04-grasshopper-plugin.md](04-grasshopper-plugin.md) | the `.gha` project, the DLL resolver, `VolumeGoo` / `VolumeParameter`, the component families, the proxy cap and its empty-contour warning, the `Write to File` threading model, the `Live Preview` process model, the icons |
| [05-export-planning.md](05-export-planning.md) | `ExportPlan`: path and extension rules, the strategy table, the tile-depth default, tiled STL and monolithic 3MF |
| [06-conventions.md](06-conventions.md) | units, metric-by-default sources and the Normalize rule, onion-before-clip, message levels, the dropdown-documentation rule, the GUID rule, provenance by commit, logic in Core |
| [07-invariants-and-limits.md](07-invariants-and-limits.md) | the golden contour counts (the one home), the coarse-depth rule, `MaxProxyDepth`, the version trap, single-flight writes, the cancel constraint, the test floor |
| [08-glossary.md](08-glossary.md) | one line per term, and the ID vocabulary (`#N`, `DualC #N`, `DualC D-NN`, `D-NN`, `NN-slug`) |

## Things we do not do

Standing decisions that shape the code and the docs; each is a one-liner here, a row in the
[decisions index](../decisions/README.md) — or DualC's, where the decision is the engine's —
and a record in the roadmap.

- **Public source, built binaries.** The repository is public and CI runs the gate on every
  push, but no native binary is ever committed — they are built from the DualC submodule —
  and nothing is pushed to the Yak server or a NuGet feed without the owner's explicit
  say-so ([06](06-conventions.md#local-only)).
- **No mesh between components.** A wire carries a `Volume`; the one output mesh is made at a
  terminal ([§ The `Volume` contract](#the-volume-contract)).
- **No temp files for input-mesh leaves** on the in-process path: they reach the DLL as
  pinned RAM buffers through the `*_with_meshes` create calls; only the out-of-process viewer
  gets a temp OBJ ([03](03-volume-and-resolvers.md)).
- **No per-primitive ABI factories.** The graph string is the construction API (DualC D-20);
  a new DualC op grows the vocabulary in `Ops.cs` / `Field.cs`, never the C surface
  ([01](01-native-interop.md)).
- **No line numbers in this layer.** A design page names a file and a symbol; a line pin
  drifts with the next edit.
- **No renumbering, no restated counts, no rewritten headings**: IDs are assigned at birth and
  a count lives only with its owner ([`docs/README.md` § Conventions](../README.md#conventions)).
- **No search index to keep in sync** — the folder READMEs plus `rg` are the search layer
  ([`docs/README.md` § How to find things](../README.md#how-to-find-things)).
- **No reflow of `docs/raw/`**: an import is never edited; a correction is a new dated file
  ([`docs/raw/README.md`](../raw/README.md)).
