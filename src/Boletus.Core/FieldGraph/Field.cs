using System;
using System.Collections.Generic;

namespace Boletus.Core.FieldGraph
{
    /// <summary>
    /// Typed, fluent builders for the MVP op vocabulary so a Grasshopper component (or a test)
    /// never hand-writes JSON. Each method returns an immutable <see cref="FieldNode"/>; call
    /// <see cref="FieldNode.ToJson"/> on the terminal node to get the canonical document for
    /// <c>DualcField.FromJson</c>.
    /// </summary>
    /// <remarks>
    /// Optional parameters are nullable and are emitted only when supplied — mirroring DualC's
    /// <c>--dump-json</c>, which echoes only the params present (it does not fill registry
    /// defaults). Required parameters are ordinary method arguments, so a built node always
    /// carries them.
    /// </remarks>
    public static class Field
    {
        // ---- TPMS sources -----------------------------------------------------------------

        public static FieldNode Gyroid(double? wavelength = null, (double X, double Y, double Z)? center = null) =>
            Tpms("gyroid", wavelength, center);

        public static FieldNode SchwarzP(double? wavelength = null, (double X, double Y, double Z)? center = null) =>
            Tpms("schwarz-p", wavelength, center);

        public static FieldNode Diamond(double? wavelength = null, (double X, double Y, double Z)? center = null) =>
            Tpms("diamond", wavelength, center);

        public static FieldNode FischerKoch(double? wavelength = null, (double X, double Y, double Z)? center = null) =>
            Tpms("fischer-koch", wavelength, center);

        public static FieldNode Lidinoid(double? wavelength = null, (double X, double Y, double Z)? center = null) =>
            Tpms("lidinoid", wavelength, center);

        public static FieldNode Neovius(double? wavelength = null, (double X, double Y, double Z)? center = null) =>
            Tpms("neovius", wavelength, center);

        private static FieldNode Tpms(string op, double? wavelength, (double X, double Y, double Z)? center)
        {
            var p = NewParams();
            if (center.HasValue) p["center"] = Vec3(center.Value);
            if (wavelength.HasValue) p["wavelength"] = FieldValue.Scalar(wavelength.Value);
            return new FieldNode(op, p);
        }

        // ---- Strut lattices (wireframe crystals) ------------------------------------------
        // sc/bcc/fcc/octet — periodic capsule-strut lattices. True SDFs, so (unlike TPMS) they
        // need no Normalize before a metric op. Infinite extent — clip with Intersection/a
        // boundary before contouring. `nodeRadius` omitted ⇒ uniform strut; supplied ⇒ tapered
        // (fat nodes, pinched spans) when it differs from `radius`.

        public static FieldNode Sc(double? wavelength = null, double? radius = null, double? nodeRadius = null, (double X, double Y, double Z)? center = null) =>
            Strut("sc", wavelength, radius, nodeRadius, center);

        public static FieldNode Bcc(double? wavelength = null, double? radius = null, double? nodeRadius = null, (double X, double Y, double Z)? center = null) =>
            Strut("bcc", wavelength, radius, nodeRadius, center);

        public static FieldNode Fcc(double? wavelength = null, double? radius = null, double? nodeRadius = null, (double X, double Y, double Z)? center = null) =>
            Strut("fcc", wavelength, radius, nodeRadius, center);

        public static FieldNode Octet(double? wavelength = null, double? radius = null, double? nodeRadius = null, (double X, double Y, double Z)? center = null) =>
            Strut("octet", wavelength, radius, nodeRadius, center);

        private static FieldNode Strut(string op, double? wavelength, double? radius, double? nodeRadius, (double X, double Y, double Z)? center)
        {
            var p = NewParams();
            if (center.HasValue) p["center"] = Vec3(center.Value);
            if (wavelength.HasValue) p["wavelength"] = FieldValue.Scalar(wavelength.Value);
            if (radius.HasValue) p["radius"] = FieldValue.Scalar(radius.Value);
            if (nodeRadius.HasValue) p["nodeRadius"] = FieldValue.Scalar(nodeRadius.Value);
            return new FieldNode(op, p);
        }

