using System;
using System.Collections.Generic;
using System.Linq;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// The catalog behind the Segment / Axial Primitive components (D-25). Each shape is built from
    /// explicit inputs and compared with the <see cref="Field"/> builder it must call — this pins the
    /// slot → positional-parameter order and the degree → radian conversion. Then every shape at its
    /// defaults is contoured through the native library, so a default the engine rejects or that
    /// yields nothing fails here, not on the canvas.
    /// </summary>
    public class PrimitiveCatalogTests
    {
        private static readonly (double, double, double) A = (0.1, -1.2, 0.3);
        private static readonly (double, double, double) B = (-0.2, 0.9, 0.4);
        private static readonly (double, double, double) C = (0.5, 0.25, -0.75);
        private static double Rad(double deg) => deg * Math.PI / 180.0;

        public static IEnumerable<object[]> SegmentCases()
        {
            yield return Seg(0, 0.4, null, Field.Capsule(A, B, 0.4));
            yield return Seg(1, 0.4, null, Field.CappedCylinder(A, B, 0.4));
            yield return Seg(2, 0.5, 0.2, Field.RoundCone(0.1, -1.2, 0.3, -0.2, 0.9, 0.4, 0.5, 0.2));
            yield return Seg(3, 0.3, null, Field.Vesica(0.1, -1.2, 0.3, -0.2, 0.9, 0.4, 0.3));
            yield return Seg(4, 0.7, null, Field.InfiniteCylinder(0.1, -1.2, 0.3, -0.2, 0.9, 0.4, 0.7));
        }

        public static IEnumerable<object[]> AxialCases()
        {
            yield return Ax(0, new double?[] { 40, 1.5 }, Field.Cone(0.5, 0.25, -0.75, Rad(40), 1.5));
            yield return Ax(1, new double?[] { 0.8, 0.9, 0.4 }, Field.CappedCone(0.5, 0.25, -0.75, 0.8, 0.9, 0.4));
            yield return Ax(2, new double?[] { 0.7, 0.6 }, Field.HexPrism(0.5, 0.25, -0.75, 0.7, 0.6));
            yield return Ax(3, new double?[] { 0.7, 0.6 }, Field.TriPrism(0.5, 0.25, -0.75, 0.7, 0.6));
            yield return Ax(4, new double?[] { 0.9 }, Field.Octahedron(0.5, 0.25, -0.75, 0.9));
            yield return Ax(5, new double?[] { 1.1 }, Field.Pyramid(0.5, 0.25, -0.75, 1.1));
            yield return Ax(6, new double?[] { 35, 1.2 }, Field.SolidAngle(0.5, 0.25, -0.75, Rad(35), 1.2));
            yield return Ax(7, new double?[] { 90, 0.9, 0.2 }, Field.CappedTorus(0.5, 0.25, -0.75, Rad(90), 0.9, 0.2));
            yield return Ax(8, new double?[] { 0.4, 0.8, 0.2 }, Field.Link(0.5, 0.25, -0.75, 0.4, 0.8, 0.2));
            yield return Ax(9, new double?[] { 0.9, 0.1 }, Field.CutSphere(0.5, 0.25, -0.75, 0.9, 0.1));
            yield return Ax(10, new double?[] { 0.9, -0.1, 0.05 }, Field.CutHollowSphere(0.5, 0.25, -0.75, 0.9, -0.1, 0.05));
            yield return Ax(11, new double?[] { 0.9, 0.6, 0.8 }, Field.DeathStar(0.5, 0.25, -0.75, 0.9, 0.6, 0.8));
            yield return Ax(12, new double?[] { 0.9, 0.5, 0.2, 0.05 }, Field.Rhombus(0.5, 0.25, -0.75, 0.9, 0.5, 0.2, 0.05));
            yield return Ax(13, new double?[] { 1.2, 0.3 }, Field.VerticalCapsule(0.5, 0.25, -0.75, 1.2, 0.3));
            yield return Ax(14, new double?[] { 0.9, 0.1, 0.8 }, Field.RoundedCylinder(0.5, 0.25, -0.75, 0.9, 0.1, 0.8));
            yield return Ax(15, new double?[] { 25 }, Field.InfiniteCone(0.5, 0.25, -0.75, Rad(25)));
        }

        private static object[] Seg(int type, double r0, double? r1, FieldNode expected) =>
            new object[] { type, r0, r1!, expected };

        private static object[] Ax(int type, double?[] numbers, FieldNode expected) =>
            new object[] { type, numbers, expected };

        [Theory]
        [MemberData(nameof(SegmentCases))]
        public void Segment_shape_calls_its_builder(int type, double r0, double? r1, FieldNode expected)
        {
            var shape = PrimitiveCatalog.Segment.Find(type)!;
            var node = shape.Build(new[] { A, B }, new[] { (double?)r0, r1 });
            Assert.Equal(expected.ToJson(), node.ToJson());
        }

        [Theory]
        [MemberData(nameof(AxialCases))]
        public void Axial_shape_calls_its_builder(int type, double?[] numbers, FieldNode expected)
        {
            var shape = PrimitiveCatalog.Axial.Find(type)!;
            var row = new double?[4];
            Array.Copy(numbers, row, numbers.Length);
            var node = shape.Build(new[] { C }, row);
            Assert.Equal(expected.ToJson(), node.ToJson());
        }

        public static IEnumerable<object[]> AllShapes() =>
            new[] { PrimitiveCatalog.Segment, PrimitiveCatalog.Axial }
                .SelectMany(f => f.Shapes.Select(s => new object[] { f.Name, s.Index }));

        private static PrimitiveFamily Family(string name) =>
            name == PrimitiveCatalog.Segment.Name ? PrimitiveCatalog.Segment : PrimitiveCatalog.Axial;

        [Theory]
        [MemberData(nameof(AllShapes))]
        public void Shape_at_its_defaults_contours_to_a_nonempty_mesh(string family, int index)
        {
            var f = Family(family);
            var shape = f.Find(index)!;
            var points = f == PrimitiveCatalog.Segment
                ? new[] { (0.0, -1.0, 0.0), (0.0, 1.0, 0.0) }   // the component's point defaults
                : new[] { (0.0, 0.0, 0.0) };
            var node = shape.Build(points, new double?[f.NumberSlots.Count]);

            var p = DualcContourParams.Default();
            p.MaxDepth = 5;
            if (shape.IsInfinite)
            {
                p.HasBounds = true;
                p.BoundsMin = (-2, -2, -2);
                p.BoundsMax = (2, 2, 2);
            }

            using var field = DualcField.FromJson(node.ToJson());
            var mesh = field.Contour(p);
            Assert.True(mesh.TriangleCount > 0, $"{shape.Name} contoured to nothing at its defaults.");
        }

        [Fact]
        public void An_empty_number_takes_the_shape_default()
        {
            var cone = PrimitiveCatalog.Axial.Find(0)!;
            var node = cone.Build(new[] { C }, new double?[4]);
            Assert.Equal(Field.Cone(0.5, 0.25, -0.75, Rad(cone.Numbers[0]!.Default), cone.Numbers[1]!.Default).ToJson(), node.ToJson());
        }

        [Fact]
        public void Indices_are_positions_and_names_are_unique()
        {
            foreach (var f in new[] { PrimitiveCatalog.Segment, PrimitiveCatalog.Axial })
            {
                for (int i = 0; i < f.Shapes.Count; i++)
                {
                    Assert.Equal(i, f.Shapes[i].Index);
                    Assert.Equal(f.PointSlots.Count, f.Shapes[i].Points.Count);
                    Assert.Equal(f.NumberSlots.Count, f.Shapes[i].Numbers.Count);
                }
                Assert.Equal(f.Shapes.Count, f.Shapes.Select(s => s.Name).Distinct().Count());
                Assert.Null(f.Find(f.Shapes.Count));
                Assert.Null(f.Find(-1));
            }
            Assert.Equal(5, PrimitiveCatalog.Segment.Shapes.Count);
            Assert.Equal(16, PrimitiveCatalog.Axial.Shapes.Count);
        }

        [Fact]
        public void Only_the_unbounded_shapes_are_flagged_infinite()
        {
            var infinite = new[] { PrimitiveCatalog.Segment, PrimitiveCatalog.Axial }
                .SelectMany(f => f.Shapes).Where(s => s.IsInfinite).Select(s => s.Name).OrderBy(n => n);
            Assert.Equal(new[] { "InfiniteCone", "InfiniteCylinder" }, infinite);
        }
    }
}
