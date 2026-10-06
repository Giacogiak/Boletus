using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Decorator (standalone): a hollow shell whose wall thickness ramps with a control field —
    /// <c>t1</c> where control ≤ <c>d0</c>, <c>t2</c> where control ≥ <c>d1</c>. Optional boundary
    /// clip applied <b>after</b> the thickness. The metric-graded shell workflow.
    /// </summary>
    public sealed class GradedOnionComponent : GH_Component
    {
        public GradedOnionComponent()
            : base("Graded Onion", "GOnion",
                   "Variable-thickness hollow shell driven by a control field; optional boundary clip after.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Base", "V", "Base volume to shell (metric).", GH_ParamAccess.item);
            pManager.AddParameter(new VolumeParameter(), "Control", "C", "Control field driving the thickness ramp.", GH_ParamAccess.item);
            pManager.AddNumberParameter("t1", "t1", "Thickness where control ≤ d0.", GH_ParamAccess.item, 0.05);
            pManager.AddNumberParameter("t2", "t2", "Thickness where control ≥ d1.", GH_ParamAccess.item, 0.2);
            pManager.AddNumberParameter("d1", "d1", "Control value where the ramp ends.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("d0", "d0", "Control value where the ramp starts (optional; default 0).", GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager.AddParameter(new VolumeParameter(), "Boundary", "B",
                "Optional clip volume; applied after the thickness.", GH_ParamAccess.item);
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The graded shell volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? baseGoo = null, controlGoo = null;
            if (!da.GetData(0, ref baseGoo) || baseGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No base volume connected.");
                return;
            }
            if (!da.GetData(1, ref controlGoo) || controlGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No control volume connected.");
                return;
            }

            double t1 = 0.05, t2 = 0.2, d1 = 1.0, d0 = double.NaN;
            da.GetData(2, ref t1);
            da.GetData(3, ref t2);
            da.GetData(4, ref d1);
            bool hasD0 = da.GetData(5, ref d0);

            VolumeGoo? boundary = null;
            bool hasBoundary = da.GetData(6, ref boundary) && boundary?.Value?.Core != null;

            FieldNode shell = Field.GradedOnion(
                baseGoo.Value.Core, controlGoo.Value.Core, t1, t2, d1, hasD0 ? d0 : (double?)null);

            if (hasBoundary)
            {
                FieldNode clipped = Field.Intersection(shell, boundary!.Value.Core);
                da.SetData(0, new VolumeGoo(Volume.Combine(clipped, baseGoo.Value, controlGoo.Value, boundary.Value)));
            }
            else
            {
                da.SetData(0, new VolumeGoo(Volume.Combine(shell, baseGoo.Value, controlGoo.Value)));
            }
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000070");

        protected override Bitmap? Icon => BoletusIcons.GradedOnion;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