        // ---- Mesh sources -----------------------------------------------------------------

        /// <summary>
        /// Signed distance to a watertight mesh — either on disk (<paramref name="path"/>) or a
        /// host-provided in-memory buffer referenced by <paramref name="id"/> (DualC v0.3.0+).
        /// Supply exactly one of <paramref name="path"/> / <paramref name="id"/>.
        /// </summary>
        public static FieldNode Mesh(string? path = null, string? sign = null, string? normals = null, string? id = null)
        {
            RequireExactlyOne(path, id);
            var p = NewParams();
            if (path != null) p["path"] = FieldValue.Text(path);
            if (id != null) p["id"] = FieldValue.Text(id);
            if (sign != null) p["sign"] = FieldValue.Text(sign);
            if (normals != null) p["normals"] = FieldValue.Text(normals);
            return new FieldNode("mesh", p);
        }

        /// <summary>
        /// Generalized-winding-number field over triangle soup / open shells — either on disk
        /// (<paramref name="path"/>) or an in-memory buffer by <paramref name="id"/> (v0.3.0+).
        /// Supply exactly one of <paramref name="path"/> / <paramref name="id"/>.
        /// </summary>
        public static FieldNode Winding(string? path = null, string? id = null)
        {
            RequireExactlyOne(path, id);
            var p = NewParams();
            if (path != null) p["path"] = FieldValue.Text(path);
            if (id != null) p["id"] = FieldValue.Text(id);
            return new FieldNode("winding", p);
        }

        private static void RequireExactlyOne(string? path, string? id)
        {
            if ((path is null) == (id is null))
                throw new ArgumentException("Supply exactly one of 'path' or 'id' for a mesh/winding source.");
        }

        // ---- Analytic primitives ----------------------------------------------------------

        public static FieldNode Sphere(double? radius = null, (double X, double Y, double Z)? center = null)
        {
            var p = NewParams();
            if (center.HasValue) p["center"] = Vec3(center.Value);
            if (radius.HasValue) p["radius"] = FieldValue.Scalar(radius.Value);
            return new FieldNode("sphere", p);
        }

        public static FieldNode Box((double X, double Y, double Z)? min = null, (double X, double Y, double Z)? max = null)
        {
            var p = NewParams();
            if (min.HasValue) p["min"] = Vec3(min.Value);
            if (max.HasValue) p["max"] = Vec3(max.Value);
            return new FieldNode("box", p);
        }

        // -- Grouped-key primitives (named vector keys, like box/sphere) --------------------

        public static FieldNode RoundBox((double X, double Y, double Z)? min = null, (double X, double Y, double Z)? max = null, double? radius = null)
        {
            var p = NewParams();
            if (min.HasValue) p["min"] = Vec3(min.Value);
            if (max.HasValue) p["max"] = Vec3(max.Value);
            if (radius.HasValue) p["radius"] = FieldValue.Scalar(radius.Value);
            return new FieldNode("roundbox", p);
        }

        public static FieldNode Capsule((double X, double Y, double Z)? a = null, (double X, double Y, double Z)? b = null, double? radius = null) =>
            TwoPointRadius("capsule", a, b, radius);

        public static FieldNode CappedCylinder((double X, double Y, double Z)? a = null, (double X, double Y, double Z)? b = null, double? radius = null) =>
            TwoPointRadius("cappedcylinder", a, b, radius);

        public static FieldNode Torus((double X, double Y, double Z)? center = null, double? major = null, double? minor = null)
        {
            var p = NewParams();
            if (center.HasValue) p["center"] = Vec3(center.Value);
            if (major.HasValue) p["major"] = FieldValue.Scalar(major.Value);
            if (minor.HasValue) p["minor"] = FieldValue.Scalar(minor.Value);
            return new FieldNode("torus", p);
        }

