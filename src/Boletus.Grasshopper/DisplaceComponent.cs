using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Domain warp: add an analytic surface bump to a volume — <c>field + amplitude·fn(frequency·p)</c>.
    /// Amplitude and frequency are optional; omit either to take DualC's engine default (0.1 / 6).
    /// Amplitude is a world-space displacement, so a metric input is wanted — Normalize a raw TPMS first.
    /// </summary>
    public sealed class DisplaceComponent : GH_Component
    {
        public DisplaceComponent()
            : base("Displace", "Displace",
                   "Add an analytic surface bump to a volume (field + Amplitude·fn(Frequency·p)). Inputs: " +
                   "Volume (V) to displace; Function (F) dropdown — 0 Sine, 1 Gyroid, 2 Bumps; Amplitude " +
                   "(A, optional) — peak displacement in world units (engine default 0.1); Frequency (Q, " +
                   "optional) — spatial frequency of the bump pattern (engine default 6). Amplitude is a " +
                   "world distance, so Normalize a raw TPMS first.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "Volume to displace.", GH_ParamAccess.item);

            var fn = new Param_Integer
            {
                Name = "Function",
                NickName = "F",
                Description = "Bump function: 0 Sine, 1 Gyroid, 2 Bumps.",
                Optional = false,
            };
            fn.AddNamedValue("Sine", 0);
            fn.AddNamedValue("Gyroid", 1);
            fn.AddNamedValue("Bumps", 2);
            fn.SetPersistentData(0);
            pManager.AddParameter(fn);

            pManager.AddNumberParameter("Amplitude", "A",
                "Peak displacement, world units (optional; engine default 0.1). Metric: Normalize a raw TPMS first.",
                GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddNumberParameter("Frequency", "Q",
                "Spatial frequency of the bump pattern (optional; engine default 6).", GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The displaced volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? input = null;
            if (!da.GetData(0, ref input) || input?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }

            int fn = 0;
            da.GetData(1, ref fn);
            string? fnName = fn switch
            {
                0 => "sine",
                1 => "gyroid",
                2 => "bumps",
                _ => null,
            };
            if (fnName is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unknown bump function index {fn} (expected 0..2).");
                return;
            }

            double amp = double.NaN;
            double? amplitude = da.GetData(2, ref amp) ? amp : (double?)null;

            double freq = double.NaN;
            double? frequency = da.GetData(3, ref freq) ? freq : (double?)null;

            da.SetData(0, new VolumeGoo(input.Value.WithCore(
                Field.Displace(input.Value.Core, fnName, amplitude, frequency))));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-0000000000E0");

        protected override Bitmap? Icon => BoletusIcons.Displace;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
