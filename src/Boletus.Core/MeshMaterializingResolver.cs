using System;
using System.IO;
using Boletus.Core.FieldGraph;

namespace Boletus.Core
{
    /// <summary>
    /// Turns a meshless <see cref="Volume"/> into a field-graph a <b>separate process</b> — DualC's
    /// <c>dualc_field_view</c> GPU viewer (the Phase-5 live-preview side-car) — can read. The viewer
    /// only accepts a text graph plus mesh files <b>on disk</b>: unlike the in-process C ABI it has no
    /// in-memory mesh channel (<c>*_with_meshes</c>). So where <see cref="VolumeResolver"/> rewrites an
    /// in-memory <c>mesh</c>/<c>winding</c> leaf to <c>mesh(id=…)</c> and hands the buffer to the DLL,
    /// this writes each referenced <see cref="MeshBuffer"/> to a temp OBJ under <c>destDir</c> and
    /// rewrites the leaf to <c>mesh(path=…)</c> pointing at that file.
    /// </summary>
    /// <remarks>
    /// Rhino-free, so the rewrite is unit-testable on a plain runner (parallel to
    /// <see cref="VolumeResolver"/>). A mesh-free graph (TPMS / analytic / booleans of those) is
    /// returned <b>unchanged, by reference</b> — no files are written.
    /// </remarks>
    public static class MeshMaterializingResolver
    {
        /// <summary>
        /// Rewrite every in-memory mesh leaf of <paramref name="volume"/> to a disk-backed
        /// <c>mesh(path=…)</c>/<c>winding(path=…)</c>, writing the OBJ files into
        /// <paramref name="destDir"/> (created if missing). The emitted path uses forward slashes
        /// (the DualC graph parser is happiest with them) and is absolute. Identical buffers share one
        /// file — the id is a content hash, so a mesh used as both clip and skin bakes once.
        /// </summary>
        public static FieldNode MaterializeToDisk(Volume volume, string destDir)
        {
            if (volume is null) throw new ArgumentNullException(nameof(volume));
            if (string.IsNullOrEmpty(destDir)) throw new ArgumentException("destDir is required.", nameof(destDir));

            var meshes = volume.Meshes;
            bool created = false;

            FieldNode Map(FieldNode node)
            {
                if (!Volume.TryGetMemoryMeshId(node, out var id)) return node;
                if (!meshes.TryGetValue(id, out var buffer))
                    throw new InvalidOperationException(
                        $"Volume references in-memory mesh '{id}' but no buffer is attached.");

                if (!created) { Directory.CreateDirectory(destDir); created = true; }

                string objPath = Path.Combine(destDir, $"mesh_{id}.obj");
                // Content-hash id ⇒ same geometry ⇒ same file; write once.
                if (!File.Exists(objPath)) buffer.WriteObj(objPath);

                // Keep the "path" key, swap the mem:// placeholder for the real (forward-slash) path.
                var p = FieldTree.CopyParams(node.Params);
                p["path"] = FieldValue.Text(objPath.Replace('\\', '/'));
                return new FieldNode(node.Op, p, node.Children);
            }

            return FieldTree.Rewrite(volume.Core, Map);
        }
    }
}