        public static FieldNode Ellipsoid((double X, double Y, double Z)? center = null, (double X, double Y, double Z)? radii = null)
        {
            var p = NewParams();
            if (center.HasValue) p["center"] = Vec3(center.Value);
            if (radii.HasValue) p["radii"] = Vec3(radii.Value);
            return new FieldNode("ellipsoid", p);
        }

        private static FieldNode TwoPointRadius(string op, (double X, double Y, double Z)? a, (double X, double Y, double Z)? b, double? radius)
        {
            var p = NewParams();
            if (a.HasValue) p["a"] = Vec3(a.Value);
            if (b.HasValue) p["b"] = Vec3(b.Value);
            if (radius.HasValue) p["radius"] = FieldValue.Scalar(radius.Value);
            return new FieldNode(op, p);
        }

        // -- Flat-params primitives (universal positional params=[…]) ----------------------
        // Each builder names the positional parameters for call-site clarity, then packs them
        // into the flat `params` array DualC canonicalises to. Order pinned from
        // 02-dualc_primitive.md / `dualc_field --list`. plane/infinitecylinder/infinitecone are
        // unbounded (need a contour-time --bounds); triangle/quad are open surfaces (need onion).

        public static FieldNode Plane(double nx, double ny, double nz, double offset) =>
            Flat("plane", nx, ny, nz, offset);

        public static FieldNode BoxFrame(double minX, double minY, double minZ, double maxX, double maxY, double maxZ, double edge) =>
            Flat("boxframe", minX, minY, minZ, maxX, maxY, maxZ, edge);

        public static FieldNode Cone(double cx, double cy, double cz, double angleRad, double height) =>
            Flat("cone", cx, cy, cz, angleRad, height);

        public static FieldNode CappedCone(double cx, double cy, double cz, double height, double radiusLow, double radiusHigh) =>
            Flat("cappedcone", cx, cy, cz, height, radiusLow, radiusHigh);

        public static FieldNode RoundCone(double ax, double ay, double az, double bx, double by, double bz, double radiusA, double radiusB) =>
            Flat("roundcone", ax, ay, az, bx, by, bz, radiusA, radiusB);

        public static FieldNode InfiniteCylinder(double px, double py, double pz, double dx, double dy, double dz, double radius) =>
            Flat("infinitecylinder", px, py, pz, dx, dy, dz, radius);

        public static FieldNode HexPrism(double cx, double cy, double cz, double radius, double halfLength) =>
            Flat("hexprism", cx, cy, cz, radius, halfLength);

        public static FieldNode TriPrism(double cx, double cy, double cz, double radius, double halfLength) =>
            Flat("triprism", cx, cy, cz, radius, halfLength);

        public static FieldNode Octahedron(double cx, double cy, double cz, double size) =>
            Flat("octahedron", cx, cy, cz, size);

        public static FieldNode Pyramid(double cx, double cy, double cz, double height) =>
            Flat("pyramid", cx, cy, cz, height);

        public static FieldNode SolidAngle(double cx, double cy, double cz, double angleRad, double radius) =>
            Flat("solidangle", cx, cy, cz, angleRad, radius);

        public static FieldNode CappedTorus(double cx, double cy, double cz, double angleRad, double major, double minor) =>
            Flat("cappedtorus", cx, cy, cz, angleRad, major, minor);

        public static FieldNode Link(double cx, double cy, double cz, double halfLength, double major, double minor) =>
            Flat("link", cx, cy, cz, halfLength, major, minor);

        public static FieldNode CutSphere(double cx, double cy, double cz, double radius, double cutHeight) =>
            Flat("cutsphere", cx, cy, cz, radius, cutHeight);

        public static FieldNode CutHollowSphere(double cx, double cy, double cz, double radius, double cutHeight, double thickness) =>
            Flat("cuthollowsphere", cx, cy, cz, radius, cutHeight, thickness);

