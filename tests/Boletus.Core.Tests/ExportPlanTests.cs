using System;
using System.IO;
using System.Linq;
using Boletus.Core.Export;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Covers the pure, native-free export planning (<see cref="ExportPlan.Resolve"/>): extension
    /// forced from the format selector, the STL/3MF × Tiled/Monolithic strategy table, tile-depth
    /// defaulting, and the empty-path / missing-directory guards. Runs on the plain test runner —
    /// no DualC, no Rhino.
    /// </summary>
    public class ExportPlanTests
    {
        // A directory guaranteed to exist so the parent-dir guard passes; combine with a file name.
        private static string InTemp(string name) => Path.Combine(Path.GetTempPath(), name);

        [Fact]
        public void Bare_name_gains_the_stl_extension()
        {
            var plan = ExportPlan.Resolve(InTemp("part"), format: 0, mode: 0, depth: 6, tileDepth: null);
            Assert.EndsWith(".stl", plan.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(plan.Warnings);
        }

        [Fact]
        public void Bare_name_gains_the_3mf_extension()
        {
            var plan = ExportPlan.Resolve(InTemp("part"), format: 1, mode: 1, depth: 6, tileDepth: null);
            Assert.EndsWith(".3mf", plan.Path, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Format_overrides_a_conflicting_typed_extension_with_a_warning()
        {
            var plan = ExportPlan.Resolve(InTemp("part.3mf"), format: 0, mode: 0, depth: 6, tileDepth: null);
            Assert.EndsWith(".stl", plan.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(plan.Warnings, w => w.Contains(".3mf") && w.Contains(".stl"));
        }

        [Fact]
        public void Matching_typed_extension_is_kept_without_a_warning()
        {
            var plan = ExportPlan.Resolve(InTemp("part.STL"), format: 0, mode: 0, depth: 6, tileDepth: null);
            Assert.EndsWith(".stl", plan.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(plan.Warnings);
        }

        [Fact]
        public void Stl_tiled_resolves_to_tiled_strategy()
        {
            var plan = ExportPlan.Resolve(InTemp("part"), format: 0, mode: 0, depth: 6, tileDepth: null);
            Assert.Equal(ExportStrategy.TiledStl, plan.Strategy);
        }

        [Fact]
        public void Stl_monolithic_resolves_to_monolithic_strategy()
        {
            var plan = ExportPlan.Resolve(InTemp("part"), format: 0, mode: 1, depth: 6, tileDepth: null);
            Assert.Equal(ExportStrategy.Monolithic, plan.Strategy);
        }

        [Fact]
        public void ThreeMf_tiled_falls_back_to_monolithic_with_a_warning()
        {
            var plan = ExportPlan.Resolve(InTemp("part"), format: 1, mode: 0, depth: 6, tileDepth: null);
            Assert.Equal(ExportStrategy.Monolithic, plan.Strategy);
            Assert.Contains(plan.Warnings, w => w.Contains("3MF"));
        }

        [Fact]
        public void Tile_depth_defaults_to_depth_minus_two()
        {
            var plan = ExportPlan.Resolve(InTemp("part"), format: 0, mode: 0, depth: 6, tileDepth: null);
            Assert.Equal(4, plan.TileDepth);
        }

        [Fact]
        public void Explicit_tile_depth_at_or_above_depth_warns()
        {
            var plan = ExportPlan.Resolve(InTemp("part"), format: 0, mode: 0, depth: 6, tileDepth: 6);
            Assert.Equal(6, plan.TileDepth);
            Assert.Contains(plan.Warnings, w => w.Contains("Tile depth"));
        }

        [Fact]
        public void Empty_path_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                ExportPlan.Resolve("   ", format: 0, mode: 0, depth: 6, tileDepth: null));
        }

        [Fact]
        public void Missing_directory_throws()
        {
            string missing = Path.Combine(Path.GetTempPath(), "boletus-no-such-dir-" + Guid.NewGuid().ToString("N"), "part");
            Assert.Throws<ArgumentException>(() =>
                ExportPlan.Resolve(missing, format: 0, mode: 0, depth: 6, tileDepth: null));
        }

        [Fact]
        public void Unknown_format_throws()
        {
            Assert.Throws<ArgumentException>(() =>
                ExportPlan.Resolve(InTemp("part"), format: 9, mode: 0, depth: 6, tileDepth: null));
        }
    }
}
