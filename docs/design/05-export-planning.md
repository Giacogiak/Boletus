# Export planning — `ExportPlan`

The deterministic half of writing a file: given the raw inputs of the `Write to File`
terminal, which path is written, by which native writer, at which tile depth, and which
advisories the user sees. It is pure and native-free — `src/Boletus.Core/Export/ExportPlan.cs`,
unit-tested on the plain runner (`tests/Boletus.Core.Tests/ExportPlanTests.cs`) — the same
"logic in Core, thin `.gha`" split as the resolvers ([03](03-volume-and-resolvers.md)). The
component owns only the threading, the actual native calls and what the engine reports
back from them — the empty-contour warning after a monolithic write
([04 § Write to File](04-grasshopper-plugin.md#write-to-file--the-threading-model)); the
inputs table with its defaults and the exact warning strings is the command reference's.

## `ExportPlan.Resolve(path, format, mode, depth, tileDepth?)`

Returns an `ExportPlanResult` — `Path`, `Strategy`, `TileDepth`, `Warnings` — or throws an
`ArgumentException` the component turns into an error and a refusal to write. The rules, in
the order they apply:

1. **A path is required.** Empty or whitespace throws; so does a `Format` or `Mode` value
   outside its domain, checked before anything else.
2. **The format selector is the source of truth for the extension.** `Format` is the
   integer domain `ExportPlan.Format` — `Stl = 0`, `ThreeMf = 1`; any other value throws.
   `Path.ChangeExtension` forces `.stl` or `.3mf`: a bare name gains the extension, and a
   typed extension that disagrees with the selector (`part.3mf` with Format = STL) is
   replaced with a warning. There is no format-by-extension guessing in the reverse
   direction; the user picks the format, the extension follows. The parent directory of the
   *resolved* path must exist; a missing directory throws rather than being created.
3. **The strategy table.** `Mode` is `ExportPlan.Mode` — `Tiled = 0`, `Monolithic = 1`;
   any other value throws.

   | Format | Mode | `ExportStrategy` | Native writer |
   | --- | --- | --- | --- |
   | STL | Tiled | `TiledStl` | `DualcField.ExportTiledStl` — streamed, RAM bounded by one tile |
   | STL | Monolithic | `Monolithic` | `DualcField.Export` — the whole mesh in RAM |
   | 3MF | either | `Monolithic` | `DualcField.Export`; Tiled adds the warning "3MF has no tiled writer; writing monolithically." |

4. **The effective tile depth** is `tileDepth ?? max(1, depth − 2)` — DualC's practical
   rule for a large RAM saving at modest CPU cost. When the strategy is `TiledStl` and the
   effective tile depth is `≥ depth`, a warning says the tiling falls back to a single pass
   with no RAM benefit; it is not an error. For a very shallow `depth` the default floors at
   `1`, where DualC's ghost-ring overhead degenerates; the native call then throws cleanly
   into the component's `DualcException` handling.

## What follows from the rules

- **STL is tiled unless the user opts out.** The default mode is Tiled, so a deep STL export
  cannot exhaust RAM by default; `Collapse` is never set on this path (`DualcContourParams
  .Default()` has it at zero), so its incompatibility with tiling cannot trip. The tiled
  output is bit-identical to the monolithic mesh
  ([07 § Tiled equals monolithic](07-invariants-and-limits.md#tiled-equals-monolithic)).
- **3MF is always monolithic**, because the ABI exposes a tiled writer for STL only; a
  streaming 3MF through the ABI is a deferred upstream ask
  ([decisions](../decisions/README.md)). A 3MF file declares **1 unit = 1 mm**
  ([06 § Units](06-conventions.md#units)).
- **OBJ is not offered** by the terminal: binary formats are preferred for heavy meshes and
  ASCII OBJ bloats; `DualcField.Export` still writes `.obj` for a caller of the Core API.
- **The path holds either the complete file or what was there before.** Every DualC export
  writes `path.part` and renames on success, so a cancelled, failed or killed write leaves
  the destination untouched; the planner's "parent directory must exist" rule is what keeps
  that rename possible ([01 § Cancel and progress](01-native-interop.md#cancel-and-progress--the-050-twins)).
- **Warnings are advisories, errors are refusals.** The planner's three warnings (extension
  override, 3MF cannot tile, tile depth ≥ depth) are posted and the write proceeds; the three
  errors (empty path, unknown selector value, missing directory) stop it before any native
  call. The engine's one advisory — an empty contour, after the write — is the component's,
  not the planner's ([04 § Write to File](04-grasshopper-plugin.md#write-to-file--the-threading-model)).

---

← Back to the [design index](README.md) · the [docs index](../README.md)
