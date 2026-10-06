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
    /// Pins the canonical <c>isolate → thicken → skin → union</c> example graph (the four-step
    /// TPMS-part workflow — DualC <c>docs/command_reference/11-dualc_field/04-workflow-open-surface.md</c>).
    /// The GH example canvas composes exactly this graph out of the existing palette
    /// (TPMS → Normalize → Onion(Boundary=Box) for the clipped lattice shell, a second
    /// Onion on a Box for the skin, Boolean union to bond them). This test builds the same
    /// tree with the <see cref="Field"/> builders and asserts our JSON round-trips byte-identically
    /// through DualC's own canonicalizer — the automatable half of "validated" (GH can't run
    /// headless). CLI-backed comparison skips when the binary is absent; the pure guard always runs.
    /// </summary>
    public class FieldGraphExampleTests
    {
        // ---- CLI oracle plumbing (mirrors FieldGraphVocabularyTests) ----------------------

        private static string? FindCli() => TestPaths.FindCli();

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private static string Normalize(string s) => s.Replace("\r\n", "\n").Trim();

        private static string Run(string cli, params string[] args)
        {
            var psi = new ProcessStartInfo(cli)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(cli)!,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var proc = Process.Start(psi)!;
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            Assert.True(proc.ExitCode == 0, $"dualc_field failed (exit {proc.ExitCode}): {stderr}");
            return Normalize(stdout);
        }

        private static string DumpJsonOfNode(string cli, FieldNode node)
        {
            string path = Path.Combine(Path.GetTempPath(), $"boletus_ex_{Guid.NewGuid():N}.json");
            File.WriteAllText(path, node.ToJson(), Utf8NoBom);
            try { return Run(cli, path, "--dump-json"); }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        private static string DumpJsonOfExpr(string cli, string expr) => Run(cli, "--expr", expr, "--dump-json");

        // ---- The composition (built from the same ops the GH example canvas emits) --------

        // Unit cube [-0.5,0.5]³, gyroid λ=0.3, thickness 0.03 — DualC 11-dualc_field/04-workflow-open-surface.md.
        private const string ExampleExpr =
            "union(" +
              "intersection(" +
                "onion(normalize(gyroid(wavelength=0.3)),thickness=0.03)," +
                "box(min=[-0.5,-0.5,-0.5],max=[0.5,0.5,0.5]))," +
              "onion(box(min=[-0.5,-0.5,-0.5],max=[0.5,0.5,0.5]),thickness=0.03))";

        private static FieldNode BuildExample()
        {
            var min = (-0.5, -0.5, -0.5);
            var max = (0.5, 0.5, 0.5);

            // isolate + thicken: clip the shelled, metric gyroid to the box.
            var latticeShell = Field.Intersection(
                Field.Onion(Field.Normalize(Field.Gyroid(0.3)), 0.03),
                Field.Box(min, max));

            // skin: shell the enclosing box's own surface (box SDF is already metric — no normalize).
            var skin = Field.Onion(Field.Box(min, max), 0.03);

            // union: bond lattice + skin into one field-graph, one contour.
            return Field.Union(latticeShell, skin);
        }

        // ---- Pure guard (always runs) -----------------------------------------------------

        [Fact]
        public void Four_step_example_graph_is_valid_and_serializes()
        {
            var node = BuildExample();

            Assert.Empty(FieldGraphValidator.Validate(node));

            string json = node.ToJson();
            Assert.False(string.IsNullOrWhiteSpace(json));
            foreach (var op in new[] { "union", "intersection", "onion", "normalize", "gyroid", "box" })
                Assert.Contains($"\"op\": \"{op}\"", json);
        }

        // ---- CLI-gated round-trip (the validation gate) -----------------------------------

        [Fact]
        public void Four_step_example_graph_round_trips_through_dump_json()
        {
            string? cli = FindCli();
            if (cli is null) return; // DualC CLI not present: skip the byte-match.

            string ours = DumpJsonOfNode(cli, BuildExample());
            string reference = DumpJsonOfExpr(cli, ExampleExpr);
            Assert.Equal(reference, ours);
        }

        // ---- End-to-end: the composition contours to a non-empty part (no CLI needed) -----

        [Fact]
        public void Four_step_example_graph_contours_to_a_non_empty_mesh()
        {
            // Exercises Boletus's own serializer → native FromJson → Contour end-to-end (runs
            // regardless of the CLI). Safe at depth 6 (repo OOM discipline): the onion(box) skin is
            // a large solid shell that alone guarantees a non-empty mesh even if thin walls thin out.
            using var field = DualcField.FromJson(BuildExample().ToJson());
            var p = DualcContourParams.Default();
            p.MaxDepth = 6;
            var mesh = field.Contour(p);
            Assert.True(mesh.VertexCount > 0);
            Assert.True(mesh.TriangleCount > 0);
        }
    }
}
