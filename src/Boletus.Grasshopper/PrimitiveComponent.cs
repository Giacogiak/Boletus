using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Source: an analytic primitive volume — the usual boundary/clip solid for a lattice. Multi-mode
    /// (type dropdown). Inputs A/B/N0/N1 are interpreted per type (see each option below); bounded
    /// primitives auto-fit at contour, plane is infinite. The rest of DualC's primitives are the
    /// catalog-driven Segment / Axial Primitive components (D-25).
    /// </summary>
    public sealed class PrimitiveComponent : GH_Component
    {
        public PrimitiveComponent()
            : base("Primitive", "Prim",
                   "An analytic primitive volume (box/sphere/roundbox/torus/plane/boxframe/ellipsoid). " +
                   "A/B are points, N0/N1 are numbers, interpreted per type.",
                   "Boletus", "Sources")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var type = new Param_Integer { Name = "Type", NickName = "T", Description =
                "Primitive type: 0 Box, 1 Sphere, 2 RoundBox, 3 Torus, 4 Plane, 5 BoxFrame, 6 Ellipsoid." };
            type.AddNamedValue("Box", 0);
            type.AddNamedValue("Sphere", 1);
            type.AddNamedValue("RoundBox", 2);
            type.AddNamedValue("Torus", 3);
            type.AddNamedValue("Plane", 4);
            type.AddNamedValue("BoxFrame", 5);
            type.AddNamedValue("Ellipsoid", 6);
            type.SetPersistentData(0);
            pManager.AddParameter(type);

            pManager.AddPointParameter("A", "A",
                "Box/RoundBox/BoxFrame min · Sphere/Torus/Ellipsoid center · Plane normal (as a vector).",
                GH_ParamAccess.item, new Point3d(-1, -1, -1));
            pManager.AddPointParameter("B", "B",
                "Box/RoundBox/BoxFrame max · Ellipsoid radii (x, y, z) (ignored otherwise).", GH_ParamAccess.item, new Point3d(1, 1, 1));
            pManager.AddNumberParameter("N0", "N0",
                "Sphere/RoundBox radius · Torus major · Plane offset.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("N1", "N1",
                "Torus minor · BoxFrame edge thickness (ignored otherwise).", GH_ParamAccess.item, 0.25);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The primitive volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            int type = 0;
            da.GetData(0, ref type);
            Point3d a = new Point3d(-1, -1, -1); da.GetData(1, ref a);
            Point3d b = new Point3d(1, 1, 1); da.GetData(2, ref b);
            double n0 = 1.0; da.GetData(3, ref n0);
            double n1 = 0.25; da.GetData(4, ref n1);

            FieldNode node = type switch
            {
                0 => Field.Box((a.X, a.Y, a.Z), (b.X, b.Y, b.Z)),
                1 => Field.Sphere(n0, (a.X, a.Y, a.Z)),
                2 => Field.RoundBox((a.X, a.Y, a.Z), (b.X, b.Y, b.Z), n0),
                3 => Field.Torus((a.X, a.Y, a.Z), n0, n1),
                4 => Field.Plane(a.X, a.Y, a.Z, n0),
                5 => Field.BoxFrame(a.X, a.Y, a.Z, b.X, b.Y, b.Z, n1),
                6 => Field.Ellipsoid((a.X, a.Y, a.Z), (b.X, b.Y, b.Z)),
                _ => null!,
            };

            if (node is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unknown primitive type {type}.");
                return;
            }

            if (type == 4)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Plane is infinite — clip it or set bounds at Contour.");

            da.SetData(0, new VolumeGoo(new Volume(node)));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000040");

        protected override Bitmap? Icon => BoletusIcons.Primitive;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
