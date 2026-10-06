using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Domain warp: twist a volume helically about an axis — the plane perpendicular to the axis is
    /// rotated by <c>rate</c> radians for every unit travelled along it. Distance from the axis is
    /// preserved.
    /// </summary>
    public sealed class TwistComponent : GH_Component
    {
        public TwistComponent()
            : base("Twist", "Twist",
                   "Twist a volume helically about an axis — the plane perpendicular to the axis rotates " +
                   "as you travel along it. Inputs: Volume (V) to twist; Rate (R) — radians of twist per " +
                   "world unit along the axis; Axis (A) dropdown — 0 X, 1 Y, 2 Z. Distance from the axis " +
                   "is preserved; no metric input required.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "Volume to twist.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rate", "R",
                "Twist rate: radians per world unit along the axis.", GH_ParamAccess.item, 0.0);

            var axis = new Param_Integer
            {
                Name = "Axis",
                NickName = "A",
                Description = "Twist axis: 0 X, 1 Y, 2 Z.",
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
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The twisted volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? input = null;
            if (!da.GetData(0, ref input) || input?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }
            double rate = 0.0;
            da.GetData(1, ref rate);

            int axis = 2;
            da.GetData(2, ref axis);
            string? axisName = AxisName.From(axis);
            if (axisName is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unknown axis index {axis} (expected 0..2).");
                return;
            }

            da.SetData(0, new VolumeGoo(input.Value.WithCore(Field.Twist(input.Value.Core, rate, axisName))));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-0000000000C0");

        protected override Bitmap? Icon => BoletusIcons.Twist;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
