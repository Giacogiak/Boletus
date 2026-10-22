# The wire type — `VolumeGoo` / `VolumeParameter`

`VolumeGoo.cs` is the `GH_Goo<Volume>` payload on every wire; `Duplicate()` shares the
immutable volume. `VolumeParameter.cs` is the `GH_Param<VolumeGoo>` (name `Volume`, nickname
`V`, `Exposure = hidden`: it is computed, never placed from the palette or persisted on the
canvas). Both are thin wrappers over the Rhino-free type of [03](../03-volume-and-resolvers.md).

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
