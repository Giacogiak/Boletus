using System;
using System.IO;
using System.Threading;
using Boletus.Core;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// The ABI 0.4.0 surface: <see cref="DualcDiagnostics"/> through the <c>out</c> overloads of
    /// <see cref="DualcField"/>. Mirrors DualC's own <c>test_diagnostics.cpp</c> empty-contour case
    /// from the managed side — a sphere sampled in a box far away from it — and pins that the 0.5.0
    /// progress twins fill the same struct as the 0.4.0 twins. The golden counts' one home is
    /// docs/design/07.
    /// </summary>
    public class DiagnosticsTests
    {
        private const string AnalyticExpr =
            "intersection(onion(gyroid(wavelength=0.5),thickness=0.12)," +
            "box(min=[-1,-1,-1],max=[1,1,1]))";

        /// <summary>A small sphere at the origin; sampled elsewhere it has no surface.</summary>
        private const string FarawaySphereExpr = "sphere(radius=0.4)";

        private static DualcContourParams ProxyParams(int depth = 6)
        {
            var p = DualcContourParams.Default();
            p.MaxDepth = depth;
            return p;
        }

        /// <summary>DualC tests/test_diagnostics.cpp: bounds (10,10,10)-(11,11,11), depth 5.</summary>
        private static DualcContourParams ElsewhereParams()
        {
            var p = DualcContourParams.Default();
            p.MaxDepth = 5;
            p.HasBounds = true;
            p.BoundsMin = (10, 10, 10);
            p.BoundsMax = (11, 11, 11);
            return p;
        }

        private static string TempPath(string ext) =>
            Path.Combine(Path.GetTempPath(), $"boletus_{Guid.NewGuid():N}{ext}");

        private static void AssertSameDiagnostics(DualcDiagnostics a, DualcDiagnostics b)
        {
            Assert.Equal(a.InputEmpty, b.InputEmpty);
            Assert.Equal(a.InputBoundaryEdges, b.InputBoundaryEdges);
            Assert.Equal(a.InputNonManifoldEdges, b.InputNonManifoldEdges);
            Assert.Equal(a.InputWatertight, b.InputWatertight);
            Assert.Equal(a.BoundsFallback, b.BoundsFallback);
            Assert.Equal(a.GridBoundsExceeded, b.GridBoundsExceeded);
            Assert.Equal(a.EmptyContour, b.EmptyContour);
            Assert.Equal(a.OutputVertices, b.OutputVertices);
            Assert.Equal(a.OutputTriangles, b.OutputTriangles);
            Assert.Equal(a.OutputBoundaryEdges, b.OutputBoundaryEdges);
            Assert.Equal(a.OutputNonManifoldEdges, b.OutputNonManifoldEdges);
            Assert.Equal(a.OutputWatertight, b.OutputWatertight);
            Assert.Equal(a.AnyIssue, b.AnyIssue);
        }

        /// <summary>Binary STL: 80-byte header, uint32 facet count, 50 bytes per facet.</summary>
        private static uint StlFacetCount(byte[] stl) => BitConverter.ToUInt32(stl, 80);

        [Fact]
        public void The_vendored_library_supports_diagnostics()
        {
            Assert.True(DualcField.SupportsDiagnostics);
        }

        [Fact]
        public void An_empty_contour_is_reported_as_a_placeholder_not_a_surface()
        {
            using var field = DualcField.FromExpr(FarawaySphereExpr);
            var mesh = field.Contour(ElsewhereParams(), out var d);

            // The engine's contract: OK + one placeholder triangle, flagged.
            Assert.True(d.EmptyContour);
            Assert.True(d.AnyIssue);
            Assert.Equal(3UL, d.OutputVertices);
            Assert.Equal(1UL, d.OutputTriangles);
            Assert.Equal(3UL, d.OutputBoundaryEdges);
            Assert.False(d.OutputWatertight);

            // The mesh itself is a legitimate one-triangle mesh — a count check alone could never tell.
            Assert.Equal(3, mesh.VertexCount);
            Assert.Equal(1, mesh.TriangleCount);
        }

        [Fact]
        public void Contour_with_diagnostics_hits_the_golden_counts_and_reports_a_closed_mesh()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            var mesh = field.Contour(ProxyParams(), out var d);

            // Golden counts (docs/design/07) — through the diagnostics twin, and in the struct.
            // outputVertices sits at byte 40, past the int/pad region: a layout error shifts it.
            Assert.Equal(101476, mesh.VertexCount);
            Assert.Equal(163740, mesh.TriangleCount);
            Assert.Equal((ulong)mesh.VertexCount, d.OutputVertices);
            Assert.Equal((ulong)mesh.TriangleCount, d.OutputTriangles);

            Assert.False(d.EmptyContour);
            Assert.False(d.AnyIssue);
            Assert.True(d.OutputWatertight);
            Assert.Equal(0UL, d.OutputBoundaryEdges);
            Assert.Equal(0UL, d.OutputNonManifoldEdges);
            Assert.False(d.BoundsFallback);
            Assert.False(d.GridBoundsExceeded);

            // The field entry points never inspect an input mesh: the engine's defaults.
            Assert.False(d.InputEmpty);
            Assert.Equal(0UL, d.InputBoundaryEdges);
            Assert.Equal(0UL, d.InputNonManifoldEdges);
            Assert.True(d.InputWatertight);
        }

        [Fact]
        public void The_progress_overload_returns_the_same_diagnostics_as_the_plain_twin()
        {
            using (var field = DualcField.FromExpr(AnalyticExpr))
            {
                field.Contour(ProxyParams(), out var plain);
                field.Contour(ProxyParams(), null, CancellationToken.None, out var hooked);
                AssertSameDiagnostics(plain, hooked);
            }
            using (var field = DualcField.FromExpr(FarawaySphereExpr))
            {
                field.Contour(ElsewhereParams(), out var plain);
                field.Contour(ElsewhereParams(), null, CancellationToken.None, out var hooked);
                AssertSameDiagnostics(plain, hooked);
                Assert.True(hooked.EmptyContour);
            }
        }

        [Fact]
        public void Export_with_diagnostics_describes_the_written_stl()
        {
            string path = TempPath(".stl");
            try
            {
                using var field = DualcField.FromExpr(AnalyticExpr);
                field.Export(path, ProxyParams(), out var d);

                byte[] stl = File.ReadAllBytes(path);
                Assert.Equal(163740UL, d.OutputTriangles);
                Assert.Equal(d.OutputTriangles, (ulong)StlFacetCount(stl));
                Assert.Equal(84 + 50L * 163740, stl.LongLength);
                Assert.True(d.OutputWatertight);
                Assert.False(d.EmptyContour);
                Assert.False(d.AnyIssue);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Export_of_an_empty_contour_writes_the_placeholder_and_says_so()
        {
            string path = TempPath(".stl");
            try
            {
                using var field = DualcField.FromExpr(FarawaySphereExpr);
                field.Export(path, ElsewhereParams(), out var d);

                Assert.True(File.Exists(path));
                byte[] stl = File.ReadAllBytes(path);
                Assert.Equal(84 + 50, stl.Length);           // header + count + one facet
                Assert.Equal(1u, StlFacetCount(stl));
                Assert.Equal(1UL, d.OutputTriangles);
                Assert.True(d.EmptyContour);
                Assert.True(d.AnyIssue);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Export_progress_overload_returns_the_same_diagnostics_as_the_plain_twin()
        {
            string plainPath = TempPath(".stl");
            string hookedPath = TempPath(".stl");
            try
            {
                using var field = DualcField.FromExpr(AnalyticExpr);
                field.Export(plainPath, ProxyParams(), out var plain);
                field.Export(hookedPath, ProxyParams(), null, CancellationToken.None, out var hooked);

                AssertSameDiagnostics(plain, hooked);
                Assert.Equal(new FileInfo(plainPath).Length, new FileInfo(hookedPath).Length);
            }
            finally
            {
                if (File.Exists(plainPath)) File.Delete(plainPath);
                if (File.Exists(hookedPath)) File.Delete(hookedPath);
            }
        }
    }
}
