using System;
using System.Collections.Generic;

namespace Boletus.Core.FieldGraph
{
    /// <summary>
    /// The pinned op vocabulary — the full ~60-op set. Maps a canonical op token to its
    /// <see cref="OpSchema"/>. Every op (token, params, arity, grouped-vs-flat) is gated by a
    /// <c>--dump-json</c> diff against DualC (roadmap Phase 3a). Sourced from
    /// <c>11-dualc_field/01-op-vocabulary.md</c>, <c>02-dualc_primitive.md</c>, and <c>dualc_field --list</c>.
    /// </summary>
    public static class Ops
    {
        // Parameter-spec shorthands.
        private static ParamSpec ScalarReq(string n) => new ParamSpec(n, ParamKind.Scalar, required: true);
        private static ParamSpec ScalarOpt(string n) => new ParamSpec(n, ParamKind.Scalar, required: false);
        private static ParamSpec Vec3Req(string n) => new ParamSpec(n, ParamKind.Vector, required: true, vectorLength: 3);
        private static ParamSpec Vec3Opt(string n) => new ParamSpec(n, ParamKind.Vector, required: false, vectorLength: 3);
        private static ParamSpec TextReq(string n) => new ParamSpec(n, ParamKind.Text, required: true);
        private static ParamSpec TextOpt(string n) => new ParamSpec(n, ParamKind.Text, required: false);
        private static ParamSpec VecReq(string n, int len) => new ParamSpec(n, ParamKind.Vector, required: true, vectorLength: len);
        // Universal flat positional array — every primitive accepts params=[…] mapping onto the
        // documented order (02-dualc_primitive.md). Optional: DualC fills trailing defaults.
        private static ParamSpec FlatParams(int n) => new ParamSpec("params", ParamKind.Vector, required: false, vectorLength: n);

        private static readonly Dictionary<string, OpSchema> Registry = BuildRegistry();

        /// <summary>Look up the schema for an op token; returns false for an unknown op.</summary>
        public static bool TryGet(string token, out OpSchema schema) => Registry.TryGetValue(token, out schema!);

        /// <summary>All registered op tokens (for diagnostics / discovery).</summary>
        public static IReadOnlyCollection<string> Tokens => Registry.Keys;

