using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Decorator: apply a Rhino transform (translate/rotate/scale/affine) to a volume's domain.
    /// </summary>
    /// <remarks>The 4×4 is passed row-major to DualC's <c>transform</c> op. If a transformed shape
    /// appears to move the wrong way, the op expects the inverse — flip <c>xf.TryGetInverse</c> here.
    /// (Documented as a known convention to verify; lowest-priority component for the MVP.)</remarks>
    public sealed class TransformComponent : GH_Component
    {
        public TransformComponent()
            : base("Transform", "Xform",
                   "Apply a Rhino transform (translate/rotate/scale/affine) to a volume.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "Volume to transform.", GH_ParamAccess.item);
            pManager.AddTransformParameter("Transform", "X", "Rhino transform.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The transformed volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? input = null;
            if (!da.GetData(0, ref input) || input?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }
            Transform xf = Transform.Identity;
            da.GetData(1, ref xf);

            double[] m =
            {
                xf.M00, xf.M01, xf.M02, xf.M03,
                xf.M10, xf.M11, xf.M12, xf.M13,
                xf.M20, xf.M21, xf.M22, xf.M23,
                xf.M30, xf.M31, xf.M32, xf.M33,
            };

            da.SetData(0, new VolumeGoo(input.Value.WithCore(Field.Transform(input.Value.Core, m))));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000090");

        protected override Bitmap? Icon => BoletusIcons.Transform;

        public override GH_Exposure Exposure => GH_Exposure.secondary;
    }
}
