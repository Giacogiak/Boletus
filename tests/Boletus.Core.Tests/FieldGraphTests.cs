using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Phase 3a — the field-graph model + canonical-JSON serializer.
    ///
    /// The correctness gate is a round-trip through DualC's own canonicalizer: emit the
    /// managed JSON, run it through <c>dualc_field --dump-json</c>, and compare to the
    /// <c>--dump-json</c> of the equivalent shipped graph. Both pass through DualC's emitter,
    /// so they are byte-identical iff the graphs are semantically equal — which means our
    /// emitter's key-sort and float formatting are not load-bearing for correctness, only
    /// that DualC parses our JSON. CLI-backed tests skip (no assertion) when the CLI is
    /// absent, like <see cref="CliParityTests"/>. The pure-formatter tests always run.
    /// </summary>
    public class FieldGraphTests
    {
        // ---- CLI oracle plumbing (mirrors CliParityTests) ---------------------------------

        private static string? FindCli() => TestPaths.FindCli();

        // UTF-8 without BOM, LF line endings — what the CLI parser expects.
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static string WriteTemp(string json)
        {
            string path = Path.Combine(Path.GetTempPath(), $"boletus_fg_{Guid.NewGuid():N}.json");
            File.WriteAllText(path, json, Utf8NoBom);
            return path;
        }

        /// <summary>Run <c>dualc_field &lt;inputPath&gt; --dump-json</c> and return canonical stdout.</summary>
        private static string DumpJson(string cli, string inputPath)
        {
            var psi = new ProcessStartInfo(cli)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(cli)!, // mesh paths resolve here
            };
            psi.ArgumentList.Add(inputPath);
            psi.ArgumentList.Add("--dump-json");

            using var proc = Process.Start(psi)!;
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            Assert.True(proc.ExitCode == 0, $"dualc_field --dump-json failed (exit {proc.ExitCode}): {stderr}");
            return Normalize(stdout);
        }

        private static string Normalize(string s) => s.Replace("\r\n", "\n").Trim();

        /// <summary>Assert the managed node canonicalizes identically to a reference graph file.</summary>
        private static void AssertRoundTripsTo(string cli, FieldNode managed, string referencePath)
        {
            string ourPath = WriteTemp(managed.ToJson());
            try
            {
                string ours = DumpJson(cli, ourPath);
                string reference = DumpJson(cli, referencePath);
                Assert.Equal(reference, ours);
            }
            finally
            {
                if (File.Exists(ourPath)) File.Delete(ourPath);
            }
        }

        // ---- First milestone: reproduce the two shipped fixtures --------------------------

        // DualC's shipped fixtures: DUALC_SAMPLES_DIR, else the submodule's examples/samples.
        private static readonly string SamplesDir = TestPaths.SamplesDir;

        [Fact]
        public void Gyroid_box_fixture_round_trips()
        {
            string? cli = FindCli();
            if (cli is null) return;

            string fixture = Path.Combine(SamplesDir, "gyroid_box.json");
            if (!File.Exists(fixture)) return;

            // difference(intersection(box, onion(normalize(gyroid))), sphere)
            FieldNode managed = Field.Difference(
                Field.Intersection(
                    Field.Box(min: (-2, -2, -2), max: (2, 2, 2)),
                    Field.Onion(Field.Normalize(Field.Gyroid(wavelength: 1.0)), thickness: 0.3)),
                Field.Sphere(radius: 1.0, center: (0, 0, 0)));

            AssertRoundTripsTo(cli, managed, fixture);
        }

        [Fact]
        public void Mesh_lattice_fixture_round_trips()
        {
            string? cli = FindCli();
            if (cli is null) return;

            string fixture = Path.Combine(SamplesDir, "mesh_lattice.json");
            if (!File.Exists(fixture)) return;

            // intersection(mesh(cube.obj), onion(normalize(gyroid)))
            FieldNode managed = Field.Intersection(
                Field.Mesh("cube.obj"),
                Field.Onion(Field.Normalize(Field.Gyroid(wavelength: 0.5)), thickness: 0.1));

            AssertRoundTripsTo(cli, managed, fixture);
        }

        [Fact]
        public void Emitted_json_byte_matches_dump_json_format()
        {
            // Secondary (not load-bearing) check: our hand-rolled emitter reproduces DualC's
            // nlohmann dump(2) format exactly, so emitted graphs are diffable against fixtures.
            string? cli = FindCli();
            if (cli is null) return;

            string fixture = Path.Combine(SamplesDir, "gyroid_box.json");
            if (!File.Exists(fixture)) return;

            FieldNode managed = Field.Difference(
                Field.Intersection(
                    Field.Box(min: (-2, -2, -2), max: (2, 2, 2)),
                    Field.Onion(Field.Normalize(Field.Gyroid(wavelength: 1.0)), thickness: 0.3)),
                Field.Sphere(radius: 1.0, center: (0, 0, 0)));

            string ours = Normalize(managed.ToJson());
            string reference = DumpJson(cli, fixture);
            Assert.Equal(reference, ours);
        }

        // ---- Acceptance baseline: managed JSON contours to a non-empty mesh ---------------

        [Fact]
        public void Managed_json_contours_to_a_non_empty_mesh()
        {
            // Analytic lattice-clip; keep depth coarse (deep lattice contours OOM the box).
            FieldNode managed = Field.Intersection(
                Field.Onion(Field.Gyroid(wavelength: 0.5), thickness: 0.12),
                Field.Box(min: (-1, -1, -1), max: (1, 1, 1)));

            using var field = DualcField.FromJson(managed.ToJson());
            var p = DualcContourParams.Default();
            p.MaxDepth = 6;
            var mesh = field.Contour(p);

            Assert.True(mesh.VertexCount > 0);
            Assert.True(mesh.TriangleCount > 0);
        }

        // ---- Load-bearing edge case the fixtures miss: backslash mesh path -----------------

        [Fact]
        public void Backslash_mesh_path_is_escaped_and_parses()
        {
            // A Windows temp path (the real Rhino mesh-clip case) carries backslashes that are
            // invalid JSON unless escaped as \\.
            FieldNode managed = Field.Mesh(@"D:\tmp\clip volume.obj");
            string json = managed.ToJson();
            Assert.Contains(@"D:\\tmp\\clip volume.obj", json); // escaped in our output

            string? cli = FindCli();
            if (cli is null) return;

            string path = WriteTemp(json);
            try
            {
                string dumped = DumpJson(cli, path); // exit 0 ⇒ DualC accepted our escaping
                Assert.Contains("mesh", dumped);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ---- Pure-formatter tests (no CLI needed) -----------------------------------------

        [Fact]
        public void Node_keys_emit_op_first_then_ordinal_sorted_params_then_in_last()
        {
            // box has params min,max → emitted max before min (ordinal); plus an "in" child to
            // confirm params precede "in".
            var box = Field.Box(min: (-1, -1, -1), max: (1, 1, 1));
            var node = Field.Onion(box, thickness: 0.3);
            string json = node.ToJson();

            int op = json.IndexOf("\"op\"", StringComparison.Ordinal);
            int thickness = json.IndexOf("\"thickness\"", StringComparison.Ordinal);
            int inKey = json.IndexOf("\"in\"", StringComparison.Ordinal);
            Assert.True(op >= 0 && thickness > op && inKey > thickness,
                "Expected op < thickness < in in node key order.");

            int max = json.IndexOf("\"max\"", StringComparison.Ordinal);
            int min = json.IndexOf("\"min\"", StringComparison.Ordinal);
            Assert.True(max >= 0 && min > max, "Expected 'max' before 'min' (ordinal sort).");
        }

        [Fact]
        public void Numbers_render_as_doubles_with_forced_decimal()
        {
            string json = Field.Sphere(radius: 2.0).ToJson();
            Assert.Contains("\"radius\": 2.0", json);   // integral double keeps a .0
            Assert.DoesNotContain("\"radius\": 2,", json);
            Assert.DoesNotContain("\"radius\": 2\n", json);

            string json2 = Field.Box(min: (-2, -2, -2), max: (2, 2, 2)).ToJson();
            Assert.Contains("-2.0", json2);
        }

        [Fact]
        public void Document_header_is_version_units_root()
        {
            string json = Field.Sphere(radius: 1.0).ToJson();
            Assert.Contains("\"version\": 1", json);     // integer, not 1.0
            Assert.Contains("\"units\": \"mm\"", json);
            Assert.Contains("\"root\":", json);
        }
    }
}
