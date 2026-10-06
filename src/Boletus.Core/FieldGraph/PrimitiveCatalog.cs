using System;
using System.Collections.Generic;

namespace Boletus.Core.FieldGraph
{
    /// <summary>
    /// What one shape makes of one input slot of its family: the name, nickname and tooltip the
    /// slot takes while that shape is selected, and — for a number slot — the value used when the
    /// input is left empty. <see cref="IsAngle"/> marks a slot entered in degrees (the engine takes
    /// radians; <see cref="PrimitiveShape.Build"/> converts).
    /// </summary>
    public sealed class PrimitiveSlotUse
    {
        public PrimitiveSlotUse(string name, string nickName, string description, double defaultValue = 0, bool isAngle = false)
        {
            Name = name;
            NickName = nickName;
            Description = description;
            Default = defaultValue;
            IsAngle = isAngle;
        }

        public string Name { get; }
        public string NickName { get; }
        public string Description { get; }
        public double Default { get; }
        public bool IsAngle { get; }
    }

    /// <summary>
    /// One entry of a family's type dropdown. <see cref="Points"/> and <see cref="Numbers"/> are
    /// aligned with the family's point and number slots; a <c>null</c> entry is a slot this shape
    /// ignores.
    /// </summary>
    public sealed class PrimitiveShape
    {
        private readonly Func<(double X, double Y, double Z)[], double[], FieldNode> _build;

        internal PrimitiveShape(int index, string name, bool isInfinite,
            PrimitiveSlotUse?[] points, PrimitiveSlotUse?[] numbers,
            Func<(double X, double Y, double Z)[], double[], FieldNode> build)
        {
            Index = index;
            Name = name;
            IsInfinite = isInfinite;
            Points = points;
            Numbers = numbers;
            _build = build;
        }

        public int Index { get; }
        public string Name { get; }
        /// <summary>Unbounded: the terminal needs Min/Max, or the volume a clip.</summary>
        public bool IsInfinite { get; }
        public IReadOnlyList<PrimitiveSlotUse?> Points { get; }
        public IReadOnlyList<PrimitiveSlotUse?> Numbers { get; }

        /// <summary>
        /// Build the node. <paramref name="points"/> holds one value per point slot;
        /// <paramref name="numbers"/> one per number slot, <c>null</c> for an empty input (the
        /// shape's default is used). Angle slots are read in degrees.
        /// </summary>
        public FieldNode Build(IReadOnlyList<(double X, double Y, double Z)> points, IReadOnlyList<double?> numbers)
        {
            if (points.Count != Points.Count)
                throw new ArgumentException($"{Name} takes {Points.Count} points, got {points.Count}.", nameof(points));
            if (numbers.Count != Numbers.Count)
                throw new ArgumentException($"{Name} takes {Numbers.Count} numbers, got {numbers.Count}.", nameof(numbers));

            var p = new (double X, double Y, double Z)[points.Count];
            for (int i = 0; i < p.Length; i++) p[i] = points[i];

            var n = new double[numbers.Count];
            for (int i = 0; i < n.Length; i++)
            {
                var use = Numbers[i];
                if (use is null) continue;
                double v = numbers[i] ?? use.Default;
                n[i] = use.IsAngle ? v * Math.PI / 180.0 : v;
            }
            return _build(p, n);
        }
    }

    /// <summary>
    /// A family of primitives sharing one component: a fixed row of point slots and number slots
    /// (so a saved canvas never loses a wire) whose meaning changes with the type dropdown.
    /// <see cref="Shapes"/> is indexed by the dropdown value, which is an ID: a shape keeps its index
    /// for good, and a new one is appended.
    /// </summary>
    public sealed class PrimitiveFamily
    {
        internal PrimitiveFamily(string name, string[] pointSlots, string[] numberSlots, PrimitiveShape[] shapes)
        {
            Name = name;
            PointSlots = pointSlots;
            NumberSlots = numberSlots;
            Shapes = shapes;
            for (int i = 0; i < shapes.Length; i++)
                if (shapes[i].Index != i)
                    throw new InvalidOperationException($"{name}: shape '{shapes[i].Name}' sits at {i}, not its index {shapes[i].Index}.");
        }

        public string Name { get; }
        /// <summary>The generic nickname of each point slot (what a slot shows while ignored).</summary>
        public IReadOnlyList<string> PointSlots { get; }
        /// <summary>The generic nickname of each number slot.</summary>
        public IReadOnlyList<string> NumberSlots { get; }
        public IReadOnlyList<PrimitiveShape> Shapes { get; }

        public PrimitiveShape? Find(int index) =>
            index >= 0 && index < Shapes.Count ? Shapes[index] : null;
    }

