using System;
using System.Drawing;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Source: a primitive placed by one point in the engine's own orientation — cones, prisms,
    /// the octahedron, pyramid, partial and cut shapes (<see cref="PrimitiveCatalog.Axial"/>).
    /// P0..P3 rename themselves after the type; orient the result with Transform.
    /// </summary>
    public sealed class AxialPrimitiveComponent : PrimitiveFamilyComponent
    {
        public AxialPrimitiveComponent()
            : base("Axial Primitive", "AxPrim",
                   "A primitive placed by one point in its own axis frame (cone, cappedcone, hexprism, " +
                   "triprism, octahedron, pyramid, solidangle, cappedtorus, link, cutsphere, " +
                   "cuthollowsphere, deathstar, rhombus, verticalcapsule, roundedcylinder, infinitecone). " +
                   "The inputs rename themselves after the selected type; an empty number takes the " +
                   "type's default; angles are in degrees. Orient it with Transform.",
                   PrimitiveCatalog.Axial, new[] { Point3d.Origin })
        {
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000042");

        protected override Bitmap? Icon => BoletusIcons.AxialPrimitive;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
