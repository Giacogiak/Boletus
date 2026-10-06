using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Source: a triply-periodic minimal-surface (TPMS) field. Deliberately minimal — just family
    /// + wavelength (no point/boundary: a TPMS is space-filling, so its "limiting space" is supplied
    /// later by the Onion/Graded-onion boundary input, or a Boolean). Output is the infinite,
    /// non-metric lattice field; the canonical metric chain is TPMS → Normalize → Onion.
    /// </summary>
    public sealed class TpmsComponent : GH_Component
    {
        public TpmsComponent()
            : base("TPMS", "TPMS",
                   "A triply-periodic minimal-surface field (space-filling). Clip it to a region with " +
                   "the Onion boundary input or a Boolean.",
                   "Boletus", "Sources")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var family = new Param_Integer
            {
                Name = "Family",
                NickName = "F",
                Description =
                    "TPMS family: 0 Gyroid, 1 Schwarz-P, 2 Diamond, 3 Fischer-Koch, " +
                    "4 Lidinoid, 5 Neovius.",
                Optional = false,
            };
            family.AddNamedValue("Gyroid", 0);
            family.AddNamedValue("Schwarz-P", 1);
            family.AddNamedValue("Diamond", 2);
            family.AddNamedValue("Fischer-Koch", 3);
            family.AddNamedValue("Lidinoid", 4);
            family.AddNamedValue("Neovius", 5);
            family.SetPersistentData(0);
            pManager.AddParameter(family);

            pManager.AddNumberParameter("Wavelength", "W",
                "Cell wavelength (optional; engine default if omitted).", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "The TPMS field (infinite, non-metric).", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            int family = 0;
            da.GetData(0, ref family);

            double wavelength = double.NaN;
            bool hasW = da.GetData(1, ref wavelength);
            double? w = hasW ? wavelength : (double?)null;

            FieldNode? node = family switch
            {
                0 => Field.Gyroid(w),
                1 => Field.SchwarzP(w),
                2 => Field.Diamond(w),
                3 => Field.FischerKoch(w),
                4 => Field.Lidinoid(w),
                5 => Field.Neovius(w),
                _ => null,
            };

            if (node is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Unknown TPMS family index {family} (expected 0..5).");
                return;
            }

            da.SetData(0, new VolumeGoo(new Volume(node)));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000020");

        protected override Bitmap? Icon => BoletusIcons.Tpms;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