    /// <summary>
    /// The primitives beyond the five of the <c>Primitive</c> component, grouped by the shape of
    /// their parameters (D-25): <see cref="Segment"/> — two points and up to two lengths;
    /// <see cref="Axial"/> — a centre and up to four numbers, in the engine's own orientation
    /// (orient with <c>Transform</c>). Defaults are DualC's own primitive defaults, angles rounded
    /// to whole degrees. <c>triangle</c> / <c>quad</c> are not here (D-45).
    /// </summary>
    public static class PrimitiveCatalog
    {
        private static PrimitiveSlotUse Num(string name, string nick, string what, double def, bool angle = false) =>
            new PrimitiveSlotUse(name, nick,
                $"{what} (optional; default {def.ToString(System.Globalization.CultureInfo.InvariantCulture)}{(angle ? "°" : "")}).",
                def, angle);

        private static readonly PrimitiveSlotUse EndA = new("A", "A", "First end point of the axis.");
        private static readonly PrimitiveSlotUse EndB = new("B", "B", "Second end point of the axis.");

        public static PrimitiveFamily Segment { get; } = new PrimitiveFamily("Segment Primitive",
            new[] { "A", "B" }, new[] { "R0", "R1" }, new[]
            {
                new PrimitiveShape(0, "Capsule", false,
                    new[] { EndA, EndB },
                    new[] { Num("Radius", "R", "Radius of the rounded segment", 0.5), null },
                    (p, n) => Field.Capsule(p[0], p[1], n[0])),
                new PrimitiveShape(1, "CappedCylinder", false,
                    new[] { EndA, EndB },
                    new[] { Num("Radius", "R", "Cylinder radius (flat caps at A and B)", 0.5), null },
                    (p, n) => Field.CappedCylinder(p[0], p[1], n[0])),
                new PrimitiveShape(2, "RoundCone", false,
                    new[] { EndA, EndB },
                    new[] { Num("RadiusA", "RA", "Sphere radius at A", 0.6), Num("RadiusB", "RB", "Sphere radius at B", 0.3) },
                    (p, n) => Field.RoundCone(p[0].X, p[0].Y, p[0].Z, p[1].X, p[1].Y, p[1].Z, n[0], n[1])),
                new PrimitiveShape(3, "Vesica", false,
                    new[] { EndA, EndB },
                    new[] { Num("Width", "W", "Half-width of the lens at the midpoint", 0.6), null },
                    (p, n) => Field.Vesica(p[0].X, p[0].Y, p[0].Z, p[1].X, p[1].Y, p[1].Z, n[0])),
                new PrimitiveShape(4, "InfiniteCylinder", true,
                    new[] { new PrimitiveSlotUse("Point", "P", "A point on the cylinder axis."),
                            new PrimitiveSlotUse("Direction", "D", "The axis direction (read as a vector).") },
                    new[] { Num("Radius", "R", "Cylinder radius", 0.5), null },
                    (p, n) => Field.InfiniteCylinder(p[0].X, p[0].Y, p[0].Z, p[1].X, p[1].Y, p[1].Z, n[0])),
            });

        private static readonly PrimitiveSlotUse Centre = new("Center", "C", "Centre of the shape.");
        private static readonly PrimitiveSlotUse Apex = new("Apex", "C", "The tip; the cone opens along -Y.");

        private static PrimitiveShape AxialShape(int index, string name, bool infinite, PrimitiveSlotUse?[] numbers,
            Func<(double X, double Y, double Z), double[], FieldNode> build, PrimitiveSlotUse? anchor = null)
        {
            var row = new PrimitiveSlotUse?[4];
            Array.Copy(numbers, row, numbers.Length);
            return new PrimitiveShape(index, name, infinite, new[] { anchor ?? Centre }, row, (p, n) => build(p[0], n));
        }

