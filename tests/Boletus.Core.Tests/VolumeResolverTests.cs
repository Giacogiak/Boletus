using System.Collections.Generic;
using System.Linq;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Covers the GH-terminal rewrite that was previously untestable while it lived in the
    /// net7.0-windows plugin project: <see cref="VolumeResolver.Resolve"/> (swap the <c>mem://&lt;id&gt;</c>
    /// placeholder for <c>mesh(id=…)</c> and collect the referenced buffers) and the shared
    /// <see cref="FieldTree.Rewrite"/> skeleton. These are pure tree transforms — no native engine,
    /// no Rhino — so they run on the plain test runner.
    /// </summary>
    public class VolumeResolverTests
    {
        private static MeshBuffer Cube() =>
            new MeshBuffer(new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }, new int[] { 0, 1, 2 });

        private static Volume VolumeWith(FieldNode core, params (string id, MeshBuffer buf)[] meshes)
        {
            var dict = meshes.ToDictionary(m => m.id, m => m.buf);
            return new Volume(core, dict);
        }

        [Fact]
        public void Resolve_swaps_mem_path_for_id_and_collects_the_buffer()
        {
            var cube = Cube();
            var core = Field.Intersection(
                Field.Onion(Field.Gyroid(0.5), 0.1),
                Field.Mesh(path: Volume.MemoryScheme + "cube"));
            var volume = VolumeWith(core, ("cube", cube));

            var (root, meshes) = VolumeResolver.Resolve(volume);

            // The mesh leaf now references id=, not the mem:// path.
            var leaf = FindOp(root, "mesh");
            Assert.False(leaf.Params.ContainsKey("path"));
            Assert.True(leaf.Params.TryGetValue("id", out var idVal));
            Assert.Equal("cube", idVal.AsText);

            // Only the referenced buffer is collected, keyed by its id.
            Assert.Single(meshes);
            Assert.Same(cube, meshes["cube"]);

            // The surrounding graph structure is preserved (onion-before-clip intersection).
            Assert.Equal("intersection", root.Op);
            Assert.Equal(2, root.Children.Count);
        }

        [Fact]
        public void Resolve_handles_winding_leaves_too()
        {
            var soup = Cube();
            var core = Field.Winding(path: Volume.MemoryScheme + "soup");
            var (root, meshes) = VolumeResolver.Resolve(VolumeWith(core, ("soup", soup)));

            Assert.Equal("winding", root.Op);
            Assert.False(root.Params.ContainsKey("path"));
            Assert.Equal("soup", root.Params["id"].AsText);
            Assert.Same(soup, meshes["soup"]);
        }

        [Fact]
        public void Resolve_dedupes_repeated_ids_and_ignores_unreferenced_buffers()
        {
            var cube = Cube();
            var extra = new MeshBuffer(new float[] { 0, 0, 0, 2, 0, 0, 0, 2, 0 }, new int[] { 0, 1, 2 });
            // Two leaves referencing the SAME id; an extra buffer the graph never references.
            var core = Field.Union(
                Field.Mesh(path: Volume.MemoryScheme + "cube"),
                Field.Mesh(path: Volume.MemoryScheme + "cube"));
            var volume = VolumeWith(core, ("cube", cube), ("unused", extra));

            var (_, meshes) = VolumeResolver.Resolve(volume);

            Assert.Single(meshes);                 // deduped to one, and 'unused' dropped
            Assert.True(meshes.ContainsKey("cube"));
            Assert.False(meshes.ContainsKey("unused"));
        }

        [Fact]
        public void Resolve_leaves_a_disk_path_mesh_untouched()
        {
            // A plain on-disk mesh(path=...) is not a mem:// placeholder — pass through unchanged.
            var core = Field.Mesh(path: "C:/models/part.obj");
            var (root, meshes) = VolumeResolver.Resolve(new Volume(core));

            Assert.Same(core, root);               // unchanged subtree returned by reference
            Assert.Empty(meshes);
        }

        [Fact]
        public void Resolve_throws_when_a_referenced_buffer_is_missing()
        {
            var core = Field.Mesh(path: Volume.MemoryScheme + "ghost");
            var volume = new Volume(core); // no buffers attached

            var ex = Assert.Throws<System.InvalidOperationException>(
                () => VolumeResolver.Resolve(volume));
            Assert.Contains("ghost", ex.Message);
        }

        [Fact]
        public void Resolved_graph_serializes_with_mesh_id_and_validates()
        {
            var core = Field.Intersection(
                Field.Onion(Field.Gyroid(0.5), 0.1),
                Field.Mesh(path: Volume.MemoryScheme + "cube"));
            var (root, _) = VolumeResolver.Resolve(VolumeWith(core, ("cube", Cube())));

            // Serializes to the id= form DualC's *_with_meshes create expects...
            Assert.Contains("\"id\"", root.ToJson());
            Assert.DoesNotContain("mem://", root.ToJson());
            // ...and the rewritten graph is itself valid (exactly-one-of path/id satisfied).
            Assert.DoesNotContain(
                FieldGraphValidator.Validate(root),
                i => i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void FieldTree_Rewrite_returns_the_same_instance_for_a_noop()
        {
            var tree = Field.Union(Field.Sphere(1.0), Field.Box((-1, -1, -1), (1, 1, 1)));
            var same = FieldTree.Rewrite(tree, n => n);
            Assert.Same(tree, same); // no change anywhere → original root, no reallocation
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
