using System;
using System.Runtime.InteropServices;

namespace Boletus.Core
{
    /// <summary>
    /// A managed copy of a contoured triangle mesh: flat arrays, 3 floats per vertex
    /// for <see cref="Positions"/>/<see cref="Normals"/> (index-aligned), 3 indices
    /// per triangle (0-based, fan-triangulated). Owns no native memory.
    /// </summary>
    public sealed class DualcMeshData
    {
        public float[] Positions { get; private set; } = Array.Empty<float>();
        public float[] Normals   { get; private set; } = Array.Empty<float>();
        public int[]   Indices   { get; private set; } = Array.Empty<int>();

        public int VertexCount   => Positions.Length / 3;
        public int TriangleCount => Indices.Length / 3;

        /// <summary>Deep-copy out of the ABI-owned native mesh. The caller releases
        /// the native mesh afterwards; this copy is independent of it.</summary>
        internal static DualcMeshData CopyFrom(in MeshNative m)
        {
            int nv = (int)m.vertexCount;
            int nt = (int)m.triangleCount;
            var d = new DualcMeshData
            {
                Positions = new float[nv * 3],
                Normals   = new float[nv * 3],
                Indices   = new int[nt * 3],
            };
            if (nv > 0)
            {
                Marshal.Copy(m.positions, d.Positions, 0, nv * 3);
                Marshal.Copy(m.normals,   d.Normals,   0, nv * 3);
            }
            // uint32 -> int: bit-identical for counts < 2^31 (always the case here).
            if (nt > 0)
                Marshal.Copy(m.indices, d.Indices, 0, nt * 3);
            return d;
        }
    }
}
