using System;
using System.IO;
using System.Linq;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Covers <see cref="MeshMaterializingResolver.MaterializeToDisk"/> — the viewer-specific twin of
    /// <see cref="VolumeResolver"/> that the Phase-5 live-preview side-car uses. Because the external
    /// <c>dualc_field_view</c> process can only read meshes from disk, an in-memory <c>mesh</c>/
    /// <c>winding</c> leaf must be written to a temp OBJ and its <c>mem://</c> path swapped for the real
    /// file path. Pure I/O + tree rewrite — no Rhino, no GPU — so it runs on the plain runner.
    /// </summary>
    public sealed class MeshMaterializingResolverTests : IDisposable
    {
        private readonly string _dir =
            Path.Combine(Path.GetTempPath(), "BoletusMatTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort temp cleanup */ }
        }

        // A tiny 2-triangle quad so vertex/face counts are unambiguous.
        private static MeshBuffer Quad() => new MeshBuffer(
            new float[] { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0 },
            new int[] { 0, 1, 2, 0, 2, 3 });

        private static Volume VolumeWith(FieldNode core, params (string id, MeshBuffer buf)[] meshes)
            => new Volume(core, meshes.ToDictionary(m => m.id, m => m.buf, StringComparer.Ordinal));

        private static (int v, int f) CountObj(string path)
        {
            int v = 0, f = 0;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("v ", StringComparison.Ordinal)) v++;
                else if (line.StartsWith("f ", StringComparison.Ordinal)) f++;
            }
            return (v, f);
        }

        [Fact]
        public void Mesh_free_graph_passes_through_unchanged_and_writes_nothing()
        {
            var core = Field.Intersection(
                Field.Onion(Field.Gyroid(0.5), 0.1),
                Field.Box((-1, -1, -1), (1, 1, 1)));

            var root = MeshMaterializingResolver.MaterializeToDisk(new Volume(core), _dir);

            Assert.Same(core, root);                 // no-op rewrite → original root by reference
            Assert.False(Directory.Exists(_dir));    // no leaf → dir never created
        }

        [Fact]
        public void Mesh_leaf_is_written_to_obj_and_path_rewritten()
        {
            var quad = Quad();
            var core = Field.Intersection(
                Field.Onion(Field.Gyroid(0.5), 0.1),
                Field.Mesh(path: Volume.MemoryScheme + "quad"));

            var root = MeshMaterializingResolver.MaterializeToDisk(VolumeWith(core, ("quad", quad)), _dir);

            var leaf = FindOp(root, "mesh");
            Assert.True(leaf.Params.TryGetValue("path", out var pathVal));
            Assert.DoesNotContain("mem://", pathVal.AsText);
            Assert.False(leaf.Params.ContainsKey("id"));   // viewer path is path=, not id=

            // The path points at a real OBJ whose counts match the buffer.
            string obj = pathVal.AsText;
            Assert.True(File.Exists(obj), $"expected OBJ at {obj}");
            var (v, f) = CountObj(obj);
            Assert.Equal(quad.VertexCount, v);
            Assert.Equal(quad.TriangleCount, f);

            // Surrounding structure preserved (onion-before-clip intersection).
            Assert.Equal("intersection", root.Op);
            Assert.Equal(2, root.Children.Count);
        }

        [Fact]
        public void Winding_leaves_are_materialized_too()
        {
            var soup = Quad();
            var core = Field.Winding(path: Volume.MemoryScheme + "soup");

            var root = MeshMaterializingResolver.MaterializeToDisk(VolumeWith(core, ("soup", soup)), _dir);

            Assert.Equal("winding", root.Op);
            Assert.DoesNotContain("mem://", root.Params["path"].AsText);
            Assert.True(File.Exists(root.Params["path"].AsText));
        }

        [Fact]
        public void Repeated_id_writes_one_file()
        {
            var quad = Quad();
            var core = Field.Union(
                Field.Mesh(path: Volume.MemoryScheme + "quad"),
                Field.Mesh(path: Volume.MemoryScheme + "quad"));

            MeshMaterializingResolver.MaterializeToDisk(VolumeWith(core, ("quad", quad)), _dir);

            Assert.Single(Directory.GetFiles(_dir, "*.obj"));   // content-hash id ⇒ one file
        }

        [Fact]
        public void Throws_when_a_referenced_buffer_is_missing()
        {
            var core = Field.Mesh(path: Volume.MemoryScheme + "ghost");
            var ex = Assert.Throws<InvalidOperationException>(
                () => MeshMaterializingResolver.MaterializeToDisk(new Volume(core), _dir));
            Assert.Contains("ghost", ex.Message);
        }

        [Fact]
        public void Rewritten_graph_validates_and_serializes_without_mem_scheme()
        {
            var core = Field.Intersection(
                Field.Onion(Field.Gyroid(0.5), 0.1),
                Field.Mesh(path: Volume.MemoryScheme + "quad"));

            var root = MeshMaterializingResolver.MaterializeToDisk(VolumeWith(core, ("quad", Quad())), _dir);

            string json = root.ToJson();
            Assert.Contains("\"path\"", json);
            Assert.DoesNotContain("mem://", json);
            Assert.DoesNotContain(
                FieldGraphValidator.Validate(root),
                i => i.Severity == ValidationSeverity.Error);
        }

        private static FieldNode FindOp(FieldNode node, string op)
        {
            if (node.Op == op) return node;
            foreach (var c in node.Children)
            {
                var found = FindOp(c, op);
                if (found != null) return found;
            }
            return null!;
        }
    }
}
