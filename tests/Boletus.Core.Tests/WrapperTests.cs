using System;
using System.IO;
using Boletus.Core;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Boundary / marshaling gate for the Boletus.Core P/Invoke wrapper. Mirrors
    /// capi/dualc_c_demo.c and the handoff §7 cases. The golden vertex/triangle
    /// counts (one home: docs/design/07-invariants-and-limits.md; the record is DualC's
    /// docs/roadmap/14-c-abi/03-implementation-and-verification.md §8) make a single equality check validate struct
    /// layout, string marshaling, and array extraction at once — a silent
    /// struct-padding bug would shift these counts rather than fail to compile.
    /// All contours are kept at the coarse maxDepth=6 the demo uses (cheap; the
    /// golden counts assume exactly that).
    /// </summary>
    public class WrapperTests
    {
        // The analytic/TPMS/boolean demo graph.
        private const string AnalyticExpr =
            "intersection(onion(gyroid(wavelength=0.5),thickness=0.12)," +
            "box(min=[-1,-1,-1],max=[1,1,1]))";

        private static DualcContourParams ProxyParams()
        {
            var p = DualcContourParams.Default();
            p.MaxDepth = 6; // coarse proxy resolution (matches the demo + golden counts)
            return p;
        }

        [Fact]
        public void Version_is_non_empty()
        {
            string v = DualcField.Version();
            Assert.False(string.IsNullOrWhiteSpace(v));
            Assert.StartsWith("dualc", v);
        }

        [Fact]
        public void DefaultParams_match_documented_library_defaults()
        {
            var p = DualcContourParams.Default();
            Assert.Equal(7, p.MaxDepth);
            Assert.Equal(3, p.MinDepth);
            Assert.Equal(0.0, p.Collapse);
            Assert.False(p.HasBounds);
            Assert.True(p.Manifold);
            Assert.Equal(0u, p.NumThreads);
        }

        [Fact]
        public void Contour_analytic_graph_hits_the_golden_counts()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            var mesh = field.Contour(ProxyParams());

            // Verified golden counts (docs/design/07-invariants-and-limits.md) — the marshaling gate.
            Assert.Equal(101476, mesh.VertexCount);
            Assert.Equal(163740, mesh.TriangleCount);

            // Internal array consistency.
            Assert.Equal(mesh.VertexCount * 3, mesh.Positions.Length);
            Assert.Equal(mesh.VertexCount * 3, mesh.Normals.Length);
            Assert.Equal(mesh.TriangleCount * 3, mesh.Indices.Length);
        }

        [Fact]
        public void Contour_can_run_repeatedly_on_one_field()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            var a = field.Contour(ProxyParams());
            var b = field.Contour(ProxyParams());
            Assert.Equal(a.VertexCount, b.VertexCount);
            Assert.Equal(a.TriangleCount, b.TriangleCount);
        }

        [Fact]
        public void Export_writes_a_nontrivial_binary_stl()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            string path = Path.Combine(Path.GetTempPath(), $"boletus_{Guid.NewGuid():N}.stl");
            try
            {
                field.Export(path, ProxyParams());
                Assert.True(File.Exists(path));
                // Binary STL = 80-byte header + 4-byte facet count = 84 minimum.
                Assert.True(new FileInfo(path).Length > 84);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void ExportTiledStl_writes_a_binary_stl_with_the_golden_facet_count()
        {
            // Streaming/tiled STL path. The box is integer-aligned ([-1,1]) and depth 6
            // is a power of 2 — a dyadic grid — so the tiled output is bit-identical to
            // the monolithic mesh: exactly the golden triangle count, not just "> 84".
            // tileDepth 4 = depth - 2 (the documented practical rule), and < depth.
            using var field = DualcField.FromExpr(AnalyticExpr);
            string path = Path.Combine(Path.GetTempPath(), $"boletus_{Guid.NewGuid():N}.stl");
            try
            {
                field.ExportTiledStl(path, ProxyParams(), tileDepth: 4);
                Assert.True(File.Exists(path));

                // Binary STL: 80-byte header, then a uint32 little-endian facet count.
                byte[] bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 84);
                uint facetCount = BitConverter.ToUInt32(bytes, 80);
                Assert.Equal(163740u, facetCount);
                // Sanity: file size = 84-byte header + 50 bytes per facet record.
                Assert.Equal(84 + 50L * facetCount, bytes.Length);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void FromExpr_with_positional_thickness_throws_graph_error_with_locator()
        {
            // Positional `thickness` is rejected by the parser — must be thickness=0.1.
            var ex = Assert.Throws<DualcException>(() => DualcField.FromExpr("onion(gyroid(),0.1)"));
            Assert.Equal(DualcStatus.Graph, ex.Code);
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        [Fact]
        public void Contour_unbounded_field_without_bounds_throws_bounds_error()
        {
            // A bare TPMS is infinite — contouring without bounds is DUALC_ERR_BOUNDS.
            using var field = DualcField.FromExpr("gyroid(wavelength=0.5)");
            var ex = Assert.Throws<DualcException>(() => field.Contour(ProxyParams()));
            Assert.Equal(DualcStatus.Bounds, ex.Code);
        }

        [Fact]
        public void Contour_through_a_mesh_path_source_hits_the_golden_counts()
        {
            // The headline file-path resolver path: clip a gyroid shell to cube.obj.
            // cube.obj is copied beside the test assembly; reference it by absolute
            // path (forward slashes survive the shorthand parser).
            string cube = Path.Combine(AppContext.BaseDirectory, "cube.obj");
            Assert.True(File.Exists(cube), $"cube.obj not found at {cube}");
            string expr =
                "intersection(onion(gyroid(wavelength=0.5),thickness=0.1)," +
                $"mesh(path=\"{cube.Replace('\\', '/')}\"))";

            using var field = DualcField.FromExpr(expr);
            var mesh = field.Contour(ProxyParams());

            Assert.Equal(70032, mesh.VertexCount);
            Assert.Equal(120612, mesh.TriangleCount);
        }
    }
}