        public static FieldNode DeathStar(double cx, double cy, double cz, double radiusMain, double radiusBite, double distance) =>
            Flat("deathstar", cx, cy, cz, radiusMain, radiusBite, distance);

        public static FieldNode Vesica(double ax, double ay, double az, double bx, double by, double bz, double width) =>
            Flat("vesica", ax, ay, az, bx, by, bz, width);

        public static FieldNode Rhombus(double cx, double cy, double cz, double la, double lb, double height, double cornerRadius) =>
            Flat("rhombus", cx, cy, cz, la, lb, height, cornerRadius);

        public static FieldNode VerticalCapsule(double cx, double cy, double cz, double height, double radius) =>
            Flat("verticalcapsule", cx, cy, cz, height, radius);

        public static FieldNode RoundedCylinder(double cx, double cy, double cz, double radius, double roundRadius, double halfHeight) =>
            Flat("roundedcylinder", cx, cy, cz, radius, roundRadius, halfHeight);

        public static FieldNode Triangle(double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz) =>
            Flat("triangle", ax, ay, az, bx, by, bz, cx, cy, cz);

        public static FieldNode Quad(double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz, double dx, double dy, double dz) =>
            Flat("quad", ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz);

        public static FieldNode InfiniteCone(double cx, double cy, double cz, double angleRad) =>
            Flat("infinitecone", cx, cy, cz, angleRad);

        // ---- Booleans (two children) ------------------------------------------------------

        public static FieldNode Union(FieldNode a, FieldNode b) => Boolean2("union", a, b);
        public static FieldNode Intersection(FieldNode a, FieldNode b) => Boolean2("intersection", a, b);
        public static FieldNode Difference(FieldNode a, FieldNode b) => Boolean2("difference", a, b);
        public static FieldNode Xor(FieldNode a, FieldNode b) => Boolean2("xor", a, b);

        public static FieldNode SmoothUnion(FieldNode a, FieldNode b, double? k = null) => Smooth("smooth-union", a, b, k);
        public static FieldNode SmoothIntersection(FieldNode a, FieldNode b, double? k = null) => Smooth("smooth-intersection", a, b, k);
        public static FieldNode SmoothDifference(FieldNode a, FieldNode b, double? k = null) => Smooth("smooth-difference", a, b, k);

        private static FieldNode Boolean2(string op, FieldNode a, FieldNode b)
        {
            RequireChild(a, nameof(a));
            RequireChild(b, nameof(b));
            return new FieldNode(op, null, new[] { a, b });
        }

        private static FieldNode Smooth(string op, FieldNode a, FieldNode b, double? k)
        {
            RequireChild(a, nameof(a));
            RequireChild(b, nameof(b));
            var p = NewParams();
            if (k.HasValue) p["k"] = FieldValue.Scalar(k.Value);
            return new FieldNode(op, p, new[] { a, b });
        }

        // ---- Decorators / domain ops (one child) ------------------------------------------

        /// <summary>Rescale a non-metric field (raw TPMS / winding) toward unit gradient.</summary>
        public static FieldNode Normalize(FieldNode child)
        {
            RequireChild(child, nameof(child));
            return new FieldNode("normalize", null, new[] { child });
        }

        /// <summary>Hollow shell of wall ≈ 2·<paramref name="thickness"/> (needs a metric child).</summary>
        public static FieldNode Onion(FieldNode child, double thickness)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p["thickness"] = FieldValue.Scalar(thickness);
            return new FieldNode("onion", p, new[] { child });
        }

        public static FieldNode Offset(FieldNode child, double r) => OneScalar("offset", "r", child, r);
        public static FieldNode Round(FieldNode child, double r) => OneScalar("round", "r", child, r);
        public static FieldNode Scale(FieldNode child, double s) => OneScalar("scale", "s", child, s);

