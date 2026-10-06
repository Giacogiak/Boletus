using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Domain warp: bend a volume into a circular arc about an axis — the angle swept is
    /// <c>curvature</c> radians per unit travelled along the axis (curvature = 1/radius). Distance
    /// from the origin within the bend plane is preserved.
    /// </summary>
    public sealed class BendComponent : GH_Component
    {
        public BendComponent()
            : base("Bend", "Bend",
                   "Bend a volume into a circular arc about an axis. Inputs: Volume (V) to bend; " +
                   "Curvature (C) — bend angle in radians per world unit along the axis (curvature = " +
                   "1/radius, so 0 is straight); Axis (A) dropdown — 0 X, 1 Y, 2 Z. Distance from the " +
                   "origin within the bend plane is preserved; no metric input required.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "Volume to bend.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Curvature", "C",
                "Bend curvature (1/radius): radians per world unit along the axis.", GH_ParamAccess.item, 0.0);

            var axis = new Param_Integer
            {
                Name = "Axis",
                NickName = "A",
                Description = "Bend axis: 0 X, 1 Y, 2 Z.",
                Optional = false,
            };
            axis.AddNamedValue("X", 0);
            axis.AddNamedValue("Y", 1);
            axis.AddNamedValue("Z", 2);
            axis.SetPersistentData(2);
            pManager.AddParameter(axis);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The bent volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? input = null;
            if (!da.GetData(0, ref input) || input?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }
            double curvature = 0.0;
            da.GetData(1, ref curvature);

            int axis = 2;
            da.GetData(2, ref axis);
            string? axisName = AxisName.From(axis);
            if (axisName is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unknown axis index {axis} (expected 0..2).");
                return;
            }

            da.SetData(0, new VolumeGoo(input.Value.WithCore(Field.Bend(input.Value.Core, curvature, axisName))));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-0000000000D0");

        protected override Bitmap? Icon => BoletusIcons.Bend;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
