using System;
using System.Collections.Generic;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Source: convert a Rhino mesh into a Volume — the in-memory route into the field graph. The
    /// geometry is kept in the Volume as a <see cref="MeshBuffer"/> (NO disk write); at Contour/Export
    /// it is handed to DualC in RAM via <c>mesh(id=…)</c> + the v0.3.0 <c>*_with_meshes</c> create
    /// (no temp file). "Kind" chooses the field: a closed mesh's signed distance (parity/pseudonormal)
    /// or a winding-number field for open shells / triangle soup. The output is <b>always metric</b> —
    /// the parity/pseudonormal SDFs already are, and the non-metric <c>winding</c> field is wrapped in
    /// <c>normalize</c> here so onion / smooth booleans get real world-unit thickness with no separate
    /// Normalize component.
    /// </summary>
    public sealed class MeshToVolumeComponent : GH_Component
    {
        public MeshToVolumeComponent()
            : base("Mesh → Volume", "Mesh",
                   "Turn a Rhino mesh into a Volume (kept in memory; handed to DualC in RAM at " +
                   "Contour/Export — no temp file).",
                   "Boletus", "Sources")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Rhino mesh to convert.", GH_ParamAccess.item);

            var kind = new Param_Integer
            {
                Name = "Kind",
                NickName = "K",
                Description =
                    "Source field kind: 0 Mesh parity (closed watertight mesh; robust default), " +
                    "1 Mesh pseudonormal (closed watertight mesh; faster, same result on clean input), " +
                    "2 Winding (open shells / triangle soup; seals holes). " +
                    "All three output a metric Volume - Winding is auto-normalized here - so no " +
                    "separate Normalize is needed downstream.",
            };
            kind.AddNamedValue("Mesh (closed, parity)", 0);
            kind.AddNamedValue("Mesh (closed, pseudonormal)", 1);
            kind.AddNamedValue("Winding (open / soup)", 2);
            kind.SetPersistentData(0);
            pManager.AddParameter(kind);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "The mesh volume (always metric; Winding is auto-normalized).", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            Mesh? mesh = null;
            if (!da.GetData(0, ref mesh) || mesh is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No mesh connected.");
                return;
            }
            int kind = 0;
            da.GetData(1, ref kind);

            if (mesh.Vertices.Count == 0 || mesh.Faces.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh has no vertices/faces.");
                return;
            }

            MeshBuffer buffer = ToMeshBuffer(mesh);
            string id = buffer.ContentHash();
            string memPath = Volume.MemoryScheme + id;

            FieldNode node = kind switch
            {
                0 => Field.Mesh(memPath, sign: "parity"),
                1 => Field.Mesh(memPath, sign: "pseudonormal"),
                // Winding is non-metric; wrap it in normalize here so a Mesh → Volume always emits a
                // metric Volume (onion/smooth booleans get real world-unit thickness with no separate
                // Normalize). normalize preserves the zero-surface and sign, so hard booleans/clips
                // over it stay correct too. The two mesh kinds are already true SDFs — no wrap needed.
                2 => Field.Normalize(Field.Winding(memPath)),
                _ => null!,
            };
            if (node is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unknown kind {kind}.");
                return;
            }

            var meshes = new Dictionary<string, MeshBuffer>(StringComparer.Ordinal) { [id] = buffer };
            da.SetData(0, new VolumeGoo(new Volume(node, meshes)));
        }

        /// <summary>Flatten a Rhino mesh (tris + quads) into a Rhino-free <see cref="MeshBuffer"/>.</summary>
        private static MeshBuffer ToMeshBuffer(Mesh mesh)
        {
            int vc = mesh.Vertices.Count;
            var verts = new float[vc * 3];
            for (int i = 0; i < vc; i++)
            {
                Point3f p = mesh.Vertices[i];
                verts[i * 3] = p.X;
                verts[i * 3 + 1] = p.Y;
                verts[i * 3 + 2] = p.Z;
            }

            var tris = new List<int>(mesh.Faces.Count * 3);
            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                MeshFace f = mesh.Faces[i];
                tris.Add(f.A); tris.Add(f.B); tris.Add(f.C);
                if (f.IsQuad) { tris.Add(f.A); tris.Add(f.C); tris.Add(f.D); }
            }

            return new MeshBuffer(verts, tris.ToArray());
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000050");

        protected override Bitmap? Icon => BoletusIcons.MeshToVolume;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
