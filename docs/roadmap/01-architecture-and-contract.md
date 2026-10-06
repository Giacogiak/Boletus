# 01 — Architecture & the C-ABI contract

> Detail for the [roadmap](README.md). The layering Boletus is built on, the pivotal
> "graph string is the API" property, and the authoritative C-ABI contract the
> `Boletus.Core` wrapper targets.

## The layering

```
Boletus.Grasshopper (.gha, net7.0-windows)     ← Phase 3 — GH components + Rhino glue
        │ project reference
Boletus.Core (netstandard2.0, no Rhino dep)    ← Phase 2 — P/Invoke wrapper + field-graph model/serializer
        │ P/Invoke (Cdecl, x64)
dualc_capi.dll (native C ABI)                  ← Phase 1 — DONE upstream; vendored into Boletus
        │ statically links
libdualc + field-graph parser (nlohmann/json) + STL/3MF/OBJ writers (miniz)
```

- **`Boletus.Core`** — a thin, idiomatic, **Rhino-free** managed layer: P/Invoke over the
  9 native entry points, plus the managed field-graph model and its canonical-JSON
  serializer (Phase 3a). Rhino-free so it is unit-testable on a plain test runner and
  reusable by any non-Rhino consumer.
- **`Boletus.Grasshopper`** — the `.gha`: Grasshopper components + Rhino glue (native-DLL
  resolver, Rhino↔DualC mesh conversion, temp-file mesh export, unit scaling).
- **`dualc_capi.dll`** — the native bridge, **DONE upstream**. Self-contained (statically
  links `libdualc`, geometry-central, the field-graph parser, and the export writers), so
  the only runtime dependencies are the VC++ runtime + Windows UCRT. Vendored into Boletus
  (see [02](02-dependency-strategy.md)).

## The pivotal property — the graph string *is* the construction API

DualC's field-graph parser is already a complete construction API: every primitive,
boolean, decorator, TPMS, and mesh/winding source — the exact vocabulary `dualc_field`
accepts as canonical JSON or the terse `--expr` shorthand. So the C ABI exposes **no
per-primitive factories**; a host composes a shape by sending **one string** and getting
back a proxy mesh and/or a file export.

Consequences for Boletus:

- The **native-interop layer is near-mechanical** (Phase 2, done) — 9 functions, no
  growing factory wall as DualC's vocabulary expands.
- The **real work is the managed serializer** (Phase 3a): each GH component builds a node
  in a managed field-graph model, and the canvas serializes to the one string the wrapper
  sends. This is where op names / params / defaults must be pinned and verified.
- ABI **stability**: new DualC ops grow the *string vocabulary*, not the C surface — the
  wrapper doesn't change when DualC adds a primitive.

## The authoritative contract (`capi/dualc_c.h`, 11 entry points)