        public static PrimitiveFamily Axial { get; } = new PrimitiveFamily("Axial Primitive",
            new[] { "C" }, new[] { "P0", "P1", "P2", "P3" }, new[]
            {
                AxialShape(0, "Cone", false,
                    new[] { Num("Angle", "Ang", "Half-angle between axis and side, degrees", 29, angle: true), Num("Height", "H", "Height", 2) },
                    (c, n) => Field.Cone(c.X, c.Y, c.Z, n[0], n[1]), Apex),
                AxialShape(1, "CappedCone", false,
                    new[] { Num("Height", "H", "Half-height along Y", 1), Num("RadiusLow", "R0", "Radius of the lower (-Y) cap", 1),
                            Num("RadiusHigh", "R1", "Radius of the upper (+Y) cap", 0.5) },
                    (c, n) => Field.CappedCone(c.X, c.Y, c.Z, n[0], n[1], n[2])),
                AxialShape(2, "HexPrism", false,
                    new[] { Num("Radius", "R", "Hexagon apothem (centre to edge)", 1), Num("HalfLength", "HL", "Half-length along Z", 1) },
                    (c, n) => Field.HexPrism(c.X, c.Y, c.Z, n[0], n[1])),
                AxialShape(3, "TriPrism", false,
                    new[] { Num("Radius", "R", "Triangle size", 1), Num("HalfLength", "HL", "Half-length along Z", 1) },
                    (c, n) => Field.TriPrism(c.X, c.Y, c.Z, n[0], n[1])),
                AxialShape(4, "Octahedron", false,
                    new[] { Num("Size", "S", "Centre-to-vertex distance", 1) },
                    (c, n) => Field.Octahedron(c.X, c.Y, c.Z, n[0])),
                AxialShape(5, "Pyramid", false,
                    new[] { Num("Height", "H", "Apex height above the side-1 square base", 1.5) },
                    (c, n) => Field.Pyramid(c.X, c.Y, c.Z, n[0]),
                    new PrimitiveSlotUse("Base", "C", "Centre of the square base (XZ plane); the apex is up +Y.")),
                AxialShape(6, "SolidAngle", false,
                    new[] { Num("Angle", "Ang", "Half-angle of the cone opening along +Y, degrees", 40, angle: true), Num("Radius", "R", "Radius of the spherical cap", 1.5) },
                    (c, n) => Field.SolidAngle(c.X, c.Y, c.Z, n[0], n[1])),
                AxialShape(7, "CappedTorus", false,
                    new[] { Num("Angle", "Ang", "Half-angle of the arc kept (ring in the XY plane), degrees", 57, angle: true), Num("Major", "Ma", "Major radius", 1),
                            Num("Minor", "Mi", "Minor (tube) radius", 0.3) },
                    (c, n) => Field.CappedTorus(c.X, c.Y, c.Z, n[0], n[1], n[2])),
                AxialShape(8, "Link", false,
                    new[] { Num("HalfLength", "HL", "Half-length of the straight run along Y", 0.5), Num("Major", "Ma", "Major radius", 1),
                            Num("Minor", "Mi", "Minor (tube) radius", 0.3) },
                    (c, n) => Field.Link(c.X, c.Y, c.Z, n[0], n[1], n[2])),
                AxialShape(9, "CutSphere", false,
                    new[] { Num("Radius", "R", "Sphere radius", 1), Num("CutHeight", "CH", "Y of the cutting plane; the cap above it is kept", 0.3) },
                    (c, n) => Field.CutSphere(c.X, c.Y, c.Z, n[0], n[1])),
                AxialShape(10, "CutHollowSphere", false,
                    new[] { Num("Radius", "R", "Sphere radius", 1), Num("CutHeight", "CH", "Y of the cutting plane", -0.2),
                            Num("Thickness", "T", "Shell thickness", 0.1) },
                    (c, n) => Field.CutHollowSphere(c.X, c.Y, c.Z, n[0], n[1], n[2])),
                AxialShape(11, "DeathStar", false,
                    new[] { Num("RadiusMain", "RM", "Radius of the main sphere", 1), Num("RadiusBite", "RB", "Radius of the bite sphere", 0.7),
                            Num("Distance", "D", "Offset of the bite centre along +X", 0.9) },
                    (c, n) => Field.DeathStar(c.X, c.Y, c.Z, n[0], n[1], n[2])),
                AxialShape(12, "Rhombus", false,
                    new[] { Num("LengthA", "LA", "Half-diagonal along X", 1), Num("LengthB", "LB", "Half-diagonal along Z", 0.6),
                            Num("Height", "H", "Half-height along Y", 0.3), Num("CornerRadius", "CR", "Corner rounding", 0) },
                    (c, n) => Field.Rhombus(c.X, c.Y, c.Z, n[0], n[1], n[2], n[3])),
                AxialShape(13, "VerticalCapsule", false,
                    new[] { Num("Height", "H", "Length of the run up +Y", 1.5), Num("Radius", "R", "Radius", 0.4) },
                    (c, n) => Field.VerticalCapsule(c.X, c.Y, c.Z, n[0], n[1]),
                    new PrimitiveSlotUse("Base", "C", "The lower end of the axis; the capsule runs up +Y.")),
                AxialShape(14, "RoundedCylinder", false,
                    new[] { Num("Radius", "R", "Cylinder radius", 1), Num("RoundRadius", "RR", "Edge rounding", 0.2),
                            Num("HalfHeight", "HH", "Half-height along Y", 1) },
                    (c, n) => Field.RoundedCylinder(c.X, c.Y, c.Z, n[0], n[1], n[2])),
                AxialShape(15, "InfiniteCone", true,
                    new[] { Num("Angle", "Ang", "Half-angle between axis and side, degrees", 29, angle: true) },
                    (c, n) => Field.InfiniteCone(c.X, c.Y, c.Z, n[0]), Apex),
            });
    }
}
