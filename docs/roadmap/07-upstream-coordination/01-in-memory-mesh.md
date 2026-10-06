# 07 — Upstream coordination: The in-memory mesh-source resolver

Part of [07 — Upstream coordination with DualC](README.md); every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4). Section numbers are the stable addresses.

## 1. In-memory mesh-source resolver — *DONE (DualC v0.3.0 upstream + the Boletus-side flip, both 2026-06-19)*

What this handoff is for — carrying a user's *input* triangles into DualC in RAM instead of
through a temp file, while the wire itself stays the meshless `Volume` and the one output mesh
is still made once at the terminal — is the present-tense contract of
[design 03](../../design/03-volume-and-resolvers.md); the `DualcMeshSource` struct and the two
`*_with_meshes` create calls the ABI gained at v0.3.0 (the surface grew from 9 to 11 entry
points) are DualC's, in [`capi/README.md`](../../../../DualC/capi/README.md) and
[14/02 § 3](../../../../DualC/docs/roadmap/14-c-abi/02-surface-and-contract.md#3-the-surface--the-graph-string-is-the-construction-api).
*(2026-09-21, roadmap 09 Phase 4: the clarification paragraph and the copied C declaration
that stood here became these links.)*

**Landed in DualC (v0.3.0):** `mesh(path=…)`/`winding(path=…)` no longer force a disk round-trip.

- `mesh`/`winding` reference a host buffer by **id** (`mesh(id="…")`/`winding(id="…")`); exactly
  one of `path`/`id` per node; `path=` works unchanged. `mesh` keeps `sign`/`normals`; `winding`
  for soup/open shells.
- **Byte-identical to the OBJ path** for the same geometry (proven by DualC's `cli_c_abi_mesh_inmem`,
  which Boletus mirrors with the cube golden-count test).
- **Lifetime — *better* than the original ask.** The buffers are **read and copied during the create
  call only** — they need **not** outlive the `DualcField`. (The original ask said "must outlive the
  field"; DualC's eager-copy design relaxes that.) For the C# marshaling this means **pin the
  managed arrays for the duration of the call only** (e.g. `GCHandle.Alloc(…, Pinned)` released
  right after), not for the field's lifetime.
- The optional `normals` field is **ignored** (DualC derives normals from geometry via the node's
  `smooth`/`sharp` option) — pass `IntPtr.Zero`.
- DualC docs updated: `capi/dualc_c.h`, `capi/CSHARP_WRAPPER_HANDOFF.md` (§4 — the C# marshaling
  spec + the `DualcMeshSourceNative` struct), `docs/command_reference/11-dualc_field/`,
  `docs/roadmap/14-c-abi/` (later entries: `18-c-abi-continued.md` — *2026-09-21: retired upstream, its entries are `14-c-abi/04-abi-0-4-0.md`*); version bumped 0.2.0 → **0.3.0**.

**Explicitly NOT needed (and not added):** per-op incremental field composition / node-handle
factories — the host composes the whole graph symbolically and instantiates once; only the
in-memory mesh handoff was required.

**Boletus side — DONE (2026-06-19):**
1. ✅ Vendored the rebuilt **0.3.0** `dualc_capi.dll` (commit `e345bf3`) into `native/x64/`;
   provenance bumped in [`native/README.md`](../../../native/README.md) (11 ABI symbols).
2. ✅ Added the two `*_with_meshes` P/Invoke decls + the blittable `DualcMeshSourceNative`
   (`NativeMethods.cs`) mirroring `DualcMeshSource`'s field order/types (default sequential layout,
   48 bytes on x64; `out DualcFieldHandle` over the handoff's `out IntPtr` to match the SafeHandle style).
3. ✅ Implemented **`DualcField.FromJson(json, meshes)`** to pin each `MeshBuffer`'s vertices /
   triangles / UTF-8 id for the create call and call `dualc_field_create_from_json_with_meshes`,
   freeing the `GCHandle`s in `finally`. `mesh`/`winding` now accept an `id` param (`Ops`/`Field`/
   `FieldGraphValidator`, exactly-one-of path/id). The terminal swaps `mem://` → `id=` via
   `VolumeResolver` and calls the in-memory create. **`Volume`, `VolumeResolver`, and the shared
   `FieldTree.Rewrite` skeleton live in `Boletus.Core`** (they are Rhino-free; only
   `VolumeGoo`/`VolumeParameter` stay in the `.gha`) — so the rewrite is unit-testable on the plain
   runner. (The obsolete temp-OBJ materializer was removed; recoverable from git if a pre-0.3.0 DLL
   ever needs supporting.)
4. ✅ **Two acceptance gates, both automated (82/82 Core suite green).** (a) The cube P/Invoke gate —
   `MeshBufferTests.InMemory_mesh_source_hits_the_same_golden_counts_as_the_temp_file` asserts the
   in-memory path hits the same 70,032 v / 120,612 t as the temp-file baseline, covering the **Core
   marshaling** (struct layout, GCHandle pinning, `int[]`→`uint32*`, id resolution). (b) The rewrite
   gate — `VolumeResolverTests` covers `VolumeResolver.Resolve` (mem:// → `id=` swap, buffer
   collection/dedup, missing-buffer error, serialize-and-validate) and the `FieldTree.Rewrite`
   skeleton, with synthetic `mem://` graphs — no native engine, no Rhino.

The input-mesh workflow is **fully diskless**, and both the marshaling boundary and the GH-terminal
rewrite are covered by automated tests. (A manual Rhino smoke test of the full canvas remains a
nice-to-have — [05 § Verification](../05-phase3-grasshopper-components/01-volume-datatype-and-palette.md#verification) — but no longer gates the
rewrite logic.)

---

← Back to the [Upstream coordination index](README.md) · the [Roadmap index](../README.md).
