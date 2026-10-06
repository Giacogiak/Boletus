using System;
using System.Collections.Generic;
using System.Text;

namespace Boletus.Core.FieldGraph
{
    /// <summary>
    /// How serious a <see cref="ValidationIssue"/> is.
    /// <see cref="Error"/> = a graph DualC would reject (arity, missing/typed/unknown params,
    /// unknown op). <see cref="Warning"/> = a graph DualC <em>accepts and contours</em> but that
    /// is likely a mistake (e.g. a non-metric source feeding <c>onion</c> makes the wall
    /// thickness non-metric — the project's own golden-count gate uses exactly this on purpose).
    /// </summary>
    public enum ValidationSeverity
    {
        Warning,
        Error,
    }

    /// <summary>A single validation problem, located by a DualC-style JSON pointer.</summary>
    public sealed class ValidationIssue
    {
        /// <summary>JSON pointer to the offending node/param, e.g. <c>/root/in/0/thickness</c>.</summary>
        public string Path { get; }
        public string Message { get; }
        public ValidationSeverity Severity { get; }

        public ValidationIssue(string path, string message, ValidationSeverity severity)
        {
            Path = path;
            Message = message;
            Severity = severity;
        }

        public override string ToString() => $"{Severity.ToString().ToLowerInvariant()} {Path}: {Message}";
    }

    /// <summary>
    /// Validates a managed field-graph tree before it is serialized and handed to DualC.
    /// Catches the structural mistakes DualC would otherwise only report after a round-trip,
    /// with the same JSON-pointer locators DualC uses. Two tiers:
    /// <list type="number">
    /// <item><b>Errors</b> (schema lookups) — unknown op, wrong child arity, missing required
    /// param, unknown/mistyped param key. These are graphs DualC itself rejects.</item>
    /// <item><b>Warnings</b> (a metric-ness tree-walk) — a non-metric source (raw TPMS /
    /// <c>winding</c>) feeding a metric op (<c>onion</c> / <c>graded-onion</c> base / smooth
    /// boolean) without a <c>normalize</c> wrap. DualC accepts and contours these (the golden
    /// gate uses one deliberately), but the resulting wall thickness is non-metric — usually a
    /// mistake worth surfacing, not a hard failure.</item>
    /// </list>
    /// </summary>
    public static class FieldGraphValidator
    {
        /// <summary>Collect every issue (errors and warnings) in the tree. Does not throw.</summary>
        public static IReadOnlyList<ValidationIssue> Validate(FieldNode root)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            var issues = new List<ValidationIssue>();
            Walk(root, "/root", issues);
            return issues;
        }

        /// <summary>
        /// Validate and throw a <see cref="FieldGraphValidationException"/> if any
        /// <see cref="ValidationSeverity.Error"/> issue is present. Warnings never throw —
        /// they describe graphs DualC accepts.
        /// </summary>
        public static void ValidateOrThrow(FieldNode root)
        {
            var issues = Validate(root);
            var errors = new List<ValidationIssue>();
            foreach (var i in issues)
                if (i.Severity == ValidationSeverity.Error) errors.Add(i);
            if (errors.Count > 0) throw new FieldGraphValidationException(errors);
        }

        private static void Walk(FieldNode node, string path, List<ValidationIssue> issues)
        {
            if (!Ops.TryGet(node.Op, out var schema))
            {
                issues.Add(new ValidationIssue(path, $"unknown op '{node.Op}'", ValidationSeverity.Error));
                // Still recurse so children get their own diagnostics.
                WalkChildren(node, path, issues);
                return;
            }

            // Tier 1a — child arity.
            if (node.Children.Count != schema.ChildCount)
            {
                issues.Add(new ValidationIssue(path,
                    $"op '{node.Op}' takes {schema.ChildCount} child(ren), got {node.Children.Count}",
                    ValidationSeverity.Error));
            }

            // Tier 1b — required params present.
            foreach (var spec in schema.Params)
            {
                if (spec.Required && !node.Params.ContainsKey(spec.Name))
                    issues.Add(new ValidationIssue(path + "/" + spec.Name,
                        $"op '{node.Op}' is missing required param '{spec.Name}'", ValidationSeverity.Error));
            }

            // Tier 1c — unknown / mistyped params.
            foreach (var kv in node.Params)
            {
                if (!schema.TryGetParam(kv.Key, out var spec))
                {
                    issues.Add(new ValidationIssue(path + "/" + kv.Key,
                        $"op '{node.Op}' has no param '{kv.Key}'", ValidationSeverity.Error));
                    continue;
                }
                if (!KindMatches(spec, kv.Value))
                    issues.Add(new ValidationIssue(path + "/" + kv.Key,
                        $"param '{kv.Key}' should be {Describe(spec)}, got {kv.Value.Kind}", ValidationSeverity.Error));
            }

            // Tier 1d — mesh/winding: exactly one of path / id (a constraint the generic
            // Required flag can't express). path = disk; id = a v0.3.0 in-memory host buffer.
            if (node.Op == "mesh" || node.Op == "winding")
            {
                bool hasPath = node.Params.ContainsKey("path");
                bool hasId = node.Params.ContainsKey("id");
                if (!hasPath && !hasId)
                    issues.Add(new ValidationIssue(path,
                        $"op '{node.Op}' needs a source: either 'path' (disk) or 'id' (in-memory buffer)",
                        ValidationSeverity.Error));
                else if (hasPath && hasId)
                    issues.Add(new ValidationIssue(path,
                        $"op '{node.Op}' has both 'path' and 'id' — supply exactly one",
                        ValidationSeverity.Error));
            }

            // Tier 2 — metric-ness requirements for the ops that need a metric input.
            CheckMetricInputs(node, schema, path, issues);

            WalkChildren(node, path, issues);
        }

