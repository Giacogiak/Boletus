using System;
using System.Drawing;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Source: a primitive spanned between two points — capsule, capped cylinder, round cone,
    /// vesica, infinite cylinder (<see cref="PrimitiveCatalog.Segment"/>). A/B are the axis ends
    /// (InfiniteCylinder: a point and a direction), R0/R1 the lengths each type reads.
    /// </summary>
    public sealed class SegmentPrimitiveComponent : PrimitiveFamilyComponent
    {
        public SegmentPrimitiveComponent()
            : base("Segment Primitive", "SegPrim",
                   "A primitive spanned between two points (capsule/cappedcylinder/roundcone/vesica/" +
                   "infinitecylinder). The inputs rename themselves after the selected type; an empty " +
                   "number takes the type's default.",
                   PrimitiveCatalog.Segment, new[] { new Point3d(0, -1, 0), new Point3d(0, 1, 0) })
        {
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000041");

        protected override Bitmap? Icon => BoletusIcons.SegmentPrimitive;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
