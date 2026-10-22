# Mesh → Volume

*Nickname `Mesh`.* Turns a Rhino mesh into a `Volume` so it can be clipped, booleaned or
shelled. The triangles stay **in memory** inside the `Volume` (quads are split in two) and
reach DualC in RAM at the terminal — no temp file
([design 03](../../design/03-volume-and-resolvers.md)). **The output is always metric**: the two
closed-mesh kinds are true signed-distance fields and the Winding kind is wrapped in
`normalize` inside the component, so a `Normalize` after `Mesh → Volume` is never wired.

| Input | Type | Default | Meaning |
| --- | --- | --- | --- |
| Mesh (`M`) | Mesh | — | The Rhino mesh to convert. |
| Kind (`K`) | Integer, dropdown | Mesh (closed, parity) (0) | **Mesh (closed, parity) (0)** — a signed-distance field from a watertight mesh; three probe rays with a majority vote, the robust default. **Mesh (closed, pseudonormal) (1)** — the same result on clean input from one closest-point query; faster, no extra robustness on a broken mesh. **Winding (open / soup) (2)** — a winding-number field that tolerates open shells and triangle soup and seals holes. |

| Output | Type | Meaning |
| --- | --- | --- |
| Volume (`V`) | Volume | The mesh volume (always metric). |

Messages: Warning `No mesh connected.`; Error `Mesh has no vertices/faces.`; Error
`Unknown kind ….`. What the three kinds are in the engine:
[DualC 11/01 § Sources](../../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md#sources).

---

← Back to [Family 1: Sources](README.md) · the [command reference](../README.md)
