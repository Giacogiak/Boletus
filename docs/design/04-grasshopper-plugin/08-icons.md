# The icons

`BoletusIcons.cs` draws every glyph **procedurally** with `System.Drawing` GDI+ primitives —
no image files, no embedded resources, no generator, no build step; the whole icon set is
reviewable C# and reproducible from source. Twenty-three `Bitmap` accessors: one per component
(`Write to File` draws the `ContourExport` glyph), one for the `Volume` parameter and one for
the assembly (the boletus mushroom on the ribbon tab and in the plugin manager). Each
accessor renders a **fresh 24×24 `Bitmap`** rather than a cached instance: Grasshopper
caches per owner and may dispose what it was handed, so a shared bitmap would be disposed out
from under its other owners (the resx pattern returns a fresh clone for the same reason).
House style: one warm accent (`#C8743C`, with two darker shades of it for depth) and a neutral
dark grey (`#3C3C3C`) on a transparent
ground, ~2 px padding, strokes ≥ ~1.4 px, shapes near the pixel grid — simple flat glyphs,
legible at palette size.

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
