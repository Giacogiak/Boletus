using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Decorator: inflate a <b>solid</b> field by an offset that ramps with a control field —
    /// <c>t1</c> where control ≤ <c>d0</c>, <c>t2</c> where control ≥ <c>d1</c>. Use this (not the
    /// Onion / Graded Onion) to grade a strut lattice's <b>radius</b> across space — an Onion would
    /// hollow the struts into tubes. The result is still infinite (the base lattice is): clip it
    /// downstream with a Boolean or a terminal boundary.
    /// </summary>
    public sealed class GradedOffsetComponent : GH_Component
    {
        public GradedOffsetComponent()
            : base("Graded Offset", "GOffset",
                   "Grade a SOLID field's radius/offset across space with a control field (t1 where " +
                   "control <= d0, t2 where control >= d1). Use this - not Onion - to vary a strut " +
                   "lattice's radius (Onion would hollow the struts). Common controls: a Primitive " +
                   "sphere (radial |p|) or plane (axial), or the clip mesh (fatten toward the skin).",
                   "Boletus", "Decorators")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Base", "V",
                "The solid field to inflate (e.g. a strut lattice; should be metric).", GH_ParamAccess.item);
            pManager.AddParameter(new VolumeParameter(), "Control", "C",
                "Control field driving the offset ramp (a Primitive sphere = radial, plane = axial, mesh = distance-to-skin).",
                GH_ParamAccess.item);
            pManager.AddNumberParameter("t1", "t1", "Offset added where control ≤ d0.", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("t2", "t2", "Offset added where control ≥ d1.", GH_ParamAccess.item, 0.05);
            pManager.AddNumberParameter("d1", "d1", "Control value where the ramp ends.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("d0", "d0", "Control value where the ramp starts (optional; default 0).", GH_ParamAccess.item);
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The graded (inflated) volume.", GH_ParamAccess.item);
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

            double t1 = 0.0, t2 = 0.05, d1 = 1.0, d0 = double.NaN;
            da.GetData(2, ref t1);
            da.GetData(3, ref t2);
            da.GetData(4, ref d1);
            bool hasD0 = da.GetData(5, ref d0);

            FieldNode node = Field.GradedOffset(
                baseGoo.Value.Core, controlGoo.Value.Core, t1, t2, d1, hasD0 ? d0 : (double?)null);

            da.SetData(0, new VolumeGoo(Volume.Combine(node, baseGoo.Value, controlGoo.Value)));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-0000000000F0");

        protected override Bitmap? Icon => BoletusIcons.GradedOffset;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