        private static void WalkChildren(FieldNode node, string path, List<ValidationIssue> issues)
        {
            for (int i = 0; i < node.Children.Count; i++)
                Walk(node.Children[i], $"{path}/in/{i}", issues);
        }

        private static void CheckMetricInputs(FieldNode node, OpSchema schema, string path, List<ValidationIssue> issues)
        {
            switch (schema.Category)
            {
                case OpCategory.Onion:
                    if (node.Children.Count >= 1 && !IsMetric(node.Children[0]))
                        issues.Add(NonMetric(path + "/in/0", node.Op, node.Children[0].Op));
                    break;

                case OpCategory.GradedOnion:
                    // Only the base (first) child must be metric; the control child is any field.
                    if (node.Children.Count >= 1 && !IsMetric(node.Children[0]))
                        issues.Add(NonMetric(path + "/in/0", node.Op, node.Children[0].Op));
                    break;

                case OpCategory.SmoothBoolean:
                    for (int i = 0; i < node.Children.Count; i++)
                        if (!IsMetric(node.Children[i]))
                            issues.Add(NonMetric($"{path}/in/{i}", node.Op, node.Children[i].Op));
                    break;
            }
        }

        private static ValidationIssue NonMetric(string path, string op, string childOp) =>
            new ValidationIssue(path,
                $"op '{op}' assumes a metric (signed-distance) input, but '{childOp}' is non-metric — " +
                "wrap it in 'normalize' for a metric wall thickness (DualC still contours it as-is)",
                ValidationSeverity.Warning);

        /// <summary>
        /// Whether a sub-tree yields a (first-order) metric field. Raw TPMS and <c>winding</c>
        /// are non-metric; <c>normalize</c> makes any child metric; metric sources, shells and
        /// smooth booleans are metric; one-child decorators propagate their child; a hard
        /// boolean is metric only when both operands are.
        /// </summary>
        public static bool IsMetric(FieldNode node)
        {
            if (!Ops.TryGet(node.Op, out var schema))
                return false; // unknown op: can't assume metric.

            switch (schema.Category)
            {
                case OpCategory.Tpms:
                case OpCategory.Winding:
                    return false;

                case OpCategory.MetricSource:
                case OpCategory.Normalize:
                case OpCategory.Onion:
                case OpCategory.GradedOnion:
                case OpCategory.SmoothBoolean:
                    return true;

                case OpCategory.Decorator:
                    return node.Children.Count >= 1 && IsMetric(node.Children[0]);

                case OpCategory.HardBoolean:
                    if (node.Children.Count < 2) return false;
                    return IsMetric(node.Children[0]) && IsMetric(node.Children[1]);

                default:
                    return false;
            }
        }

        private static bool KindMatches(ParamSpec spec, FieldValue value)
        {
            switch (spec.Kind)
            {
                case ParamKind.Scalar: return value.Kind == FieldValueKind.Scalar;
                case ParamKind.Text: return value.Kind == FieldValueKind.Text;
                case ParamKind.Vector:
                    if (value.Kind != FieldValueKind.Vector) return false;
                    return spec.VectorLength is null || value.AsVector.Count == spec.VectorLength.Value;
                default: return false;
            }
        }

        private static string Describe(ParamSpec spec) =>
            spec.Kind == ParamKind.Vector && spec.VectorLength.HasValue
                ? $"a {spec.VectorLength.Value}-vector"
                : spec.Kind.ToString().ToLowerInvariant();
    }

    /// <summary>Thrown by <see cref="FieldGraphValidator.ValidateOrThrow"/> when a tree is invalid.</summary>
    public sealed class FieldGraphValidationException : Exception
    {
        public IReadOnlyList<ValidationIssue> Issues { get; }

        public FieldGraphValidationException(IReadOnlyList<ValidationIssue> issues)
            : base(BuildMessage(issues))
        {
            Issues = issues;
        }

        private static string BuildMessage(IReadOnlyList<ValidationIssue> issues)
        {
            var sb = new StringBuilder("invalid field graph:");
            foreach (var issue in issues) sb.Append("\n  ").Append(issue);
            return sb.ToString();
        }
    }
}