        private static Dictionary<string, OpSchema> BuildRegistry()
        {
            var r = new Dictionary<string, OpSchema>(StringComparer.Ordinal);

            void Add(OpSchema s) => r[s.Token] = s;

            // ---- TPMS sources (non-metric; center=[x,y,z], wavelength) -------------------
            foreach (var t in new[] { "gyroid", "schwarz-p", "diamond", "fischer-koch", "lidinoid", "neovius" })
                Add(new OpSchema(t, 0, OpCategory.Tpms, Vec3Opt("center"), ScalarOpt("wavelength")));

            // ---- Strut lattices (wireframe crystals) ------------------------------------
            // sc/bcc/fcc/octet: periodic capsule-strut lattices. True SDFs (a union of exact
            // capsules / round-cones) → MetricSource, NOT Tpms: no `normalize` needed, unlike the
            // TPMS surfaces above. Same four params for every crystal; `nodeRadius` optional
            // (omitted ⇒ uniform strut, present ⇒ tapered). Infinite extent — the graph must clip
            // them (intersection with a box/mesh) or set contour bounds. (DualC's
            // 11-dualc_field/02-strut-lattices.md; its roadmap 05 #17. Requires a DLL ≥ d6b2808 — the parser lives in the DLL.)
            foreach (var t in new[] { "sc", "bcc", "fcc", "octet" })
                Add(new OpSchema(t, 0, OpCategory.MetricSource,
                    Vec3Opt("center"), ScalarOpt("wavelength"), ScalarOpt("radius"), ScalarOpt("nodeRadius")));

            // ---- Mesh sources -----------------------------------------------------------
            // mesh: exactly one of path / id (disk vs. v0.3.0 in-memory buffer — enforced by the
            // validator, not the schema); sign ∈ parity|pseudonormal; normals ∈ smooth|sharp.
            Add(new OpSchema("mesh", 0, OpCategory.MetricSource, TextOpt("path"), TextOpt("id"), TextOpt("sign"), TextOpt("normals")));
            // winding: exactly one of path / id; non-metric (generalized winding number).
            Add(new OpSchema("winding", 0, OpCategory.Winding, TextOpt("path"), TextOpt("id")));

            // ---- Analytic primitives: grouped semantic keys -----------------------------
            // Every primitive param has a DualC default, so all are optional (matches sphere/box;
            // DualC accepts a bare `roundbox()`). Categorised MetricSource (true/unsigned SDF).
            Add(new OpSchema("sphere", 0, OpCategory.MetricSource, Vec3Opt("center"), ScalarOpt("radius")));
            Add(new OpSchema("box", 0, OpCategory.MetricSource, Vec3Opt("min"), Vec3Opt("max")));
            Add(new OpSchema("roundbox", 0, OpCategory.MetricSource, Vec3Opt("min"), Vec3Opt("max"), ScalarOpt("radius")));
            Add(new OpSchema("capsule", 0, OpCategory.MetricSource, Vec3Opt("a"), Vec3Opt("b"), ScalarOpt("radius")));
            Add(new OpSchema("cappedcylinder", 0, OpCategory.MetricSource, Vec3Opt("a"), Vec3Opt("b"), ScalarOpt("radius")));
            Add(new OpSchema("torus", 0, OpCategory.MetricSource, Vec3Opt("center"), ScalarOpt("major"), ScalarOpt("minor")));
            Add(new OpSchema("ellipsoid", 0, OpCategory.MetricSource, Vec3Opt("center"), Vec3Opt("radii")));

            // ---- Analytic primitives: universal flat params=[…] (positional count pinned) -
            // (token, element count) from `dualc_field --list` / 02-dualc_primitive.md.
            var flat = new (string token, int n)[]
            {
                ("plane", 4), ("boxframe", 7), ("cone", 5), ("cappedcone", 6), ("roundcone", 8),
                ("infinitecylinder", 7), ("hexprism", 5), ("triprism", 5), ("octahedron", 4),
                ("pyramid", 4), ("solidangle", 5), ("cappedtorus", 6), ("link", 6), ("cutsphere", 5),
                ("cuthollowsphere", 6), ("deathstar", 6), ("vesica", 7), ("rhombus", 7),
                ("verticalcapsule", 5), ("roundedcylinder", 6), ("triangle", 9), ("quad", 12),
                ("infinitecone", 4),
            };
            // All categorised MetricSource. NB: triangle/quad are *unsigned* distance (open
            // surfaces, |∇|=1 but no inside) — treating them as metric means the validator won't
            // warn when they feed onion, which is correct since onion is exactly how they gain
            // thickness. A judgment call, not confirmed against DualC's own metric-ness view.
            foreach (var (token, n) in flat)
                Add(new OpSchema(token, 0, OpCategory.MetricSource, FlatParams(n)));

            // ---- Booleans (two children) ------------------------------------------------
            foreach (var t in new[] { "union", "intersection", "difference", "xor" })
                Add(new OpSchema(t, 2, OpCategory.HardBoolean));
            foreach (var t in new[] { "smooth-union", "smooth-intersection", "smooth-difference" })
                Add(new OpSchema(t, 2, OpCategory.SmoothBoolean, ScalarOpt("k"))); // k default 0.25

            // ---- Decorators / domain ops (one child) ------------------------------------
            Add(new OpSchema("normalize", 1, OpCategory.Normalize));
            Add(new OpSchema("onion", 1, OpCategory.Onion, ScalarReq("thickness")));
            Add(new OpSchema("offset", 1, OpCategory.Decorator, ScalarReq("r")));
            Add(new OpSchema("round", 1, OpCategory.Decorator, ScalarReq("r"))); // alias of offset
            Add(new OpSchema("scale", 1, OpCategory.Decorator, ScalarOpt("s")));  // s default 1
            Add(new OpSchema("translate", 1, OpCategory.Decorator, Vec3Req("by")));
            Add(new OpSchema("rotate", 1, OpCategory.Decorator, Vec3Req("axis"), ScalarReq("degrees")));
            Add(new OpSchema("elongate", 1, OpCategory.Decorator, Vec3Req("h")));
            Add(new OpSchema("transform", 1, OpCategory.Decorator, VecReq("matrix", 16))); // row-major 4×4
            // NB: twist/bend `axis` is a STRING ("x"/"y"/"z") here — unlike rotate's vector axis.
            Add(new OpSchema("twist", 1, OpCategory.Decorator, ScalarReq("radiansPerUnit"), TextReq("axis")));
            Add(new OpSchema("bend", 1, OpCategory.Decorator, ScalarReq("curvature"), TextReq("axis")));
            Add(new OpSchema("mirror", 1, OpCategory.Decorator, Vec3Req("normal")));
            Add(new OpSchema("repeat", 1, OpCategory.Decorator, Vec3Req("period")));            // infinite → needs --bounds
            Add(new OpSchema("repeat-limited", 1, OpCategory.Decorator, Vec3Req("period"), Vec3Req("count")));
            Add(new OpSchema("displace", 1, OpCategory.Decorator, TextReq("fn"), ScalarOpt("amplitude"), ScalarOpt("frequency")));

            // ---- Graded shell / graded inflation (two children: base, control) ----------
            // t1, t2, d1 required; d0 default 0.
            Add(new OpSchema("graded-onion", 2, OpCategory.GradedOnion,
                ScalarReq("t1"), ScalarReq("t2"), ScalarReq("d1"), ScalarOpt("d0")));
            // graded-offset inflates a SOLID by a control-driven amount (grades a strut radius
            // across space) — same param shape as graded-onion. Category GradedOnion is REUSED
            // deliberately: the validator's only rule for it is "the base (first) child must be
            // metric", which is exactly right here (the base is a strut lattice); the "onion" in
            // the enum name is incidental. It is NOT a hollow shell.
            Add(new OpSchema("graded-offset", 2, OpCategory.GradedOnion,
                ScalarReq("t1"), ScalarReq("t2"), ScalarReq("d1"), ScalarOpt("d0")));

            // ---- Value-lerp morph (three children: A, B, control) -----------------------
            // mix(A, B, control; lo=0, hi): value = lerp(A, B, clamp((control-lo)/(hi-lo))). The
            // first arity-3 op. Category HardBoolean is REUSED deliberately: its IsMetric checks
            // children[0] && children[1] (A and B) and imposes no metric-input requirement — the
            // correct metric-ness semantics for a value blend (the control child is ignored). It
            // is NOT a CSG boolean; the enum name is incidental.
            Add(new OpSchema("mix", 3, OpCategory.HardBoolean, ScalarReq("hi"), ScalarOpt("lo")));

            return r;
        }
    }
}