        public static FieldNode Translate(FieldNode child, (double X, double Y, double Z) by)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p["by"] = Vec3(by);
            return new FieldNode("translate", p, new[] { child });
        }

        public static FieldNode Rotate(FieldNode child, (double X, double Y, double Z) axis, double degrees)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p["axis"] = Vec3(axis);
            p["degrees"] = FieldValue.Scalar(degrees);
            return new FieldNode("rotate", p, new[] { child });
        }

        private static FieldNode OneScalar(string op, string key, FieldNode child, double value)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p[key] = FieldValue.Scalar(value);
            return new FieldNode(op, p, new[] { child });
        }

        // -- Domain operators (one child) --------------------------------------------------

        /// <summary>Stretch a field by a slab per axis (<paramref name="h"/> = [x,y,z]).</summary>
        public static FieldNode Elongate(FieldNode child, (double X, double Y, double Z) h)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p["h"] = Vec3(h);
            return new FieldNode("elongate", p, new[] { child });
        }

        /// <summary>Apply a raw row-major 4×4 transform (16 floats) to a field.</summary>
        public static FieldNode Transform(FieldNode child, IReadOnlyList<double> matrix)
        {
            RequireChild(child, nameof(child));
            if (matrix is null) throw new ArgumentNullException(nameof(matrix));
            if (matrix.Count != 16) throw new ArgumentException("transform matrix must be 16 row-major floats.", nameof(matrix));
            var p = NewParams();
            p["matrix"] = FieldValue.Vector(matrix);
            return new FieldNode("transform", p, new[] { child });
        }

        /// <summary>Twist about an axis (<paramref name="axis"/> = "x" | "y" | "z" — a string, not a vector).</summary>
        public static FieldNode Twist(FieldNode child, double radiansPerUnit, string axis) =>
            AxisOp("twist", "radiansPerUnit", radiansPerUnit, child, axis);

        /// <summary>Bend about an axis (<paramref name="axis"/> = "x" | "y" | "z").</summary>
        public static FieldNode Bend(FieldNode child, double curvature, string axis) =>
            AxisOp("bend", "curvature", curvature, child, axis);

        /// <summary>Mirror across a plane through the origin with the given <paramref name="normal"/>.</summary>
        public static FieldNode Mirror(FieldNode child, (double X, double Y, double Z) normal)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p["normal"] = Vec3(normal);
            return new FieldNode("mirror", p, new[] { child });
        }

        /// <summary>Infinite tiling by <paramref name="period"/> — needs a contour-time --bounds.</summary>
        public static FieldNode Repeat(FieldNode child, (double X, double Y, double Z) period)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p["period"] = Vec3(period);
            return new FieldNode("repeat", p, new[] { child });
        }

        /// <summary>Finite tiling: <paramref name="period"/> spacing × integer <paramref name="count"/> per axis.</summary>
        public static FieldNode RepeatLimited(FieldNode child, (double X, double Y, double Z) period, (double X, double Y, double Z) count)
        {
            RequireChild(child, nameof(child));
            var p = NewParams();
            p["period"] = Vec3(period);
            p["count"] = Vec3(count);
            return new FieldNode("repeat-limited", p, new[] { child });
        }

        /// <summary>Analytic surface bump (<paramref name="fn"/> = "sine" | "gyroid" | "bumps").</summary>
        public static FieldNode Displace(FieldNode child, string fn, double? amplitude = null, double? frequency = null)
        {
            RequireChild(child, nameof(child));
            if (fn is null) throw new ArgumentNullException(nameof(fn));
            var p = NewParams();
            p["fn"] = FieldValue.Text(fn);
            if (amplitude.HasValue) p["amplitude"] = FieldValue.Scalar(amplitude.Value);
            if (frequency.HasValue) p["frequency"] = FieldValue.Scalar(frequency.Value);
            return new FieldNode("displace", p, new[] { child });
        }

        private static FieldNode AxisOp(string op, string scalarKey, double scalar, FieldNode child, string axis)
        {
            RequireChild(child, nameof(child));
            if (axis is null) throw new ArgumentNullException(nameof(axis));
            var p = NewParams();
            p[scalarKey] = FieldValue.Scalar(scalar);
            p["axis"] = FieldValue.Text(axis);
            return new FieldNode(op, p, new[] { child });
        }

        // ---- Graded shell (two children: base, control) -----------------------------------

        /// <summary>
        /// Hollow shell whose wall thickness ramps with the <paramref name="control"/> field:
        /// <c>t1</c> where control ≤ <c>d0</c>, <c>t2</c> where control ≥ <c>d1</c>.
        /// </summary>
        public static FieldNode GradedOnion(FieldNode baseField, FieldNode control, double t1, double t2, double d1, double? d0 = null)
        {
            RequireChild(baseField, nameof(baseField));
            RequireChild(control, nameof(control));
            var p = NewParams();
            p["t1"] = FieldValue.Scalar(t1);
            p["t2"] = FieldValue.Scalar(t2);
            p["d1"] = FieldValue.Scalar(d1);
            if (d0.HasValue) p["d0"] = FieldValue.Scalar(d0.Value);
            return new FieldNode("graded-onion", p, new[] { baseField, control });
        }

        /// <summary>
        /// Inflate a <b>solid</b> <paramref name="baseField"/> by an offset that ramps with the
        /// <paramref name="control"/> field: <c>t1</c> where control ≤ <c>d0</c>, <c>t2</c> where
        /// control ≥ <c>d1</c>. Use this (not <see cref="GradedOnion"/>) to grade a strut lattice's
        /// radius across space — onion would hollow the struts into tubes.
        /// </summary>
        public static FieldNode GradedOffset(FieldNode baseField, FieldNode control, double t1, double t2, double d1, double? d0 = null)
        {
            RequireChild(baseField, nameof(baseField));
            RequireChild(control, nameof(control));
            var p = NewParams();
            p["t1"] = FieldValue.Scalar(t1);
            p["t2"] = FieldValue.Scalar(t2);
            p["d1"] = FieldValue.Scalar(d1);
            if (d0.HasValue) p["d0"] = FieldValue.Scalar(d0.Value);
            return new FieldNode("graded-offset", p, new[] { baseField, control });
        }

        // ---- Value-lerp morph (three children: A, B, control) -----------------------------

        /// <summary>
        /// Value-lerp morph: <c>value = lerp(A, B, w)</c> where
        /// <c>w = clamp((control − lo)/(hi − lo))</c>. Blends field <b>values, not shapes</b> — only
        /// fluid/watertight when <paramref name="a"/> and <paramref name="b"/> share the same lattice
        /// geometry (same crystal family, differing only in <c>radius</c>); a cross-family blend tapers
        /// to nothing at the mid-plane and splits into two bodies (use <c>smooth-union</c> instead).
        /// </summary>
        public static FieldNode Mix(FieldNode a, FieldNode b, FieldNode control, double hi, double? lo = null)
        {
            RequireChild(a, nameof(a));
            RequireChild(b, nameof(b));
            RequireChild(control, nameof(control));
            var p = NewParams();
            p["hi"] = FieldValue.Scalar(hi);
            if (lo.HasValue) p["lo"] = FieldValue.Scalar(lo.Value);
            return new FieldNode("mix", p, new[] { a, b, control });
        }

        // ---- helpers ----------------------------------------------------------------------

        private static Dictionary<string, FieldValue> NewParams() =>
            new Dictionary<string, FieldValue>(StringComparer.Ordinal);

        private static FieldValue Vec3((double X, double Y, double Z) v) =>
            FieldValue.Vector(v.X, v.Y, v.Z);

        /// <summary>Pack positional values into a source node's universal flat <c>params</c> array.</summary>
        private static FieldNode Flat(string op, params double[] values)
        {
            var p = NewParams();
            p["params"] = FieldValue.Vector(values);
            return new FieldNode(op, p);
        }

        private static void RequireChild(FieldNode child, string name)
        {
            if (child is null) throw new ArgumentNullException(name);
        }
    }
}
