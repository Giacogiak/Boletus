using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Boletus.Core;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// The ABI 0.5.0 surface: the cancel token and the <c>*_with_progress</c> twins behind the
    /// <see cref="IProgress{T}"/> / <see cref="CancellationToken"/> overloads of <see cref="DualcField"/>.
    /// Mirrors DualC's own <c>cli_c_abi_cancel</c> cases (capi/dualc_c_demo.c) from the managed side:
    /// a pre-cancelled token cancels before any work, a cancel mid-way leaves no file and no
    /// <c>.part</c>, the hooked calls hit the same golden counts as the plain ones, and every stage
    /// ends at <c>done == total</c>. The golden counts' one home is docs/design/07.
    /// </summary>
    public class ProgressAndCancelTests
    {
        private const string AnalyticExpr =
            "intersection(onion(gyroid(wavelength=0.5),thickness=0.12)," +
            "box(min=[-1,-1,-1],max=[1,1,1]))";

        private static DualcContourParams ProxyParams(int depth = 6)
        {
            var p = DualcContourParams.Default();
            p.MaxDepth = depth;
            return p;
        }

        private sealed class Recorder : IProgress<DualcProgress>
        {
            public readonly List<DualcProgress> Reports = new List<DualcProgress>();
            public readonly List<int> ThreadIds = new List<int>();
            private readonly Action<DualcProgress>? _onReport;
            public Recorder(Action<DualcProgress>? onReport = null) => _onReport = onReport;
            public void Report(DualcProgress value)
            {
                lock (Reports)
                {
                    Reports.Add(value);
                    ThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
                }
                _onReport?.Invoke(value);
            }
        }

        private static string TempPath(string ext) =>
            Path.Combine(Path.GetTempPath(), $"boletus_{Guid.NewGuid():N}{ext}");

        private static void AssertStageShape(IEnumerable<DualcProgress> all, DualcStage stage)
        {
            var reports = all.Where(r => r.Stage == stage).ToList();
            Assert.NotEmpty(reports);
            uint total = reports[0].Total;
            Assert.True(total > 0, $"{stage}: total must be positive");
            Assert.All(reports, r => Assert.Equal(total, r.Total));           // total constant
            for (int i = 1; i < reports.Count; i++)                             // done monotonic
                Assert.True(reports[i].Done >= reports[i - 1].Done, $"{stage}: done went backwards");
            Assert.Equal(total, reports[reports.Count - 1].Done);               // last is (total, total)
        }

        [Fact]
        public void The_vendored_library_supports_progress_and_is_at_least_0_5_0()
        {
            Assert.True(DualcField.SupportsProgress);

            // "dualc X.Y.Z" — the ABI level the overloads need is 0.5.0 or later. A floor, not a
            // restated pin: the exact version and commit live in native/README.md.
            string v = DualcField.Version();
            string[] parts = v.Split(' ')[1].Split('.');
            var version = new Version(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
            Assert.True(version >= new Version(0, 5, 0), $"version {v} predates ABI 0.5.0");
        }

        [Fact]
        public void Contour_with_progress_hits_the_golden_counts_and_reports_sample_then_contour()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            var rec = new Recorder();
            int callerThread = Thread.CurrentThread.ManagedThreadId;

            var mesh = field.Contour(ProxyParams(), rec, CancellationToken.None);

            Assert.Equal(101476, mesh.VertexCount);
            Assert.Equal(163740, mesh.TriangleCount);

            Assert.NotEmpty(rec.Reports);
            Assert.All(rec.ThreadIds, id => Assert.Equal(callerThread, id));   // calling thread only
            Assert.Equal(DualcStage.Sample, rec.Reports[0].Stage);
            int firstContour = rec.Reports.FindIndex(r => r.Stage == DualcStage.Contour);
            Assert.True(firstContour > 0);
            Assert.All(rec.Reports.Take(firstContour), r => Assert.Equal(DualcStage.Sample, r.Stage));
            Assert.DoesNotContain(rec.Reports, r => r.Stage == DualcStage.Write || r.Stage == DualcStage.Tile);
            AssertStageShape(rec.Reports, DualcStage.Sample);
            AssertStageShape(rec.Reports, DualcStage.Contour);
        }

        [Fact]
        public void ExportTiledStl_with_progress_reports_tiles_only_and_matches_the_golden_facet_count()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            string path = TempPath(".stl");
            var rec = new Recorder();
            try
            {
                field.ExportTiledStl(path, ProxyParams(), tileDepth: 4, rec, CancellationToken.None);

                byte[] bytes = File.ReadAllBytes(path);
                Assert.Equal(163740u, BitConverter.ToUInt32(bytes, 80));
                Assert.False(File.Exists(path + ".part"));

                Assert.All(rec.Reports, r => Assert.Equal(DualcStage.Tile, r.Stage));
                Assert.Equal(0u, rec.Reports[0].Done);                          // (0, T) before the loop
                AssertStageShape(rec.Reports, DualcStage.Tile);                 // … (T, T) after the rename
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Export_with_progress_reports_write_as_a_final_stage_and_writes_the_file()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            string path = TempPath(".stl");
            var rec = new Recorder();
            try
            {
                field.Export(path, ProxyParams(), rec, CancellationToken.None);

                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 84);
                Assert.False(File.Exists(path + ".part"));

                AssertStageShape(rec.Reports, DualcStage.Sample);
                AssertStageShape(rec.Reports, DualcStage.Contour);
                var write = rec.Reports.Where(r => r.Stage == DualcStage.Write).ToList();
                Assert.Equal(2, write.Count);                                   // (0,1) then (1,1)
                Assert.Equal((0u, 1u), (write[0].Done, write[0].Total));
                Assert.Equal((1u, 1u), (write[1].Done, write[1].Total));
                Assert.Equal(DualcStage.Write, rec.Reports[rec.Reports.Count - 1].Stage);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void A_pre_cancelled_token_cancels_before_any_work_and_writes_nothing()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            string path = TempPath(".stl");
            var rec = new Recorder();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = Assert.Throws<OperationCanceledException>(
                () => field.ExportTiledStl(path, ProxyParams(), 4, rec, cts.Token));

            Assert.Equal(cts.Token, ex.CancellationToken);
            Assert.Contains("cancelled", ex.Message);
            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".part"));
            Assert.DoesNotContain(rec.Reports, r => r.Done > 0);               // no tile was finished
        }

        [Fact]
        public void A_cancel_from_inside_the_progress_callback_leaves_no_file_and_no_part()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            string path = TempPath(".stl");
            using var cts = new CancellationTokenSource();
            // The callback may itself request the token: cancel as the second tile starts.
            var rec = new Recorder(p => { if (p.Stage == DualcStage.Tile && p.Done == 1) cts.Cancel(); });

            Assert.Throws<OperationCanceledException>(
                () => field.ExportTiledStl(path, ProxyParams(), 4, rec, cts.Token));

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".part"));
            uint total = rec.Reports[0].Total;
            Assert.True(rec.Reports.Max(r => r.Done) < total, "the export ran to completion despite the cancel");
        }

        [Fact]
        public void A_cancel_from_another_thread_returns_promptly_and_leaves_no_file()
        {
            // The Write to File shape: the call blocks on a worker, the UI thread cancels. Depth 8 of
            // the analytic graph is seconds of work; the engine promises to notice within well under
            // a second, so the bound here is generous and the proof is "far less than the full run".
            string path = TempPath(".stl");
            using var cts = new CancellationTokenSource();
            var started = new ManualResetEventSlim();
            var rec = new Recorder(_ => started.Set());
            var cancelledAt = new Stopwatch();

            Task worker = Task.Run(() =>
            {
                using var field = DualcField.FromExpr(AnalyticExpr);
                field.Export(path, ProxyParams(depth: 8), rec, cts.Token);
            });

            Assert.True(started.Wait(TimeSpan.FromSeconds(30)), "the export never reported progress");
            Thread.Sleep(150);
            cancelledAt.Start();
            cts.Cancel();

            var agg = Assert.Throws<AggregateException>(() => worker.Wait(TimeSpan.FromSeconds(60)));
            cancelledAt.Stop();
            Assert.IsType<OperationCanceledException>(agg.InnerException);
            Assert.True(cancelledAt.Elapsed < TimeSpan.FromSeconds(10),
                $"the cancel took {cancelledAt.Elapsed.TotalSeconds:0.0}s to be honoured");
            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".part"));
        }

        [Fact]
        public void A_cancelled_export_leaves_a_file_already_at_the_path_untouched()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            string path = TempPath(".stl");
            try
            {
                File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                Assert.Throws<OperationCanceledException>(
                    () => field.Export(path, ProxyParams(), null, cts.Token));

                Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(path));
                Assert.False(File.Exists(path + ".part"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void A_throwing_progress_consumer_never_reaches_native_code()
        {
            using var field = DualcField.FromExpr(AnalyticExpr);
            var rec = new Recorder(_ => throw new InvalidOperationException("consumer bug"));

            var mesh = field.Contour(ProxyParams(), rec, CancellationToken.None);   // must not crash

            Assert.Equal(101476, mesh.VertexCount);
            Assert.NotEmpty(rec.Reports);
        }
    }
}
