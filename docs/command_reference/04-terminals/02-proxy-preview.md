# Proxy preview

that cannot exhaust memory: the depth is clamped to a hard ceiling of **7**, however high it
is dialled ([design 07 § `MaxProxyDepth`](../../design/07-invariants-and-limits.md#maxproxydepth)).

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Volume (`V`) | Volume | — | Volume to preview. |
| Depth (`D`) | Integer | 5 | Maximum octree depth; clamped to 1…7, with a remark when the clamp bites. |
| Min (`Min`) | Point | *(optional)* | Bounds minimum corner, only for an unbounded field. |
| Max (`Max`) | Point | *(optional)* | Bounds maximum corner. |

No outputs: it draws into the viewport, and Zoom Extents frames it.

It contours **once** per solve and caches, so pan and zoom stay smooth
([design 04 § Proxy preview](../../design/04-grasshopper-plugin/05-proxy-preview.md#proxy-preview--the-capped-viewport-lod)).
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

---

← Back to [Family 4: Terminals](README.md) · the [command reference](../README.md)
