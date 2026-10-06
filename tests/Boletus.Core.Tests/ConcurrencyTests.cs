using System;
using System.IO;
using System.Threading.Tasks;
using Boletus.Core;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Independent handles on independent threads, at the same time, in one process — what
    /// the plugin does when <c>Proxy preview</c> contours on the UI thread while
    /// <c>Write to File</c> exports on its worker (design 01 § The single-threaded rule
    /// allows exactly this: one handle per thread, never one handle across threads), and
    /// what xunit does when it runs the export-writing test classes in parallel. Every
    /// export must still produce the golden mesh; a count that varies under concurrency is
    /// an engine-side race, not a marshaling fault (roadmap 10 #34, the flaky Windows gate).
    /// </summary>
    public class ConcurrencyTests
    {
        private const string AnalyticExpr =
            "intersection(onion(gyroid(wavelength=0.5),thickness=0.12)," +
            "box(min=[-1,-1,-1],max=[1,1,1]))";

        private const uint GoldenFacets = 163740u;

        private static DualcContourParams ProxyParams()
        {
            var p = DualcContourParams.Default();
            p.MaxDepth = 6;
            return p;
        }

        private static uint FacetCountOf(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 84, "binary STL header missing");
            uint n = BitConverter.ToUInt32(bytes, 80);
            Assert.Equal(84 + 50L * n, bytes.Length);
            return n;
        }

        [Fact]
        public void Parallel_exports_in_one_process_each_keep_the_golden_facet_count()
        {
            const int N = 6; // three tiled, three monolithic, all at once
            var facets = new uint[N];
            var errors = new Exception?[N];

            Parallel.For(0, N, i =>
            {
                string path = Path.Combine(Path.GetTempPath(), $"boletus_par_{Guid.NewGuid():N}.stl");
                try
                {
                    using var field = DualcField.FromExpr(AnalyticExpr);
                    if (i % 2 == 0) field.ExportTiledStl(path, ProxyParams(), tileDepth: 4);
                    else field.Export(path, ProxyParams());
                    facets[i] = FacetCountOf(path);
                }
                catch (Exception e) { errors[i] = e; }
                finally { if (File.Exists(path)) File.Delete(path); }
            });

            Assert.All(errors, e => Assert.Null(e));
            Assert.All(facets, n => Assert.Equal(GoldenFacets, n));
        }

        [Fact]
        public void Parallel_contours_in_one_process_each_keep_the_golden_counts()
        {
            const int N = 4;
            var counts = new (int v, int t)[N];
            var errors = new Exception?[N];

            Parallel.For(0, N, i =>
            {
                try
                {
                    using var field = DualcField.FromExpr(AnalyticExpr);
                    var mesh = field.Contour(ProxyParams());
                    counts[i] = (mesh.VertexCount, mesh.TriangleCount);
                }
                catch (Exception e) { errors[i] = e; }
            });

            Assert.All(errors, e => Assert.Null(e));
            Assert.All(counts, c => { Assert.Equal(101476, c.v); Assert.Equal(163740, c.t); });
        }
    }
}
