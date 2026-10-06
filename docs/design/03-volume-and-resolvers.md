# `Volume` and the two resolvers

The runtime currency of the canvas and the two Rhino-free rewrites that turn it into what a
consumer can read: the in-process DLL, which takes an input mesh as RAM buffers, and the
out-of-process GPU viewer, which can only read a file. All of it lives in `Boletus.Core`
so the terminal logic unit-tests on the plain runner; only the Grasshopper wrappers
(`VolumeGoo` / `VolumeParameter`, [04](04-grasshopper-plugin.md)) belong to the `.gha`.

## `Volume`

`src/Boletus.Core/Volume.cs` — the meshless, lightweight representation that flows along
every wire ([README § The `Volume` contract](README.md#the-volume-contract)):

- **`Core`** — the symbolic field, a `FieldNode` tree ([02](02-field-graph-model.md)).
  Passing it between components is free; a decorator or boolean component builds a new tree
  around it and never evaluates anything.
- **`Meshes`** — `IReadOnlyDictionary<string, MeshBuffer>`, the in-memory geometry of the
  graph's `mesh` / `winding` leaves, keyed by **content hash**. A volume with no mesh leaves
  carries an empty map.
- **`MemoryScheme`** = `"mem://"`. An in-memory leaf is written by its source component as
  `mesh(path="mem://<id>")` — a placeholder in the `path` parameter that no consumer sees:
  a resolver rewrites it before the graph leaves the process.
  `Volume.TryGetMemoryMeshId(node, out id)` is the single source of truth for recognising
  such a leaf (`mesh` or `winding`, a `Text` `path` with the scheme prefix).
- **`WithCore(node)`** keeps the buffers under a new tree; **`Combine(core, inputs…)`**
  merges the buffer maps of every input — keys are content hashes, so the same mesh used
  twice dedupes to one buffer. Every multi-input component (`Boolean`, `Onion` with a
  boundary, `Graded Onion`, `Graded Offset`, `Mix`) builds its output this way.

Two senses of "mesh" never mix here. The buffers are the user's **input** triangles — a clip
body, a boolean operand — never contoured, never a DualC product, and there is no lighter
faithful representation of an arbitrary input surface than its triangles (an SDF grid would
be heavier; DualC evaluates the mesh lazily as a distance field and pre-bakes nothing). The
**output** mesh exists only at a terminal.

## `MeshBuffer`

`MeshBuffer.cs` is the Rhino-free triangle soup: `Vertices` (`float[]`, three per vertex),
`Triangles` (`int[]`, three 0-based indices per face, bit-identical to the ABI's `uint32*`),
optional `Normals`, the `VertexCount` / `TriangleCount` accessors, `ContentHash()` — the id
under which a buffer is keyed and referenced: a 64-bit FNV-1a over the two lengths, the
vertex floats and the triangle indices (not the normals), so identical meshes get one id and
dedupe, as DualC's per-build mesh cache does; a fast stable key, not a cryptographic hash — and `WriteObj(path)`, the writer the
materializing resolver uses. `Mesh → Volume` flattens a Rhino mesh (triangles and quads) into
one of these; a quad becomes two triangles.

## `VolumeResolver` — `mem://` → `id=`, the diskless path

`VolumeResolver.Resolve(volume)` returns `(Root, Meshes)`: the tree with every in-memory leaf
rewritten from `path="mem://<id>"` to `id="<id>"` (the `path` key removed), and the subset of
the volume's buffers the graph **actually references**, deduped. A leaf whose id has no
buffer attached is an `InvalidOperationException`; a disk-backed `mesh(path=…)` passes through
untouched. No disk is touched, so nothing needs disposing. The result feeds
`DualcField.FromJson(json, meshes)`, which pins the buffers for the create call only
([01 § Buffers are pinned for the call only](01-native-interop.md#buffers-are-pinned-for-the-call-only)):
the buffers are copied inside DualC during the call and the graph references them by id
through the `*_with_meshes` entry points. This is the path every in-process terminal takes —
`Write to File` and `Proxy preview` — and it is why an input mesh **never touches disk** on the
way to the engine. Contouring through this path and through a `mesh(path="cube.obj")` on disk
yields the same golden counts ([07](07-invariants-and-limits.md#the-golden-contour-counts)),
which is the gate on the marshaling.

## `MeshMaterializingResolver` — `mem://` → `path=`, the side-car path

`MeshMaterializingResolver.MaterializeToDisk(volume, destDir)` is the viewer twin. DualC's
`dualc_field_view` is a **separate process** fed a text graph, and the in-memory mesh channel
reaches only the in-process DLL, so a leaf the viewer must see is written to a temp OBJ:
`destDir/mesh_<id>.obj` (the directory created on first need), the `path` key kept and its
value replaced by the absolute, **forward-slash** path — the form the DualC parser reads
without escaping questions. Identical buffers share one file and are written once — the id is
a content hash, so a mesh used as both clip and skin bakes once. A mesh-free graph is returned
**unchanged, by reference**, and no file is written. The `Live Preview` terminal owns the
directory and its cleanup ([04 § Live Preview — the process model](04-grasshopper-plugin.md#live-preview--the-process-model)).

## `FieldTree.Rewrite` — the shared skeleton

Both resolvers are one `map` function over `FieldTree.Rewrite(node, map)`
(`FieldGraph/FieldTree.cs`): a bottom-up immutable rewrite that rebuilds a node only when
the map changed it or one of its children, so an untouched subtree keeps its reference
identity. `FieldTree.CopyParams` gives a map a mutable copy of a node's parameter bag. The
rewrite is unit-tested directly, and each resolver on synthetic `mem://` graphs with no
engine and no Rhino.

---

← Back to the [design index](README.md) · the [docs index](../README.md)
