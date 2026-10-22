using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// DualC's <c>porous</c> source end to end (roadmap 11 #36): the sponge as the canvas
    /// composes it against DualC's own sample, export parity with the CLI, an in-memory mesh
    /// volume used twice, a cancel during a porous export, and the Live Preview's OBJ path
    /// against the in-memory one. Every contour stays at depth 6 (DualC measured the porous
    /// contours at 0.30–0.44 GB there; depth 7 is 1.6–3.2 GB and does not belong in
    /// <c>dotnet test</c>). The CLI-backed facts skip when the binary is absent.
    /// </summary>
    public sealed class PorousShellTests : IDisposable
    {
        private readonly string _dir =
            Path.Combine(Path.GetTempPath(), "BoletusPorousTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort temp cleanup */ }
        }

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static DualcContourParams Depth6()
        {
            var p = DualcContourParams.Default();
            p.MaxDepth = 6;
            return p;
        }

        private static string RunCli(string cli, params string[] args)
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
            return stdout.Replace("\r\n", "\n").Trim();
        }

        // ---- The sponge, as the canvas composes it -----------------------------------------
        // Porous Shell(Volume = Sphere r 1, Spacing 0.4) → Graded Onion(Control = Sphere r 0,
        // t1 0.04, t2 0.08, d1 1) → Boolean smooth intersection (k 0.1) with the sphere: DualC's
        // porous-sponge.json, the worked example W5.

        private static FieldNode BuildSponge() =>
            Field.SmoothIntersection(
                Field.Sphere(radius: 1.0),
                Field.GradedOnion(Field.Porous(Field.Sphere(radius: 1.0), 0.4), Field.Sphere(radius: 0.0),
                    t1: 0.04, t2: 0.08, d1: 1.0),
                k: 0.1);

        [Fact]
        public void Sponge_is_valid_and_matches_dualc_sample()
        {
            Assert.Empty(FieldGraphValidator.Validate(BuildSponge()));

            string? cli = TestPaths.FindCli();
            string sample = Path.Combine(TestPaths.SamplesDir, "porous-sponge.json");
            if (cli is null || !File.Exists(sample)) return;

            string ours = Path.Combine(Path.GetTempPath(), $"boletus_sponge_{Guid.NewGuid():N}.json");
            File.WriteAllText(ours, BuildSponge().ToJson(), Utf8NoBom);
            try { Assert.Equal(RunCli(cli, sample, "--dump-json"), RunCli(cli, ours, "--dump-json")); }
            finally { File.Delete(ours); }
        }

        [Fact]
        public void Sponge_contours_to_a_non_empty_mesh()
        {
            using var field = DualcField.FromJson(BuildSponge().ToJson());
            var mesh = field.Contour(Depth6(), out DualcDiagnostics diag);
            Assert.True(mesh.TriangleCount > 0);
            Assert.False(diag.EmptyContour);
        }

        // ---- Export parity with the CLI ----------------------------------------------------

        // DualC's own watertight library graph (V2 at depth 6): 40,086 vertices / 80,116 triangles.
        private const string FoamExpr =
            "smooth-intersection(sphere(radius=1),onion(porous(sphere(radius=1),spacing=0.4,seed=3),thickness=0.06),k=0.1)";

        private static string Sha1(string path)
        {
            using var sha = SHA1.Create();
            using var fs = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(fs));
        }

        [Fact]
        public void Porous_export_is_byte_identical_to_the_cli()
        {
            string? cli = TestPaths.FindCli();
            if (cli is null) return;

            Directory.CreateDirectory(_dir);
            string wrapperStl = Path.Combine(_dir, "w.stl");
            string cliStl = Path.Combine(_dir, "c.stl");
            var node = Field.SmoothIntersection(Field.Sphere(radius: 1),
                Field.Onion(Field.Porous(Field.Sphere(radius: 1), 0.4, seed: 3), 0.06), k: 0.1);
            using (var field = DualcField.FromJson(node.ToJson()))
                field.Export(wrapperStl, Depth6());
            RunCli(cli, "--expr", FoamExpr, "--depth", "6", "-o", cliStl);

            Assert.Equal(Sha1(cliStl), Sha1(wrapperStl));
        }

        // ---- An in-memory mesh volume, referenced twice ------------------------------------

        /// <summary>An icosahedron of circumradius 1: twelve vertices whose coordinates are not
        /// dyadic (the golden ratio), so a float written as text and read back as a double can
        /// differ from the float handed over in memory.</summary>
        private static MeshBuffer Icosahedron()
        {
            double phi = (1 + Math.Sqrt(5)) / 2, s = 1 / Math.Sqrt(1 + phi * phi);
            double[] v =
            {
                -1, phi, 0,   1, phi, 0,   -1, -phi, 0,   1, -phi, 0,
                0, -1, phi,   0, 1, phi,   0, -1, -phi,   0, 1, -phi,
                phi, 0, -1,   phi, 0, 1,   -phi, 0, -1,   -phi, 0, 1,
            };
            int[] t =
            {
                0, 11, 5,  0, 5, 1,  0, 1, 7,  0, 7, 10,  0, 10, 11,
                1, 5, 9,  5, 11, 4,  11, 10, 2,  10, 7, 6,  7, 1, 8,
                3, 9, 4,  3, 4, 2,  3, 2, 6,  3, 6, 8,  3, 8, 9,
                4, 9, 5,  2, 4, 11,  6, 2, 10,  8, 6, 7,  9, 8, 1,
            };
            return new MeshBuffer(v.Select(x => (float)(x * s)).ToArray(), t);
        }

        /// <summary>The bracket recipe over a host mesh: intersection(mesh, onion(porous(mesh))),
        /// the one buffer referenced twice, as Mesh → Volume → Porous Shell → Onion → Boolean
        /// hands it over.</summary>
        private static Volume FoamInMesh()
        {
            var mesh = new Volume(Field.Mesh(path: Volume.MemoryScheme + "ico"),
                new Dictionary<string, MeshBuffer>(StringComparer.Ordinal) { ["ico"] = Icosahedron() });
            FieldNode porous = Field.Porous(mesh.Core, 0.3);
            FieldNode foam = Field.Intersection(mesh.Core, Field.Onion(porous, 0.04));
            return Volume.Combine(foam, mesh);
        }

        private static DualcMeshData ContourInMemory(Volume volume)
        {
            var (root, meshes) = VolumeResolver.Resolve(volume);
            using var field = DualcField.FromJson(root.ToJson(), meshes);
            return field.Contour(Depth6());
        }

        [Fact]
        public void A_mesh_volume_used_twice_is_one_buffer_and_contours_deterministically()
        {
            var volume = FoamInMesh();
            Assert.Single(volume.Meshes);
            Assert.Empty(FieldGraphValidator.Validate(volume.Core));

            var a = ContourInMemory(volume);
            var b = ContourInMemory(volume);
            Assert.True(a.TriangleCount > 0);
            Assert.Equal(a.Positions, b.Positions);
            Assert.Equal(a.Indices, b.Indices);
        }

        [Fact]
        public void Live_preview_obj_path_builds_the_same_cells_as_the_in_memory_mesh()
        {
            // Live Preview hands the viewer the mesh as an OBJ (MeshMaterializingResolver); the
            // terminals hand DualC the buffer. The seeds are placed by the volume's value, so a
            // vertex that parses to another double could move one. Measured 2026-10-22: the two
            // contours have the same triangles (the same cells); a few dozen vertices at the
            // mesh's creases move by up to ~0.01, as they do for onion(mesh) with no porous node
            // at all — the mesh path's own baseline, not the seeds (roadmap 11 #36).
            var volume = FoamInMesh();
            var inMemory = ContourInMemory(volume);

            FieldNode onDisk = MeshMaterializingResolver.MaterializeToDisk(volume, _dir);
            using var field = DualcField.FromJson(onDisk.ToJson());
            var fromObj = field.Contour(Depth6());

            Assert.Equal(inMemory.TriangleCount, fromObj.TriangleCount);
            Assert.Equal(inMemory.Indices, fromObj.Indices);
        }

        // ---- Cancel: the cells are built in the create call, the export still cancels ------

        [Fact]
        public void A_cancel_during_a_porous_export_leaves_no_file_and_no_part()
        {
            Directory.CreateDirectory(_dir);
            string path = Path.Combine(_dir, "cancelled.stl");
            using var field = DualcField.FromExpr(FoamExpr);
            using var cts = new CancellationTokenSource();
            var progress = new CancelOnContour(cts);

            Assert.Throws<OperationCanceledException>(
                () => field.Export(path, Depth6(), progress, cts.Token));

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".part"));
        }

        private sealed class CancelOnContour : IProgress<DualcProgress>
        {
            private readonly CancellationTokenSource _cts;
            public CancelOnContour(CancellationTokenSource cts) => _cts = cts;
            public void Report(DualcProgress value)
            {
                if (value.Stage == DualcStage.Contour) _cts.Cancel();
            }
        }
    }
}
