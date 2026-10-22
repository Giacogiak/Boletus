# The Grasshopper plugin — `Boletus.Grasshopper`

The `.gha`: the component palette, the `Volume` wire type, the Rhino glue and the two pieces
of machinery that are not pure tree surgery — the background file writer and the external
viewer process. Everything with logic in it lives in `Boletus.Core`
([README § The layering](../README.md#the-layering)); this project wires Grasshopper inputs to
the `Field` builders and the resolvers, and owns only what needs Rhino, a thread or a
process. What each component's inputs mean, their defaults and the exact messages they post
is the command reference's, not this page's.

| File | Section |
| --- | --- |
| [01-the-project.md](01-the-project.md) | The project — the TFM, the compile-only Rhino references, the four shipped files |
| [02-native-dll-resolver.md](02-native-dll-resolver.md) | The native-DLL resolver installed at plugin load |
| [03-wire-type.md](03-wire-type.md) | The wire type — `VolumeGoo` / `VolumeParameter` |
| [04-component-families.md](04-component-families.md) | The component families — what each sub-panel holds, the dropdown pattern |
| [05-proxy-preview.md](05-proxy-preview.md) | `Proxy preview` — the capped viewport LOD |
| [06-write-to-file.md](06-write-to-file.md) | `Write to File` — the threading model |
| [07-live-preview.md](07-live-preview.md) | `Live Preview` — the process model |
| [08-icons.md](08-icons.md) | The icons — procedural GDI+ glyphs, the house style |

---

← Back to the [design index](../README.md) · the [docs index](../../README.md)