Source of truth: `D:\DualC\capi\dualc_c.h` + `CSHARP_WRAPPER_HANDOFF.md`; full record
`D:\DualC\docs\roadmap\14-c-abi\`. (9 entry points at Phase 1; **11 since DualC v0.3.0**,
which adds the two `*_with_meshes` in-memory-mesh create twins — last two rows below.) Every
fallible call returns an `int` status
(`DUALC_OK == 0`); **no C++ exception ever crosses the boundary**. On failure an optional
`err` buffer receives a NUL-terminated message — and for `DUALC_ERR_GRAPH` a **locator**
(a JSON pointer like `/root/in/0/radius` for JSON, a character offset for `--expr`).

| Function | Purpose |
|---|---|
| `const char* dualc_version(void)` | Version string, e.g. `"dualc 0.3.0"`; never NULL. |
| `void dualc_default_params(DualcContourParams*)` | Library defaults: `maxDepth=7, minDepth=3, collapse=0, hasBounds=0, manifold=1, numThreads=0`. |
| `int dualc_field_create_from_json(const char*, DualcField**, char* err, int errlen)` | Build a field from canonical JSON. |
| `int dualc_field_create_from_expr(const char*, DualcField**, char* err, int errlen)` | Build a field from `--expr` shorthand (identical result tree). |
| `int dualc_field_create_from_json_with_meshes(const char*, const DualcMeshSource*, int meshCount, DualcField**, char* err, int errlen)` | **(v0.3.0)** Build from JSON + host in-memory mesh buffers the graph references by `mesh(id=…)`/`winding(id=…)`. Buffers copied during the call. |
| `int dualc_field_create_from_expr_with_meshes(const char*, const DualcMeshSource*, int meshCount, DualcField**, char* err, int errlen)` | **(v0.3.0)** The `--expr` twin. |
| `void dualc_field_destroy(DualcField*)` | Release a field handle (and meshes it loaded). |
| `int dualc_field_contour(DualcField*, const DualcContourParams*, DualcMesh* out, char* err, int errlen)` | Contour to an ABI-owned flat mesh. **`maxDepth` is the proxy lever.** |
| `void dualc_mesh_release(DualcMesh*)` | Free a contoured mesh and zero the struct. |
| `int dualc_field_export(DualcField*, const char* path, const DualcContourParams*, char* err, int errlen)` | Contour + write a file; format by extension. |
| `int dualc_field_export_tiled_stl(DualcField*, const char* path, const DualcContourParams*, int tileDepth, char* err, int errlen)` | Streaming STL for dense parts (bounded RAM). |

**Status codes:** `OK`=0, `ERR_BOUNDS`=1 (unbounded field → set `hasBounds`+bounds),
`ERR_IO`=2, `ERR_GRAPH`=3 (parse/build + locator), `ERR_USAGE`=4 (bad arg, e.g. NULL),
`ERR_UNKNOWN`=5.

### Structs

`DualcContourParams` (flat; the `dualc_field` CLI flags):
`int maxDepth; int minDepth; double collapse; int hasBounds; double boundsMin[3];
double boundsMax[3]; int manifold; unsigned numThreads;` — 80 bytes on x64 (note the
4-byte pad after `hasBounds`).

`DualcMesh` (ABI-owned; release with `dualc_mesh_release`):
`float* positions` (3·vtx), `float* normals` (3·vtx, **index-aligned, unit**),
`uint32_t* indices` (3·tri, **0-based, fan-triangulated**), `uint32_t vertexCount`,
`uint32_t triangleCount`.

`DualcMeshSource` **(v0.3.0; host-owned, read+copied during the create call only)** — one
input-mesh leaf the graph references by id: `const char* id; const float* vertices;
uint32_t vertexCount; const uint32_t* indices; uint32_t triangleCount; const float* normals;`.
The C# `DualcMeshSourceNative` must mirror this **field order** exactly (default sequential
layout). `normals` is **currently ignored** (DualC derives normals from geometry) — pass NULL.

## Contract rules that shape the design

- **Input-mesh leaves reach DualC in RAM (no disk) — v0.3.0.** When a volume is *defined by*
  an imported mesh (a clip body / boolean operand), that leaf's raw triangles are handed to
  DualC via `mesh(id=…)`/`winding(id=…)` + the `*_with_meshes` create calls — **no temp file**.
  This passes only the user's **input** geometry; it triggers **no contouring** (DualC wraps it
  in a lazily-evaluated `MeshSource`), and the only output mesh is still made once at
  Contour/Export. Buffers are **copied during the create call**, so pin the managed arrays for
  the call only (they need not outlive the field). This is the active path in Boletus as of
  2026-06-19. *Legacy fallback (pre-0.3.0 DLL only):* export the mesh to a temp file and reference
  `mesh(path=…)`, deleting it right after `create` returns. See [07 §1](07-upstream-coordination/01-in-memory-mesh.md#1-in-memory-mesh-source-resolver--done-dualc-v030-upstream--the-boletus-side-flip-both-2026-06-19).
- **Proxy vs. export is one lever** — `maxDepth`. Coarse = cheap drawable proxy; full =
  export-grade. A coarse proxy of a *lattice* is inherently **lossy** (thin walls drop
  out); faithful for boundary/solid parts and framing. True live lattice preview is the
  side-car's job (later).
- **Threading:** a `DualcField` is **single-threaded**. Create one handle per Grasshopper
  solve; do not share across parallel solves. Independent handles are independent.
- **Lifetime:** mesh lifetime is independent of the field; you may contour the same field
  repeatedly at different depths. Always `dualc_mesh_release` after copying out.
- **Units:** `.3mf` export is **1 unit = 1 mm** — plan a Rhino-doc-units → mm scaling step
  in the GH export component.

**Note (2026-09-21, roadmap 09 Phase 3).** This page is the Phase-1/Phase-2 record: its "9
entry points" figures are the count at the time. The present-tense contract the wrapper binds —
every entry point of the pinned ABI, the structs, the pinning rule — is
[design 01](../design/01-native-interop.md); the DLL's provenance is `native/README.md`.
*(2026-09-21, roadmap 09 Phase 4: the units rule is DualC's — [design 09 § Units and frames](../../../DualC/docs/design/09-conventions.md#units-and-frames)
— and the scaling step planned in the list was never built: the canvas numbers are the millimetres
in the file, [design 06 § Units](../design/06-conventions.md#units).)*
*(2026-09-21, roadmap 09 Phase 5, the first semantic lint: § The layering, § Contract rules that shape the
design and § Marshaling facts are likewise the Phase-2 record; the present tense is
[design README § The layering](../design/README.md#the-layering) and
[design 01](../design/01-native-interop.md).)*

## Marshaling facts (netstandard2.0)

- **x64, `__cdecl`, `extern "C"`** — declare `CallingConvention.Cdecl`; symbols are
  unmangled (verified via `dumpbin /exports`).
- Strings are **UTF-8** `const char*`. netstandard2.0 lacks `[LibraryImport]` /
  `Marshal.PtrToStringUTF8`, so the wrapper marshals NUL-terminated UTF-8 `byte[]`
  manually (incl. reading the version string with `Marshal.ReadByte`).
- The flat structs are **blittable** when the `double[3]` arrays are split into scalars —
  no custom marshaling, exact layout match.

---

← Back to the [roadmap](README.md) · next: [02 — Dependency strategy](02-dependency-strategy.md)
