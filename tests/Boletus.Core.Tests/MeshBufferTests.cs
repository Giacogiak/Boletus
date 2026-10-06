using System;
using System.IO;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Phase 3b Core verification: the new <see cref="MeshBuffer"/> (the in-memory mesh currency the
    /// Grasshopper Volume datatype carries) and the onion-before-clip composition the Onion-with-
    /// boundary component emits. Both lean on the golden contour counts (docs/design/07-invariants-and-limits.md) at the coarse
    /// maxDepth=6.
    /// </summary>
    public class MeshBufferTests
    {
        private static DualcContourParams ProxyParams()
        {
            var p = DualcContourParams.Default();
            p.MaxDepth = 6;
            return p;
        }

        /// <summary>The unit cube of cube.obj (centered at origin, ±0.5), built in memory.</summary>
        private static MeshBuffer UnitCube()
        {
            float[] v =
            {
                -0.5f, -0.5f, -0.5f,
                 0.5f, -0.5f, -0.5f,
                -0.5f,  0.5f, -0.5f,
                 0.5f,  0.5f, -0.5f,
                -0.5f, -0.5f,  0.5f,
                 0.5f, -0.5f,  0.5f,
                -0.5f,  0.5f,  0.5f,
                 0.5f,  0.5f,  0.5f,
            };
            // cube.obj faces, converted to 0-based.
            int[] t =
            {
                0, 2, 3,  0, 3, 1,
                4, 5, 7,  4, 7, 6,
                0, 1, 5,  0, 5, 4,
                2, 6, 7,  2, 7, 3,
                0, 4, 6,  0, 6, 2,
                1, 3, 7,  1, 7, 5,
            };
            return new MeshBuffer(v, t);
        }

        [Fact]
        public void WriteObj_cube_hits_the_mesh_path_golden_counts()
        {
            // A MeshBuffer written to OBJ must be a DualC mesh source byte-equivalent to the cube.obj
            // fixture — so the headline mesh-path graph hits the same golden counts.
            string path = Path.Combine(Path.GetTempPath(), $"boletus_meshbuf_{Guid.NewGuid():N}.obj")
                .Replace('\\', '/');
            try
            {
                UnitCube().WriteObj(path);

                var graph = Field.Intersection(
                    Field.Onion(Field.Gyroid(0.5), 0.1),
                    Field.Mesh(path));

                using var field = DualcField.FromJson(graph.ToJson());
                var mesh = field.Contour(ProxyParams());

                Assert.Equal(70032, mesh.VertexCount);
                Assert.Equal(120612, mesh.TriangleCount);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void InMemory_mesh_source_hits_the_same_golden_counts_as_the_temp_file()
        {
            // THE marshaling acceptance gate (roadmap 07 §1): the same headline graph as the
            // temp-OBJ baseline above, but the cube reaches DualC as an in-memory buffer via
            // dualc_field_create_from_json_with_meshes. DualC copies the buffers during the create
            // call; for the exactly-representable ±0.5 cube the result is byte-identical to the OBJ
            // path, so the counts must match exactly. A shifted count signals a struct-layout /
            // pinning regression across the P/Invoke boundary.
            var graph = Field.Intersection(
                Field.Onion(Field.Gyroid(0.5), 0.1),
                Field.Mesh(id: "cube"));

            var meshes = new System.Collections.Generic.Dictionary<string, MeshBuffer>
            {
                ["cube"] = UnitCube(),
            };

            using var field = DualcField.FromJson(graph.ToJson(), meshes);
            var mesh = field.Contour(ProxyParams());

            Assert.Equal(70032, mesh.VertexCount);
            Assert.Equal(120612, mesh.TriangleCount);
        }

        [Fact]
        public void Onion_then_boundary_clip_matches_the_analytic_golden_counts()
        {
            // The exact graph the Onion component emits with a boundary input:
            // intersection(onion(child), boundary) — onion BEFORE the clip. Hitting the analytic
            // golden counts proves the ordering is correct (a clip-before-onion graph would differ).
            var graph = Field.Intersection(
                Field.Onion(Field.Gyroid(0.5), 0.12),
                Field.Box((-1, -1, -1), (1, 1, 1)));

            using var field = DualcField.FromJson(graph.ToJson());
            var mesh = field.Contour(ProxyParams());

            Assert.Equal(101476, mesh.VertexCount);
            Assert.Equal(163740, mesh.TriangleCount);
        }

        [Fact]
        public void ContentHash_is_stable_and_distinct()
        {
            var a = UnitCube();
            var b = UnitCube();
            Assert.Equal(a.ContentHash(), b.ContentHash());

            // Perturb one coordinate → different hash.
            float[] v = (float[])CloneVerts(a);
            v[0] += 1.0f;
            var c = new MeshBuffer(v, CloneTris(a));
            Assert.NotEqual(a.ContentHash(), c.ContentHash());
        }

        [Fact]
        public void WriteObj_emits_positions_and_faces()
        {
            string path = Path.Combine(Path.GetTempPath(), $"boletus_objfmt_{Guid.NewGuid():N}.obj");
            try
            {
                UnitCube().WriteObj(path);
                string[] lines = File.ReadAllLines(path);
                int vLines = 0, fLines = 0;
                foreach (var line in lines)
                {
                    if (line.StartsWith("v ", StringComparison.Ordinal)) vLines++;
                    else if (line.StartsWith("f ", StringComparison.Ordinal)) fLines++;
                }
                Assert.Equal(8, vLines);
                Assert.Equal(12, fLines);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static float[] CloneVerts(MeshBuffer m) => (float[])m.Vertices.Clone();
        private static int[] CloneTris(MeshBuffer m) => (int[])m.Triangles.Clone();
    }
}
