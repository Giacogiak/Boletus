using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Decorator (standalone — the metric-correctness gate): rescale a non-metric field (raw TPMS,
    /// winding) toward unit gradient so downstream Onion thickness / smooth-boolean blend radius are
    /// metric (mm). The canonical chain is TPMS → Normalize → Onion.
    /// </summary>
    public sealed class NormalizeComponent : GH_Component
    {
        public NormalizeComponent()
            : base("Normalize", "Norm",
                   "Rescale a non-metric field toward unit gradient (so thickness/blend become metric).",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "Volume to normalize.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The normalized volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? input = null;
            if (!da.GetData(0, ref input) || input?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }
            da.SetData(0, new VolumeGoo(input.Value.WithCore(Field.Normalize(input.Value.Core))));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000080");

        protected override Bitmap? Icon => BoletusIcons.Normalize;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
