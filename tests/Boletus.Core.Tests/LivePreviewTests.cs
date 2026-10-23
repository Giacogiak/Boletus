using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Boletus.Core.LivePreview;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// The Rhino-free half of <c>Live Preview</c>: the document it writes (the canonical graph
    /// plus the optional <c>view</c> block), the command line of each side-car, and the
    /// locate-and-fall-back rule between DualCEditor and <c>dualc_field_view</c>. No process is
    /// started here; the process and GL path stay a manual Rhino check.
    /// </summary>
    public sealed class LivePreviewTests : IDisposable
    {
        private readonly string _dir =
            Path.Combine(Path.GetTempPath(), "BoletusLivePreviewTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort temp cleanup */ }
        }

        private static FieldNode GyroidInBox() => Field.Intersection(
            Field.Onion(Field.Gyroid(0.5), 0.1),
            Field.Box((-1, -1, -1), (1, 1, 1)));

        private static readonly ViewBounds Unit = new ViewBounds(-1, -1, -1, 1, 1, 1);

        // ---- the document --------------------------------------------------------------

        [Fact]
        public void Compose_without_bounds_is_the_serializer_byte_for_byte()
        {
            var root = GyroidInBox();
            Assert.Equal(FieldGraphSerializer.Serialize(root), LivePreviewDocument.Compose(root, null));
        }

        [Fact]
        public void Compose_with_bounds_adds_one_view_line_and_changes_nothing_else()
        {
            var root = GyroidInBox();
            string plain = FieldGraphSerializer.Serialize(root);
            string withView = LivePreviewDocument.Compose(root, new ViewBounds(-2, -1.5, 0, 2, 1.5, 3.25));

            string[] lines = withView.Split('\n');
            Assert.Equal("{", lines[0]);
            Assert.Equal("  \"view\": { \"bounds\": [ -2.0, -1.5, 0.0, 2.0, 1.5, 3.25 ] },", lines[1]);
            Assert.Equal(1, lines.Count(l => l.Contains("\"view\"")));

            string stripped = string.Join("\n", lines.Where((_, i) => i != 1));
            Assert.Equal(plain, stripped);
        }

        [Fact]
        public void Compose_rejects_bounds_that_are_not_a_box()
        {
            Assert.Throws<ArgumentException>(() =>
                LivePreviewDocument.Compose(GyroidInBox(), new ViewBounds(1, -1, -1, -1, 1, 1)));
        }

        [Fact]
        public void The_C_ABI_ignores_the_view_key()
        {
            // The same graph with and without a view block (and a view box that is NOT the
            // field's own) contours to the same mesh: the key never reaches the engine.
            var root = GyroidInBox();
            var p = DualcContourParams.Default();
            p.MaxDepth = 4;

            using var plain = DualcField.FromJson(LivePreviewDocument.Compose(root, null));
            using var viewed = DualcField.FromJson(LivePreviewDocument.Compose(root, new ViewBounds(-7, -7, -7, 7, 7, 7)));
            var a = plain.Contour(p);
            var b = viewed.Contour(p);

            Assert.True(a.TriangleCount > 0);
            Assert.Equal(a.VertexCount, b.VertexCount);
            Assert.Equal(a.TriangleCount, b.TriangleCount);
        }

        [Fact]
        public void Dump_json_round_trip_drops_the_view_key()
        {
            string? cli = TestPaths.FindCli();
            if (cli is null) return; // no native build on this checkout: the suite stays green

            var root = GyroidInBox();
            Directory.CreateDirectory(_dir);
            string plainPath = Path.Combine(_dir, "plain.json");
            string viewPath = Path.Combine(_dir, "view.json");
            File.WriteAllText(plainPath, LivePreviewDocument.Compose(root, null));
            File.WriteAllText(viewPath, LivePreviewDocument.Compose(root, Unit));

            string plain = DumpJson(cli, plainPath);
            string viewed = DumpJson(cli, viewPath);
            Assert.Equal(plain, viewed);
            Assert.DoesNotContain("\"view\"", viewed, StringComparison.Ordinal);
        }

        private static string DumpJson(string cli, string inputPath)
        {
            var psi = new ProcessStartInfo(cli)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add(inputPath);
            psi.ArgumentList.Add("--dump-json");
            using var proc = Process.Start(psi)!;
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            Assert.True(proc.ExitCode == 0, $"dualc_field --dump-json failed (exit {proc.ExitCode}): {stderr}");
            return stdout.Replace("\r\n", "\n").Trim();
        }

        // ---- the bounds ----------------------------------------------------------------

        [Theory]
        [InlineData(0, 0, 0, 1, 1, 1, true)]
        [InlineData(0, 0, 0, 0, 1, 1, false)]       // flat on X
        [InlineData(1, 0, 0, -1, 1, 1, false)]      // inverted on X
        [InlineData(double.NaN, 0, 0, 1, 1, 1, false)]
        [InlineData(0, 0, 0, double.PositiveInfinity, 1, 1, false)]
        public void ViewBounds_is_valid_only_for_a_finite_box(double x0, double y0, double z0, double x1, double y1, double z1, bool valid)
        {
            Assert.Equal(valid, new ViewBounds(x0, y0, z0, x1, y1, z1).IsValid);
        }

        [Fact]
        public void ViewBounds_flag_value_is_invariant_and_comma_separated()
        {
            Assert.Equal("-1.5,0,0.25,1.5,2,3", new ViewBounds(-1.5, 0, 0.25, 1.5, 2, 3).ToFlagValue());
        }

        // ---- the command lines ---------------------------------------------------------

        [Fact]
        public void Field_view_takes_the_graph_and_the_bounds_flag()
        {
            Assert.Equal(new[] { "g.json" }, ViewerArguments.Build(ViewerKind.FieldView, "g.json", null));
            Assert.Equal(new[] { "g.json", "--bounds", "-1,-1,-1,1,1,1" },
                ViewerArguments.Build(ViewerKind.FieldView, "g.json", Unit));
        }

        [Fact]
        public void Editor_takes_no_bounds_flag_and_its_two_switches_only_when_asked()
        {
            Assert.Equal(new[] { "g.json" }, ViewerArguments.Build(ViewerKind.Editor, "g.json", Unit));
            Assert.Equal(new[] { "g.json", "--explore", "--title", "Live Preview" },
                ViewerArguments.Build(ViewerKind.Editor, "g.json", Unit, title: "Live Preview", explore: true));
            Assert.Equal(new[] { "g.json", "--explore" },
                ViewerArguments.Build(ViewerKind.Editor, "g.json", null, title: "  ", explore: true));
        }

        [Fact]
        public void Field_view_rejects_bounds_that_are_not_a_box()
        {
            Assert.Throws<ArgumentException>(() =>
                ViewerArguments.Build(ViewerKind.FieldView, "g.json", new ViewBounds(0, 0, 0, 0, 1, 1)));
        }

        // ---- locate and fall back ------------------------------------------------------

        [Fact]
        public void Locate_puts_the_field_view_beside_the_gha_and_the_editor_under_editor()
        {
            var paths = ViewerLocator.Locate(_dir);
            Assert.Equal(Path.Combine(_dir, "dualc_field_view.exe"), paths.FieldViewExe);
            Assert.Equal(Path.Combine(_dir, "editor", "DualCEditor.exe"), paths.EditorExe);
            Assert.False(paths.FieldViewExists);
            Assert.False(paths.EditorExists);
        }

        [Fact]
        public void Locate_honours_the_editor_override()
        {
            string elsewhere = Path.Combine(_dir, "dev", "DualCEditor.exe");
            Assert.Equal(elsewhere, ViewerLocator.Locate(_dir, elsewhere).EditorExe);
            Assert.Equal(Path.Combine(_dir, "editor", "DualCEditor.exe"), ViewerLocator.Locate(_dir, "  ").EditorExe);
        }

        [Fact]
        public void Resolve_applies_the_one_fallback_rule()
        {
            Directory.CreateDirectory(Path.Combine(_dir, "editor"));
            var paths = ViewerLocator.Locate(_dir);

            // Nothing built: nothing to launch, whichever is asked for.
            Assert.Null(ViewerLocator.Resolve(ViewerKind.FieldView, paths));
            Assert.Null(ViewerLocator.Resolve(ViewerKind.Editor, paths));

            // Only the field view: the editor request falls back to it, flagged.
            File.WriteAllText(paths.FieldViewExe, "");
            var fv = ViewerLocator.Resolve(ViewerKind.FieldView, paths)!.Value;
            Assert.Equal(ViewerKind.FieldView, fv.Kind);
            Assert.False(fv.FellBack);
            var fell = ViewerLocator.Resolve(ViewerKind.Editor, paths)!.Value;
            Assert.Equal(ViewerKind.FieldView, fell.Kind);
            Assert.Equal(paths.FieldViewExe, fell.Exe);
            Assert.True(fell.FellBack);

            // Both: each request gets its own.
            File.WriteAllText(paths.EditorExe, "");
            var ed = ViewerLocator.Resolve(ViewerKind.Editor, paths)!.Value;
            Assert.Equal(ViewerKind.Editor, ed.Kind);
            Assert.Equal(paths.EditorExe, ed.Exe);
            Assert.False(ed.FellBack);
            Assert.Equal(ViewerKind.FieldView, ViewerLocator.Resolve(ViewerKind.FieldView, paths)!.Value.Kind);
        }
    }
}
