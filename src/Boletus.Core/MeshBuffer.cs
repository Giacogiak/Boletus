using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Boletus.Core
{
    /// <summary>
    /// A Rhino-free, in-memory triangle mesh — the host-neutral currency for a mesh source in a
    /// field graph. The Grasshopper layer converts a <c>Rhino.Geometry.Mesh</c> into one of these;
    /// it is then handed to the native ABI directly in RAM (<c>VolumeResolver</c> + the
    /// <c>*_with_meshes</c> create call — <c>docs/design/03-volume-and-resolvers.md</c>; how it landed:
    /// <c>docs/roadmap/07-upstream-coordination/01-in-memory-mesh.md</c>) or, for the out-of-process
    /// viewer, written to a temp OBJ (<c>MeshMaterializingResolver</c>). Lives in <c>Boletus.Core</c>
    /// precisely so it can flow to the ABI without a Rhino dependency.
    /// </summary>
    public sealed class MeshBuffer
    {
        /// <summary>Vertex positions, 3 floats (x,y,z) per vertex.</summary>
        public float[] Vertices { get; }

        /// <summary>Triangle indices, 3 (0-based) per face into <see cref="Vertices"/>.</summary>
        public int[] Triangles { get; }

        /// <summary>Optional per-vertex normals, 3 floats per vertex (index-aligned), or null.</summary>
        public float[]? Normals { get; }

        public int VertexCount => Vertices.Length / 3;
        public int TriangleCount => Triangles.Length / 3;

        public MeshBuffer(float[] vertices, int[] triangles, float[]? normals = null)
        {
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            if (vertices.Length % 3 != 0)
                throw new ArgumentException("Vertices length must be a multiple of 3.", nameof(vertices));
            if (triangles.Length % 3 != 0)
                throw new ArgumentException("Triangles length must be a multiple of 3.", nameof(triangles));
            if (normals != null && normals.Length != vertices.Length)
                throw new ArgumentException("Normals length must equal Vertices length.", nameof(normals));
            Normals = normals;
        }

        /// <summary>
        /// A stable content hash (hex) over the geometry — used as the mesh-source id so identical
        /// meshes dedupe (matching DualC's per-build mesh cache). FNV-1a over the raw bytes; not
        /// cryptographic, just a fast stable key.
        /// </summary>
        public string ContentHash()
        {
            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;
            ulong h = offset;

            void Mix(byte b) { h ^= b; h *= prime; }
            void MixInt(int v)
            {
                Mix((byte)v); Mix((byte)(v >> 8)); Mix((byte)(v >> 16)); Mix((byte)(v >> 24));
            }
            void MixFloat(float f) => MixInt(BitConverter.ToInt32(BitConverter.GetBytes(f), 0));

            MixInt(Vertices.Length);
            MixInt(Triangles.Length);
            foreach (var v in Vertices) MixFloat(v);
            foreach (var t in Triangles) MixInt(t);
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Write this mesh as a minimal Wavefront OBJ (positions + faces, 1-based) at
        /// <paramref name="path"/> — the temp file a <c>mesh(path=…)</c>/<c>winding(path=…)</c> node
        /// references until the in-memory resolver lands. Forward-slash paths survive the parser.
        /// </summary>
        public void WriteObj(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            var sb = new StringBuilder(VertexCount * 24 + TriangleCount * 16);
            var ic = CultureInfo.InvariantCulture;

            for (int i = 0; i < Vertices.Length; i += 3)
            {
                sb.Append("v ")
                  .Append(Vertices[i].ToString("R", ic)).Append(' ')
                  .Append(Vertices[i + 1].ToString("R", ic)).Append(' ')
                  .Append(Vertices[i + 2].ToString("R", ic)).Append('\n');
            }

            for (int i = 0; i < Triangles.Length; i += 3)
            {
                sb.Append("f ")
                  .Append(Triangles[i] + 1).Append(' ')
                  .Append(Triangles[i + 1] + 1).Append(' ')
                  .Append(Triangles[i + 2] + 1).Append('\n');
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
