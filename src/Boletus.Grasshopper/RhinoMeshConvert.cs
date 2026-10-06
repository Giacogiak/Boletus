using Boletus.Core;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Shared conversion from DualC's flat float/int arrays (<see cref="DualcMeshData"/>) into a
    /// <see cref="Rhino.Geometry.Mesh"/>. Used by every component that materializes a contour
    /// (<see cref="ContourExportComponent"/>, <see cref="ProxyPreviewComponent"/>).
    /// </summary>
    internal static class RhinoMeshConvert
    {
        /// <summary>Convert DualC's flat float/int arrays into a Rhino mesh.</summary>
        public static Mesh ToRhinoMesh(DualcMeshData d)
        {
            var mesh = new Mesh();

            float[] pos = d.Positions;
            for (int i = 0; i < pos.Length; i += 3)
                mesh.Vertices.Add(pos[i], pos[i + 1], pos[i + 2]);

            float[] nrm = d.Normals;
            bool hasNormals = nrm.Length == pos.Length && nrm.Length > 0;
            if (hasNormals)
                for (int i = 0; i < nrm.Length; i += 3)
                    mesh.Normals.Add(nrm[i], nrm[i + 1], nrm[i + 2]);

            int[] idx = d.Indices;
            for (int i = 0; i < idx.Length; i += 3)
                mesh.Faces.AddFace(idx[i], idx[i + 1], idx[i + 2]);

            if (!hasNormals) mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }
    }
}
