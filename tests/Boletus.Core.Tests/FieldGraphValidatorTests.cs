using System.Collections.Generic;
using System.Linq;
using Boletus.Core.FieldGraph;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Validator tiers: schema lookups (arity / required / unknown params) and the metric-ness
    /// tree-walk (non-metric source feeding a metric op without <c>normalize</c>). Pure managed
    /// — no CLI / native dependency.
    /// </summary>
    public class FieldGraphValidatorTests
    {
        private static IReadOnlyList<ValidationIssue> Validate(FieldNode n) => FieldGraphValidator.Validate(n);

        // ---- Happy paths ------------------------------------------------------------------

        [Fact]
        public void Normalized_gyroid_lattice_clip_is_valid()
        {
            var graph = Field.Intersection(
                Field.Onion(Field.Normalize(Field.Gyroid(wavelength: 0.5)), thickness: 0.1),
                Field.Box(min: (-1, -1, -1), max: (1, 1, 1)));

            Assert.Empty(Validate(graph));
        }

        [Fact]
        public void Mesh_into_onion_is_metric_and_valid()
        {
            // mesh is a true SDF, so onion(mesh) needs no normalize.
            var graph = Field.Onion(Field.Mesh("part.obj"), thickness: 0.5);
            Assert.Empty(Validate(graph));
        }

        // ---- Tier 2: metric-ness ----------------------------------------------------------

        [Fact]
        public void Onion_over_raw_gyroid_without_normalize_is_a_warning_not_an_error()
        {
            // DualC accepts and contours onion(gyroid) — it is the project's golden-count gate.
            // So this is a quality warning (non-metric wall), never a hard error.
            var graph = Field.Onion(Field.Gyroid(wavelength: 0.5), thickness: 0.1);

            var issue = Assert.Single(Validate(graph));
            Assert.Equal("/root/in/0", issue.Path);
            Assert.Equal(ValidationSeverity.Warning, issue.Severity);
            Assert.Contains("normalize", issue.Message);
            Assert.Contains("non-metric", issue.Message);
        }

        [Fact]
        public void Smooth_union_requires_both_operands_metric()
        {
            // sphere is metric; raw gyroid is not → exactly one issue, on the gyroid operand.
            var graph = Field.SmoothUnion(Field.Sphere(radius: 1.0), Field.Gyroid(), k: 0.3);

            var issues = Validate(graph);
            var issue = Assert.Single(issues);
            Assert.Equal("/root/in/1", issue.Path);
        }

        [Fact]
        public void Graded_onion_checks_only_the_base_child_not_the_control()
        {
            // base = raw gyroid (non-metric → flagged); control = raw gyroid (allowed, any field).
            var graph = Field.GradedOnion(Field.Gyroid(), Field.Gyroid(), t1: 0.04, t2: 0.18, d1: 7);

            var issues = Validate(graph);
            var issue = Assert.Single(issues);
            Assert.Equal("/root/in/0", issue.Path); // only the base, not /root/in/1
        }

        [Fact]
        public void Hard_boolean_propagates_metric_ness_of_operands()
        {
            // union(mesh, sphere) is metric → onion over it is fine.
            var metricUnion = Field.Union(Field.Mesh("a.obj"), Field.Sphere(radius: 1.0));
            Assert.True(FieldGraphValidator.IsMetric(metricUnion));
            Assert.Empty(Validate(Field.Onion(metricUnion, thickness: 0.2)));

            // union(gyroid, sphere) is non-metric (one operand non-metric) → onion flags it.
            var mixedUnion = Field.Union(Field.Gyroid(), Field.Sphere(radius: 1.0));
            Assert.False(FieldGraphValidator.IsMetric(mixedUnion));
            Assert.NotEmpty(Validate(Field.Onion(mixedUnion, thickness: 0.2)));
        }

        // ---- Tier 1: schema lookups -------------------------------------------------------

        [Fact]
        public void Unknown_op_is_reported()
        {
            var graph = new FieldNode("frobnicate");
            var issue = Assert.Single(Validate(graph));
            Assert.Contains("unknown op 'frobnicate'", issue.Message);
        }

        [Fact]
        public void Wrong_child_arity_is_reported()
        {
            // intersection needs 2 children; give it 1.
            var graph = new FieldNode("intersection", null, new[] { Field.Sphere(radius: 1.0) });
            var issue = Assert.Single(Validate(graph));
            Assert.Contains("2 child(ren), got 1", issue.Message);
        }

        [Fact]
        public void Missing_required_param_is_reported_with_locator()
        {
            // onion requires thickness; hand-build a node without it.
            var graph = new FieldNode("onion", null, new[] { Field.Mesh("p.obj") });
            var issues = Validate(graph);
            Assert.Contains(issues, i => i.Path == "/root/thickness" && i.Message.Contains("thickness"));
        }

        [Fact]
        public void Unknown_param_key_is_reported()
        {
            var bag = new Dictionary<string, FieldValue> { ["wibble"] = FieldValue.Scalar(1.0) };
            var graph = new FieldNode("sphere", bag);
            Assert.Contains(Validate(graph), i => i.Message.Contains("no param 'wibble'"));
        }

        [Fact]
        public void ValidateOrThrow_throws_on_errors_only_not_warnings()
        {
            FieldGraphValidator.ValidateOrThrow(Field.Sphere(radius: 1.0)); // valid: no throw

            // onion(gyroid) is a warning, not an error → must NOT throw (DualC contours it).
            FieldGraphValidator.ValidateOrThrow(Field.Onion(Field.Gyroid(), thickness: 0.1));

            // Arity error → throws, carrying only the error issue(s).
            var bad = new FieldNode("intersection", null, new[] { Field.Sphere(radius: 1.0) });
            var ex = Assert.Throws<FieldGraphValidationException>(() => FieldGraphValidator.ValidateOrThrow(bad));
            Assert.All(ex.Issues, i => Assert.Equal(ValidationSeverity.Error, i.Severity));
        }

        // ---- porous: the arity range, the choices, the grading, the thickness --------------

        private static FieldNode PorousWith(int children, params (string key, FieldValue value)[] ps)
        {
            var p = new Dictionary<string, FieldValue>(System.StringComparer.Ordinal)
            {
                ["spacing"] = FieldValue.Scalar(0.4),
            };
            foreach (var (key, value) in ps) p[key] = value;
            var kids = Enumerable.Range(0, children).Select(_ => Field.Sphere(radius: 1)).ToArray();
            return new FieldNode("porous", p, kids);
        }

        [Fact]
        public void Porous_takes_one_or_two_children()
        {
            foreach (int n in new[] { 0, 3 })
            {
                var issue = Assert.Single(Validate(Field.Onion(PorousWith(n), 0.05)),
                    i => i.Severity == ValidationSeverity.Error);
                Assert.Equal("/root/in/0", issue.Path);
                Assert.Contains("takes 1–2 child(ren)", issue.Message);
            }
            Assert.Empty(Validate(Field.Onion(PorousWith(1), 0.05)));
            Assert.DoesNotContain(Validate(Field.Onion(PorousWith(2,
                    ("spacing2", FieldValue.Scalar(0.6)), ("d1", FieldValue.Scalar(1))), 0.05)),
                i => i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void An_exact_arity_keeps_its_message()
        {
            var bad = new FieldNode("onion",
                new Dictionary<string, FieldValue> { ["thickness"] = FieldValue.Scalar(0.1) });
            var issue = Assert.Single(Validate(bad));
            Assert.Equal("op 'onion' takes 1 child(ren), got 0", issue.Message);
        }

        [Fact]
        public void Porous_rejects_a_name_outside_its_choices()
        {
            var issue = Assert.Single(Validate(Field.Onion(
                Field.Porous(Field.Sphere(radius: 1), 0.4, cells: "hexagonal"), 0.05)));
            Assert.Equal(ValidationSeverity.Error, issue.Severity);
            Assert.Equal("/root/in/0/cells", issue.Path);
            Assert.Contains("voronoi, delaunay", issue.Message);
        }

        [Fact]
        public void Graded_porous_needs_a_control_and_d1()
        {
            var noControl = Validate(Field.Onion(
                Field.Porous(Field.Sphere(radius: 1), 0.4, spacing2: 0.6, d1: 1), 0.05));
            var e1 = Assert.Single(noControl);
            Assert.Equal("'porous' spacing2 differs from spacing: give a control field as the second child", e1.Message);

            var noD1 = Validate(Field.Onion(
                Field.Porous(Field.Sphere(radius: 1), 0.4, control: Field.Sphere(radius: 0), spacing2: 0.6), 0.05));
            var e2 = Assert.Single(noD1);
            Assert.Equal("/root/in/0/d1", e2.Path);
            Assert.Equal(ValidationSeverity.Error, e2.Severity);
        }

        [Fact]
        public void A_control_without_grading_is_a_warning()
        {
            var issue = Assert.Single(Validate(Field.Onion(
                Field.Porous(Field.Sphere(radius: 1), 0.4, control: Field.Sphere(radius: 0)), 0.05)));
            Assert.Equal(ValidationSeverity.Warning, issue.Severity);
            Assert.Equal("/root/in/0/in/1", issue.Path);
        }

        [Fact]
        public void Porous_over_a_non_metric_volume_is_a_warning()
        {
            var issue = Assert.Single(Validate(Field.Onion(
                Field.Porous(Field.Intersection(Field.Gyroid(), Field.Sphere(radius: 1)), 0.4), 0.05)));
            Assert.Equal(ValidationSeverity.Warning, issue.Severity);
            Assert.Equal("/root/in/0/in/0", issue.Path);
            Assert.Contains("normalize", issue.Message);
        }

        [Fact]
        public void A_porous_field_without_a_thickness_is_a_warning()
        {
            var bare = Assert.Single(Validate(Field.Porous(Field.Sphere(radius: 1), 0.4)));
            Assert.Equal(ValidationSeverity.Warning, bare.Severity);
            Assert.Contains("no thickness", bare.Message);

            var clipped = Assert.Single(Validate(
                Field.Intersection(Field.Sphere(radius: 1), Field.Porous(Field.Sphere(radius: 1), 0.4))));
            Assert.Equal("/root/in/1", clipped.Path);

            // Thickened, or read as a control value: clean.
            Assert.Empty(Validate(Field.Offset(Field.Porous(Field.Sphere(radius: 1), 0.25, mode: "struts"), 0.015)));
            Assert.Empty(Validate(Field.GradedOnion(Field.Sphere(radius: 1),
                Field.Porous(Field.Sphere(radius: 1), 0.4), t1: 0.02, t2: 0.06, d1: 0.1)));
        }

        [Fact]
        public void Porous_is_metric()
        {
            Assert.True(FieldGraphValidator.IsMetric(Field.Porous(Field.Sphere(radius: 1), 0.4)));
            Assert.Empty(Validate(Field.SmoothIntersection(Field.Sphere(radius: 1),
                Field.Onion(Field.Porous(Field.Sphere(radius: 1), 0.4), 0.06), k: 0.1)));
        }
    }
}
