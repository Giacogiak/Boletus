using System;
using System.Drawing;
using Boletus.Core;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// The wire parameter carrying a <see cref="VolumeGoo"/> between Boletus components. Plain
    /// <see cref="GH_Param{T}"/> (not persistent) — volumes are computed, never stored on the
    /// canvas. Hidden from the palette; it appears only as the in/out type on Boletus components.
    /// </summary>
    public sealed class VolumeParameter : GH_Param<VolumeGoo>
    {
        public VolumeParameter()
            : base(new GH_InstanceDescription(
                "Volume", "V", "A DualC implicit volume (field graph).", "Boletus", "Params"))
        {
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000010");

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        protected override Bitmap? Icon => BoletusIcons.Volume;
    }
}
