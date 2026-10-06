using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Value-lerp morph of two fields driven by a control: <c>value = lerp(A, B, w)</c>, with
    /// <c>w = clamp((control − lo)/(hi − lo))</c>. It blends <b>values, not shapes</b> — only
    /// fluid/watertight when A and B share the same lattice geometry (same crystal family, differing
    /// only in Radius). A cross-family morph (e.g. bcc↔fcc) tapers to nothing at the mid-plane and
    /// splits into two bodies with a gap — by design, not a bug. For a continuous cross-family solid
    /// use a smooth Boolean instead; for same-family radius grading, Graded Offset is cheaper.
    /// </summary>
    public sealed class MixComponent : GH_Component
    {
        public MixComponent()
            : base("Mix", "Mix",
                   "Value-lerp morph: value = lerp(A, B, w), w = clamp((control - lo)/(hi - lo)). " +
                   "Blends VALUES not shapes - only watertight when A and B are the SAME crystal " +
                   "family differing only in Radius. Cross-family morphs leave a mid-gap (use a " +
                   "smooth Boolean instead). For same-family radius grading, Graded Offset is cheaper.",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "A", "A", "Field at w=0 (control ≤ lo).", GH_ParamAccess.item);
            pManager.AddParameter(new VolumeParameter(), "B", "B", "Field at w=1 (control ≥ hi).", GH_ParamAccess.item);
            pManager.AddParameter(new VolumeParameter(), "Control", "C",
                "Scalar ramp field (a Primitive plane = axial, sphere = radial |p|).", GH_ParamAccess.item);
            pManager.AddNumberParameter("hi", "hi", "Control value where w=1 (blend fully to B).", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("lo", "lo", "Control value where w=0 (optional; default 0).", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The morphed volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? aGoo = null, bGoo = null, controlGoo = null;
            if (!da.GetData(0, ref aGoo) || aGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No A volume connected.");
                return;
            }
            if (!da.GetData(1, ref bGoo) || bGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No B volume connected.");
                return;
            }
            if (!da.GetData(2, ref controlGoo) || controlGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No control volume connected.");
                return;
            }

            double hi = 1.0, lo = double.NaN;
            da.GetData(3, ref hi);
            bool hasLo = da.GetData(4, ref lo);

            FieldNode node = Field.Mix(
                aGoo.Value.Core, bGoo.Value.Core, controlGoo.Value.Core, hi, hasLo ? lo : (double?)null);

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Mix blends field VALUES, not shapes: watertight only when A and B are the same crystal " +
                "family differing in Radius; a cross-family blend leaves a mid-gap by design.");

            da.SetData(0, new VolumeGoo(Volume.Combine(node, aGoo.Value, bGoo.Value, controlGoo.Value)));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000100");

        protected override Bitmap? Icon => BoletusIcons.Mix;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
