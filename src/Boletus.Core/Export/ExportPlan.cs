using System;
using System.Collections.Generic;
using System.IO;

namespace Boletus.Core.Export
{
    /// <summary>Which native writer the resolved target uses.</summary>
    public enum ExportStrategy
    {
        /// <summary>Streaming, bounded-RAM binary STL via <see cref="DualcField.ExportTiledStl"/>.</summary>
        TiledStl,

        /// <summary>Whole-mesh-in-RAM write via <see cref="DualcField.Export"/> (any format).</summary>
        Monolithic,
    }

    /// <summary>The deterministic, Rhino-free result of planning a file export: the resolved path
    /// (extension forced from the chosen format), the native writer to call, the effective tile
    /// depth, and any advisory warnings. Hard problems (empty path, missing directory) throw so the
    /// component can surface an error and refuse to write.</summary>
    public sealed class ExportPlanResult
    {
        internal ExportPlanResult(string path, ExportStrategy strategy, int tileDepth, IReadOnlyList<string> warnings)
        {
            Path = path;
            Strategy = strategy;
            TileDepth = tileDepth;
            Warnings = warnings;
        }

        /// <summary>Absolute-or-relative path with the extension set from the format selector.</summary>
        public string Path { get; }

        /// <summary>The native writer to invoke.</summary>
        public ExportStrategy Strategy { get; }

        /// <summary>Sub-grid depth for <see cref="ExportStrategy.TiledStl"/> (ignored otherwise).</summary>
        public int TileDepth { get; }

        /// <summary>Non-fatal advisories (extension override, 3MF-can't-tile, tile≥depth).</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// Plans a file export from the raw component inputs. Pure and native-free so the extension /
    /// format / tiling decisions are unit-tested on a plain runner (the same "logic in Core, thin
    /// <c>.gha</c>" split as <see cref="VolumeResolver"/>). The component owns only the threading and
    /// the actual native calls.
    /// </summary>
    public static class ExportPlan
    {
        /// <summary>STL (0) or 3MF (1) — the format selector's integer domain.</summary>
        public enum Format { Stl = 0, ThreeMf = 1 }

        /// <summary>Tiled (0, streaming) or Monolithic (1) — the mode selector's integer domain.</summary>
        public enum Mode { Tiled = 0, Monolithic = 1 }

        private const string StlExt = ".stl";
        private const string ThreeMfExt = ".3mf";

        /// <summary>
        /// Resolve raw inputs into an <see cref="ExportPlanResult"/>. The <b>format is the source of
        /// truth for the extension</b>: a bare name gains one, a conflicting typed extension is
        /// replaced (with a warning). 3MF has no tiled writer, so 3MF always resolves to
        /// <see cref="ExportStrategy.Monolithic"/>.
        /// </summary>
        /// <param name="path">User path; may be a bare name, or carry any extension.</param>
        /// <param name="format">0 = STL, 1 = 3MF (out-of-range throws).</param>
        /// <param name="mode">0 = Tiled, 1 = Monolithic (out-of-range throws).</param>
        /// <param name="depth">Contour max depth (for the default tile depth).</param>
        /// <param name="tileDepth">Explicit tile depth, or null to default to <c>depth - 2</c>.</param>
        /// <exception cref="ArgumentException">Empty path, unknown format/mode, or the parent
        /// directory does not exist.</exception>
        public static ExportPlanResult Resolve(string? path, int format, int mode, int depth, int? tileDepth)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A file path is required.", nameof(path));
            if (format != (int)Format.Stl && format != (int)Format.ThreeMf)
                throw new ArgumentException($"Unknown format {format} (expected 0 = STL, 1 = 3MF).", nameof(format));
            if (mode != (int)Mode.Tiled && mode != (int)Mode.Monolithic)
                throw new ArgumentException($"Unknown mode {mode} (expected 0 = Tiled, 1 = Monolithic).", nameof(mode));

            var warnings = new List<string>();

            var fmt = (Format)format;
            string wantExt = fmt == Format.Stl ? StlExt : ThreeMfExt;

            string trimmed = path!.Trim();
            string existingExt = System.IO.Path.GetExtension(trimmed);
            if (!string.IsNullOrEmpty(existingExt) &&
                !existingExt.Equals(wantExt, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"Path extension '{existingExt}' overridden by the Format selector → '{wantExt}'.");
            }
            string resolvedPath = System.IO.Path.ChangeExtension(trimmed, wantExt);

            string? dir = System.IO.Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                throw new ArgumentException($"Directory does not exist: {dir}", nameof(path));

            // 3MF has no streaming writer; STL honors the mode. Tiling a 3MF is a no-op fallback.
            ExportStrategy strategy;
            if (fmt == Format.ThreeMf)
            {
                strategy = ExportStrategy.Monolithic;
                if ((Mode)mode == Mode.Tiled)
                    warnings.Add("3MF has no tiled writer; writing monolithically.");
            }
            else
            {
                strategy = (Mode)mode == Mode.Tiled ? ExportStrategy.TiledStl : ExportStrategy.Monolithic;
            }

            int effectiveTile = tileDepth ?? Math.Max(1, depth - 2);
            if (strategy == ExportStrategy.TiledStl && effectiveTile >= depth)
                warnings.Add($"Tile depth {effectiveTile} ≥ Depth {depth}: tiling falls back to a single pass (no RAM benefit).");

            return new ExportPlanResult(resolvedPath, strategy, effectiveTile, warnings);
        }
    }
}
