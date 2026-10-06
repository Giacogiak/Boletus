using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Decorator: shift a volume's level set outward (+) or inward (−) by a world-space distance —
    /// <c>offset(field, r) = field − r</c>. A pure level-set move; the gradient is unchanged. Wants a
    /// metric input for the distance to mean world units — Normalize a raw TPMS first.
    /// </summary>
    public sealed class OffsetComponent : GH_Component
    {
        public OffsetComponent()
            : base("Offset", "Offset",
                   "Grow (+) or shrink (−) a volume by shifting its level set a world-space distance " +
                   "(offset = field − Distance). Inputs: Volume (V) to offset; Distance (D) — the signed " +
                   "shift in world units (+ grows / − shrinks). A pure level-set move: the surface slides " +
                   "along its normal and the gradient is unchanged. Wants a metric input for the distance " +
                   "to mean world units — Normalize a raw TPMS first.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "Volume to offset.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "D",
                "Level-set shift in world units (+ grows / − shrinks). Metric: Normalize a raw TPMS first.",
                GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The offset volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? input = null;
            if (!da.GetData(0, ref input) || input?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }
            double r = 0.0;
            da.GetData(1, ref r);

            da.SetData(0, new VolumeGoo(input.Value.WithCore(Field.Offset(input.Value.Core, r))));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-0000000000B0");

        protected override Bitmap? Icon => BoletusIcons.Offset;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
