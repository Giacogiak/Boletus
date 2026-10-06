using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Grasshopper assembly metadata for the Boletus plugin (shown in the GH plugin manager).
    /// </summary>
    public sealed class BoletusInfo : GH_AssemblyInfo
    {
        public override string Name => "Boletus";

        public override string Description =>
            "Implicit field-graph modeling for Rhino/Grasshopper, powered by DualC.";

        public override Bitmap? Icon => BoletusIcons.Boletus;

        public override Guid Id => new Guid("4F1B2A30-0000-4000-8000-000000000001");

        public override string AuthorName => "Giacomo Forcina";

        public override string AuthorContact => "forcina.giacomo@gmail.com";

        public override string Version => "0.1.0";
    }
}
