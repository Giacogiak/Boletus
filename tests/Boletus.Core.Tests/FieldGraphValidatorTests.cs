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
    }
}
