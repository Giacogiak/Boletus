using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Phase 3a vocabulary fan-out — every op beyond the MVP set, each gated by a round-trip
    /// through DualC's own canonicalizer. For each op we build the node with its <see cref="Field"/>
    /// builder, emit our JSON, and assert <c>dualc_field &lt;ours&gt; --dump-json</c> is byte-identical
    /// to <c>dualc_field --expr "&lt;equivalent&gt;" --dump-json</c>. Both pass through DualC's emitter,
    /// so equality ⟺ the graphs are semantically equal — this is what pins each flat-<c>params</c>
    /// positional count, each grouped key, and each per-op param type. CLI-backed tests skip (no
    /// assertion) when the binary is absent; the pure guards always run.
    /// </summary>
    public class FieldGraphVocabularyTests
    {
        // ---- CLI oracle plumbing (mirrors FieldGraphTests) --------------------------------

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
            string path = Path.Combine(Path.GetTempPath(), $"boletus_voc_{Guid.NewGuid():N}.json");
            File.WriteAllText(path, node.ToJson(), Utf8NoBom);
            try { return Run(cli, path, "--dump-json"); }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        private static string DumpJsonOfExpr(string cli, string expr) => Run(cli, "--expr", expr, "--dump-json");

        // ---- The per-op round-trip cases --------------------------------------------------

        public static IEnumerable<object[]> Cases()
        {
            // Grouped-key primitives (named vector keys).
            yield return Case("roundbox", Field.RoundBox((-1, -1, -1), (1, 1, 1), 0.3),
                "roundbox(min=[-1,-1,-1],max=[1,1,1],radius=0.3)");
            yield return Case("capsule", Field.Capsule((-1, 0, 0), (1, 0, 0), 0.5),
                "capsule(a=[-1,0,0],b=[1,0,0],radius=0.5)");
            yield return Case("cappedcylinder", Field.CappedCylinder((0, -1, 0), (0, 1, 0), 0.5),
                "cappedcylinder(a=[0,-1,0],b=[0,1,0],radius=0.5)");
            yield return Case("torus", Field.Torus((0, 0, 0), 1, 0.3),
                "torus(center=[0,0,0],major=1,minor=0.3)");
            yield return Case("ellipsoid", Field.Ellipsoid((0, 0, 0), (1, 0.6, 0.4)),
                "ellipsoid(center=[0,0,0],radii=[1,0.6,0.4])");

            // Flat-params primitives (universal positional params=[…]) — every one, to pin counts.
            yield return Case("plane", Field.Plane(0, 1, 0, 0), "plane(0,1,0,0)");
            yield return Case("boxframe", Field.BoxFrame(-1, -1, -1, 1, 1, 1, 0.1), "boxframe(-1,-1,-1,1,1,1,0.1)");
            yield return Case("cone", Field.Cone(0, 1, 0, 0.5, 2), "cone(0,1,0,0.5,2)");
            yield return Case("cappedcone", Field.CappedCone(0, 0, 0, 1, 1, 0.5), "cappedcone(0,0,0,1,1,0.5)");
            yield return Case("roundcone", Field.RoundCone(0, -1, 0, 0, 1, 0, 0.6, 0.3), "roundcone(0,-1,0,0,1,0,0.6,0.3)");
            yield return Case("infinitecylinder", Field.InfiniteCylinder(0, 0, 0, 0, 1, 0, 0.5), "infinitecylinder(0,0,0,0,1,0,0.5)");
            yield return Case("hexprism", Field.HexPrism(0, 0, 0, 1, 1), "hexprism(0,0,0,1,1)");
            yield return Case("triprism", Field.TriPrism(0, 0, 0, 1, 1), "triprism(0,0,0,1,1)");
            yield return Case("octahedron", Field.Octahedron(0, 0, 0, 1), "octahedron(0,0,0,1)");
            yield return Case("pyramid", Field.Pyramid(0, 0, 0, 1.5), "pyramid(0,0,0,1.5)");
            yield return Case("solidangle", Field.SolidAngle(0, 0, 0, 0.7, 1.5), "solidangle(0,0,0,0.7,1.5)");
            yield return Case("cappedtorus", Field.CappedTorus(0, 0, 0, 1.0, 1, 0.3), "cappedtorus(0,0,0,1.0,1,0.3)");
            yield return Case("link", Field.Link(0, 0, 0, 0.5, 1, 0.3), "link(0,0,0,0.5,1,0.3)");
            yield return Case("cutsphere", Field.CutSphere(0, 0, 0, 1, 0.3), "cutsphere(0,0,0,1,0.3)");
            yield return Case("cuthollowsphere", Field.CutHollowSphere(0, 0, 0, 1, -0.2, 0.1), "cuthollowsphere(0,0,0,1,-0.2,0.1)");
            yield return Case("deathstar", Field.DeathStar(0, 0, 0, 1, 0.7, 0.9), "deathstar(0,0,0,1,0.7,0.9)");
            yield return Case("vesica", Field.Vesica(0, -1, 0, 0, 1, 0, 0.6), "vesica(0,-1,0,0,1,0,0.6)");
            yield return Case("rhombus", Field.Rhombus(0, 0, 0, 1, 0.6, 0.3, 0), "rhombus(0,0,0,1,0.6,0.3,0)");
            yield return Case("verticalcapsule", Field.VerticalCapsule(0, 0, 0, 1.5, 0.4), "verticalcapsule(0,0,0,1.5,0.4)");
            yield return Case("roundedcylinder", Field.RoundedCylinder(0, 0, 0, 1, 0.2, 1), "roundedcylinder(0,0,0,1,0.2,1)");
            yield return Case("triangle", Field.Triangle(0, 0, 0, 1, 0, 0, 0, 1, 0), "triangle(0,0,0,1,0,0,0,1,0)");
            yield return Case("quad", Field.Quad(0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0), "quad(0,0,0,1,0,0,1,1,0,0,1,0)");
            yield return Case("infinitecone", Field.InfiniteCone(0, 0, 0, 0.5), "infinitecone(0,0,0,0.5)");

            // Domain operators (one child).
            yield return Case("elongate", Field.Elongate(Field.Box(), (1, 0, 0)), "elongate(box(),h=[1,0,0])");
            yield return Case("transform", Field.Transform(Field.Box(), Identity4x4),
                "transform(box(),matrix=[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1])");
            yield return Case("twist", Field.Twist(Field.Box(), 1.5, "z"), "twist(box(),radiansPerUnit=1.5,axis=z)");
            yield return Case("bend", Field.Bend(Field.Box(), 0.5, "x"), "bend(box(),curvature=0.5,axis=x)");
            yield return Case("mirror", Field.Mirror(Field.Sphere(radius: 0.6, center: (0.6, 0, 0)), (1, 0, 0)),
                "mirror(sphere(center=[0.6,0,0],radius=0.6),normal=[1,0,0])");
            yield return Case("repeat", Field.Repeat(Field.Sphere(), (3, 0, 0)), "repeat(sphere(),period=[3,0,0])");
            yield return Case("repeat-limited", Field.RepeatLimited(Field.Sphere(), (3, 0, 0), (4, 1, 1)),
                "repeat-limited(sphere(),period=[3,0,0],count=[4,1,1])");
            yield return Case("displace", Field.Displace(Field.Box(), "sine", 0.1, 6), "displace(box(),fn=sine,amplitude=0.1,frequency=6)");

            // Strut lattices (grouped-key sources; center omitted at default). nodeRadius omitted
            // ⇒ uniform; supplied ⇒ tapered.
            yield return Case("sc", Field.Sc(wavelength: 0.5, radius: 0.05), "sc(wavelength=0.5,radius=0.05)");
            yield return Case("bcc", Field.Bcc(wavelength: 0.5, radius: 0.05), "bcc(wavelength=0.5,radius=0.05)");
            yield return Case("fcc", Field.Fcc(wavelength: 0.5, radius: 0.05), "fcc(wavelength=0.5,radius=0.05)");
            yield return Case("octet", Field.Octet(wavelength: 0.5, radius: 0.05), "octet(wavelength=0.5,radius=0.05)");
            yield return Case("octet-tapered", Field.Octet(wavelength: 0.5, radius: 0.02, nodeRadius: 0.06),
                "octet(wavelength=0.5,radius=0.02,nodeRadius=0.06)");

            // graded-offset (two children: base strut, control field).
            yield return Case("graded-offset",
                Field.GradedOffset(Field.Bcc(wavelength: 0.4, radius: 0.02), Field.Sphere(radius: 0), t1: 0.0, t2: 0.06, d1: 1.0, d0: 0.0),
                "graded-offset(bcc(wavelength=0.4,radius=0.02),sphere(radius=0),t1=0.0,t2=0.06,d0=0.0,d1=1.0)");

            // mix (three children: A, B, control) — same-family radius morph. The control is a
            // positional plane so both sides dump the flat `params` form of plane (DualC's
            // --dump-json is representation-preserving: positional→params, grouped→named keys).
            yield return Case("mix",
                Field.Mix(Field.Bcc(wavelength: 0.5, radius: 0.03), Field.Bcc(wavelength: 0.5, radius: 0.09), Field.Plane(1, 0, 0, 0), hi: 0.6, lo: -0.6),
                "mix(bcc(wavelength=0.5,radius=0.03),bcc(wavelength=0.5,radius=0.09),plane(1,0,0,0),lo=-0.6,hi=0.6)");
        }

        private static readonly double[] Identity4x4 = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };

        private static object[] Case(string label, FieldNode node, string expr) => new object[] { label, node, expr };

        [Theory]
        [MemberData(nameof(Cases))]
        public void Op_round_trips_through_dump_json(string label, FieldNode node, string expr)
        {
            _ = label; // surfaced in the test name for failure triage
            string? cli = FindCli();
            if (cli is null) return;

            string ours = DumpJsonOfNode(cli, node);
            string reference = DumpJsonOfExpr(cli, expr);
            Assert.Equal(reference, ours);
        }

        // ---- Targeted guards (pure; no CLI) -----------------------------------------------

        [Fact]
        public void Flat_params_primitive_emits_a_positional_array_of_the_right_length()
        {
            // hexprism is cx cy cz radius halfLength → exactly 5 positional values under "params".
            var node = Field.HexPrism(0, 0, 0, 1, 1);
            Assert.True(node.Params.TryGetValue("params", out var v));
            Assert.Equal(FieldValueKind.Vector, v.Kind);
            Assert.Equal(5, v.AsVector.Count);
        }

        [Fact]
        public void Twist_axis_is_a_string_not_a_vector()
        {
            // The load-bearing per-op quirk: rotate's axis is a vector, but twist/bend's axis is
            // the string "x"/"y"/"z". Emitted JSON must carry a quoted string.
            string json = Field.Twist(Field.Box(), 1.5, "z").ToJson();
            Assert.Contains("\"axis\": \"z\"", json);
            Assert.DoesNotContain("\"axis\": [", json);
        }

        [Fact]
        public void Transform_rejects_a_non_16_element_matrix()
        {
            Assert.Throws<ArgumentException>(() => Field.Transform(Field.Box(), new double[] { 1, 0, 0 }));
        }

        // ---- Acceptance: a new primitive contours to a non-empty mesh ---------------------

        [Fact]
        public void Torus_contours_to_a_non_empty_mesh()
        {
            var node = Field.Torus((0, 0, 0), 1, 0.3);
            using var field = DualcField.FromJson(node.ToJson());
            var p = DualcContourParams.Default();
            p.MaxDepth = 6; // coarse — keep contours cheap
            var mesh = field.Contour(p);
            Assert.True(mesh.VertexCount > 0);
            Assert.True(mesh.TriangleCount > 0);
        }

        [Fact]
        public void Clipped_bcc_strut_lattice_contours_to_a_non_empty_mesh()
        {
            // A strut lattice is infinite — clip it with a box so the contour has a finite solid
            // to fill. Proves the re-vendored (d6b2808) DLL parses AND contours the new vocabulary.
            var node = Field.Intersection(
                Field.Box((-1, -1, -1), (1, 1, 1)),
                Field.Bcc(wavelength: 0.5, radius: 0.05));
            using var field = DualcField.FromJson(node.ToJson());
            var p = DualcContourParams.Default();
            p.MaxDepth = 6; // coarse — a dense lattice is exponential
            var mesh = field.Contour(p);
            Assert.True(mesh.VertexCount > 0);
            Assert.True(mesh.TriangleCount > 0);
        }

        [Fact]
        public void Clipped_graded_offset_contours_to_a_non_empty_mesh()
        {
            // A 2-child decorator over a strut base, graded by a sphere control, clipped to a box.
            // Proves graded-offset MESHES (not just parses) through the re-vendored DLL.
            var node = Field.Intersection(
                Field.Box((-1, -1, -1), (1, 1, 1)),
                Field.GradedOffset(Field.Bcc(wavelength: 0.4, radius: 0.02), Field.Sphere(radius: 0),
                    t1: 0.0, t2: 0.06, d1: 1.0, d0: 0.0));
            using var field = DualcField.FromJson(node.ToJson());
            var p = DualcContourParams.Default();
            p.MaxDepth = 6;
            var mesh = field.Contour(p);
            Assert.True(mesh.VertexCount > 0);
            Assert.True(mesh.TriangleCount > 0);
        }

        [Fact]
        public void Clipped_mix_contours_to_a_non_empty_mesh()
        {
            // mix is the first arity-3 op — the child-traversal/marshaling case the 0/2-child paths
            // never exercise. Same-family radius morph (watertight), clipped to a box. Proves it MESHES.
            var node = Field.Intersection(
                Field.Box((-1, -1, -1), (1, 1, 1)),
                Field.Mix(Field.Bcc(wavelength: 0.5, radius: 0.03), Field.Bcc(wavelength: 0.5, radius: 0.09),
                    Field.Plane(1, 0, 0, 0), hi: 0.6, lo: -0.6));
            using var field = DualcField.FromJson(node.ToJson());
            var p = DualcContourParams.Default();
            p.MaxDepth = 6;
            var mesh = field.Contour(p);
            Assert.True(mesh.VertexCount > 0);
            Assert.True(mesh.TriangleCount > 0);
        }

        [Fact]
        public void Strut_nodeRadius_is_omitted_when_uniform_and_emitted_when_tapered()
        {
            // Optional param: absent ⇒ uniform strut (no key), present ⇒ tapered (key emitted).
            Assert.DoesNotContain("nodeRadius", Field.Bcc(wavelength: 0.5, radius: 0.05).ToJson());
            Assert.Contains("\"nodeRadius\": 0.06", Field.Octet(wavelength: 0.5, radius: 0.02, nodeRadius: 0.06).ToJson());
        }

        // ---- Validator spot-checks on new ops (the table tier is generic) -----------------

        [Fact]
        public void New_domain_op_missing_required_param_is_an_error()
        {
            // twist requires 'axis'; hand-build a node without it.
            var bag = new Dictionary<string, FieldValue> { ["radiansPerUnit"] = FieldValue.Scalar(1.5) };
            var node = new FieldNode("twist", bag, new[] { Field.Box() });
            Assert.Contains(FieldGraphValidator.Validate(node),
                i => i.Path == "/root/axis" && i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void Flat_params_wrong_length_is_an_error()
        {
            // hexprism wants a 5-vector; give it 3.
            var bag = new Dictionary<string, FieldValue> { ["params"] = FieldValue.Vector(0, 0, 0) };
            var node = new FieldNode("hexprism", bag);
            Assert.Contains(FieldGraphValidator.Validate(node),
                i => i.Path == "/root/params" && i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void Graded_offset_missing_required_param_is_an_error()
        {
            // graded-offset requires d1; hand-build a node without it.
            var bag = new Dictionary<string, FieldValue>
            {
                ["t1"] = FieldValue.Scalar(0.0),
                ["t2"] = FieldValue.Scalar(0.06),
            };
            var node = new FieldNode("graded-offset", bag, new[] { Field.Bcc(radius: 0.02), Field.Sphere(radius: 0) });
            Assert.Contains(FieldGraphValidator.Validate(node),
                i => i.Path == "/root/d1" && i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void Mix_requires_three_children_and_a_hi_param()
        {
            // Wrong arity: two children where mix needs three.
            var twoKids = new FieldNode("mix",
                new Dictionary<string, FieldValue> { ["hi"] = FieldValue.Scalar(1.0) },
                new[] { Field.Bcc(radius: 0.03), Field.Bcc(radius: 0.09) });
            Assert.Contains(FieldGraphValidator.Validate(twoKids),
                i => i.Path == "/root" && i.Severity == ValidationSeverity.Error && i.Message.Contains("child"));

            // Missing required 'hi'.
            var noHi = new FieldNode("mix", new Dictionary<string, FieldValue>(),
                new[] { Field.Bcc(radius: 0.03), Field.Bcc(radius: 0.09), Field.Plane(1, 0, 0, 0) });
            Assert.Contains(FieldGraphValidator.Validate(noHi),
                i => i.Path == "/root/hi" && i.Severity == ValidationSeverity.Error);
        }
    }
}
