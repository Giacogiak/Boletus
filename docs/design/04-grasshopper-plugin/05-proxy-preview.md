# `Proxy preview` — the capped viewport LOD

`ProxyPreviewComponent.cs` draws a coarse contour of a volume directly in the viewport,
**viewport-only, no Mesh output**: the requested depth is clamped to `MaxProxyDepth`
([07](../07-invariants-and-limits.md#maxproxydepth)) with a remark when it clamps, and the
component contours **once in `SolveInstance`** through `VolumeResolver` +
`FromJson(json, meshes)`, caches the `Rhino.Geometry.Mesh` and its bounding box, and only
draws the cache from `DrawViewportMeshes` (which fires on every pan, zoom and rotate);
`ClippingBox` returns the cached box so Zoom Extents frames it, and a failed or empty solve
clears the cache. On a library that `SupportsDiagnostics` the contour goes through the
diagnostics twin ([01](../01-native-interop.md#the-public-surface--dualcfield)): an **empty
contour** — the engine's one-triangle placeholder for a field with no surface inside the
sampled region — posts a warning and draws nothing, and a proxy that is **not watertight**
(boundary edges, as where Min / Max cut through the surface, or non-manifold ones) posts a
remark carrying both edge counts; on an
older DLL the plain contour runs and nothing is said. Min / Max bounds are needed only for an unbounded field, with a friendly
`DualcStatus.Bounds` message like the writer's — two near-identical strings, both quoted on
the command reference. `RhinoMeshConvert.ToRhinoMesh`
(vertices from the `Positions` triples, faces from the `Indices` triples, normals from
`Normals` or `ComputeNormals()`) is the one Rhino-side copy, shared by whoever needs a Rhino
mesh. Lattice *fidelity* is not this component's job — a coarse contour of a dense lattice
drops thin walls; that is what the side-car is for.

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
