using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Decorator (standalone — the lattice-shell maker): hollow a volume to a wall ≈ 2·thickness,
    /// then optionally clip to a boundary volume. The clip is applied <b>after</b> the thickness
    /// (<c>intersection(onion(field), boundary)</c>) so the cut faces are clean, not shelled.
    /// Wants a metric input — feed it through Normalize after a raw TPMS.
    /// </summary>
    public sealed class OnionComponent : GH_Component
    {
        public OnionComponent()
            : base("Onion", "Onion",
                   "Hollow shell (wall ≈ 2·thickness); optional boundary clip applied after the thickness.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "Volume to hollow (metric).", GH_ParamAccess.item);
            pManager.AddNumberParameter("Thickness", "T", "Wall half-thickness (world units).", GH_ParamAccess.item, 0.1);
            pManager.AddParameter(new VolumeParameter(), "Boundary", "B",
                "Optional clip volume; applied after the thickness.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The shelled (and optionally clipped) volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? input = null;
            if (!da.GetData(0, ref input) || input?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }
            double thickness = 0.1;
            da.GetData(1, ref thickness);

            VolumeGoo? boundary = null;
            bool hasBoundary = da.GetData(2, ref boundary) && boundary?.Value?.Core != null;

            FieldNode shell = Field.Onion(input.Value.Core, thickness);
            if (hasBoundary)
            {
                FieldNode clipped = Field.Intersection(shell, boundary!.Value.Core);
                da.SetData(0, new VolumeGoo(Volume.Combine(clipped, input.Value, boundary.Value)));
            }
            else
            {
                da.SetData(0, new VolumeGoo(input.Value.WithCore(shell)));
            }
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000060");

        protected override Bitmap? Icon => BoletusIcons.Onion;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
