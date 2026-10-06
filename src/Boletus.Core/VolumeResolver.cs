using System;
using System.Collections.Generic;
using Boletus.Core.FieldGraph;

namespace Boletus.Core
{
    /// <summary>
    /// Turns a meshless <see cref="Volume"/> into the diskless inputs DualC v0.3.0 takes at the
    /// Contour/Export terminal: for every <c>mesh</c>/<c>winding</c> leaf whose <c>path</c> is a
    /// <see cref="Volume.MemoryScheme"/> placeholder, it rewrites the leaf to reference the geometry
    /// by <c>id</c> (dropping <c>path</c>) and collects the in-memory <see cref="MeshBuffer"/>s the
    /// graph actually references. The result feeds <c>DualcField.FromJson(json, meshes)</c> — no temp
    /// file is ever written.
    /// </summary>
    public static class VolumeResolver
    {
        /// <summary>An id-referenced graph + the buffers it references (a subset of the Volume's,
        /// deduped). No disk is touched, so nothing needs disposing.</summary>
        public static (FieldNode Root, IReadOnlyDictionary<string, MeshBuffer> Meshes) Resolve(Volume volume)
        {
            if (volume is null) throw new ArgumentNullException(nameof(volume));

            var meshes = volume.Meshes;
            var used = new Dictionary<string, MeshBuffer>(StringComparer.Ordinal);

            FieldNode Map(FieldNode node)
            {
                if (!Volume.TryGetMemoryMeshId(node, out var id)) return node;
                if (!meshes.TryGetValue(id, out var buffer))
                    throw new InvalidOperationException(
                        $"Volume references in-memory mesh '{id}' but no buffer is attached.");

                used[id] = buffer;

                // Swap path=mem://<id> → id=<id> so the graph references the host buffer DualC copies.
                var p = FieldTree.CopyParams(node.Params);
                p.Remove("path");
                p["id"] = FieldValue.Text(id);
                return new FieldNode(node.Op, p, node.Children);
            }

            var root = FieldTree.Rewrite(volume.Core, Map);
            return (root, used);
        }
    }
}
