using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Combine two volumes with a CSG operator. Hard ops (union/intersection/difference/xor) work
    /// over non-metric inputs; the smooth ops blend by radius <c>k</c> and assume metric inputs
    /// (Normalize first). This is also the way to clip a TPMS <b>without</b> thickening — e.g.
    /// <c>intersection(tpms, solid)</c> for a network solid.
    /// </summary>
    public sealed class BooleanComponent : GH_Component
    {
        public BooleanComponent()
            : base("Boolean", "Bool",
                   "CSG of two volumes (union/intersection/difference/xor + smooth variants). " +
                   "Smooth ops blend by radius k and assume metric inputs: primitives and Mesh -> " +
                   "Volume parity/pseudonormal are already metric (no Normalize needed); only " +
                   "Winding and raw TPMS need a Normalize first.",
                   "Boletus", "Booleans")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var op = new Param_Integer
            {
                Name = "Operation",
                NickName = "Op",
                Description =
                    "Boolean operation: 0 Union, 1 Intersection, 2 Difference, 3 Xor, " +
                    "4 Smooth Union, 5 Smooth Intersection, 6 Smooth Difference. " +
                    "k applies only to the smooth ops (4-6); the hard ops (0-3) ignore it. " +
                    "Smooth ops assume metric (distance) inputs.",
            };
            op.AddNamedValue("Union", 0);
            op.AddNamedValue("Intersection", 1);
            op.AddNamedValue("Difference", 2);
            op.AddNamedValue("Xor", 3);
            op.AddNamedValue("Smooth Union", 4);
            op.AddNamedValue("Smooth Intersection", 5);
            op.AddNamedValue("Smooth Difference", 6);
            op.SetPersistentData(0);
            pManager.AddParameter(op);

            pManager.AddParameter(new VolumeParameter(), "A", "A", "First volume.", GH_ParamAccess.item);
            pManager.AddParameter(new VolumeParameter(), "B", "B", "Second volume.", GH_ParamAccess.item);
            pManager.AddNumberParameter("k", "k",
                "Smooth blend radius in world/model units (smooth ops 4-6 only; engine default 0.25 " +
                "if omitted). Scale it to the model: for a 50-100 mm part try single-digit-to-tens of " +
                "mm. A k far below one contour cell (~ model size / 2^Depth) gives only a faint blend " +
                "- raise k or Depth.",
                GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The combined volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            int op = 0;
            da.GetData(0, ref op);

            VolumeGoo? aGoo = null, bGoo = null;
            if (!da.GetData(1, ref aGoo) || aGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No A volume connected.");
                return;
            }
            if (!da.GetData(2, ref bGoo) || bGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No B volume connected.");
                return;
            }

            double k = double.NaN;
            bool hasK = da.GetData(3, ref k);
            double? kk = hasK ? k : (double?)null;

            FieldNode a = aGoo.Value.Core, b = bGoo.Value.Core;
            FieldNode node = op switch
            {
                0 => Field.Union(a, b),
                1 => Field.Intersection(a, b),
                2 => Field.Difference(a, b),
                3 => Field.Xor(a, b),
                4 => Field.SmoothUnion(a, b, kk),
                5 => Field.SmoothIntersection(a, b, kk),
                6 => Field.SmoothDifference(a, b, kk),
                _ => null!,
            };
            if (node is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unknown operation {op}.");
                return;
            }
            if (hasK && op < 4)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "k is ignored for hard booleans - choose a Smooth variant (op 4-6) to blend.");

            da.SetData(0, new VolumeGoo(Volume.Combine(node, aGoo.Value, bGoo.Value)));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-0000000000A0");

        protected override Bitmap? Icon => BoletusIcons.Boolean;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
