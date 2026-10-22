using System;
using System.Collections.Generic;

namespace Boletus.Core.FieldGraph
{
    /// <summary>Broad op category — drives the metric-ness reasoning in the validator.</summary>
    public enum OpCategory
    {
        /// <summary>Raw TPMS source (gyroid, schwarz-p, …): non-metric until <c>normalize</c>d.</summary>
        Tpms,
        /// <summary><c>winding</c>: non-metric (generalized-winding-number field).</summary>
        Winding,
        /// <summary>Analytic primitive or <c>mesh</c>: a true signed-distance (metric) source.</summary>
        MetricSource,
        /// <summary>Hard boolean (union/intersection/difference/xor): correct over non-metric inputs.</summary>
        HardBoolean,
        /// <summary>Smooth boolean (smooth-*): assumes metric distance inputs.</summary>
        SmoothBoolean,
        /// <summary><c>normalize</c>: rescales any child toward unit gradient → metric output.</summary>
        Normalize,
        /// <summary>Shell op (<c>onion</c>): needs a metric child.</summary>
        Onion,
        /// <summary><c>graded-onion</c>: needs a metric base child (first child).</summary>
        GradedOnion,
        /// <summary>Any other one-child decorator / domain op (offset, scale, translate, …).</summary>
        Decorator,
        /// <summary><c>porous</c>: an unsigned, 1-Lipschitz distance to a cell complex — metric, and
        /// empty until thickened (onion / offset / their graded forms).</summary>
        Porous,
    }

    /// <summary>The expected value kind for a parameter.</summary>
    public enum ParamKind
    {
        Scalar,
        Vector,
        Text,
    }

    /// <summary>Schema for a single op parameter.</summary>
    public sealed class ParamSpec
    {
        public string Name { get; }
        public ParamKind Kind { get; }
        public bool Required { get; }
        /// <summary>Expected element count for a <see cref="ParamKind.Vector"/> (null = any length, e.g. flat <c>params</c>).</summary>
        public int? VectorLength { get; }
        /// <summary>The closed set a <see cref="ParamKind.Text"/> value must be one of (null = any text).</summary>
        public IReadOnlyList<string>? Choices { get; }

        public ParamSpec(string name, ParamKind kind, bool required, int? vectorLength = null,
            IReadOnlyList<string>? choices = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Kind = kind;
            Required = required;
            VectorLength = vectorLength;
            Choices = choices;
        }
    }

    /// <summary>
    /// The pinned schema for one field-graph op: its canonical token, child arity (exact, or a
    /// range such as <c>porous</c>'s 1–2), its
    /// parameters, and the category the validator uses for metric-ness reasoning. Tokens,
    /// parameter names and arities are pinned from DualC's
    /// <c>docs/command_reference/11-dualc_field/01-op-vocabulary.md</c> + <c>12-primitive-catalogue.md</c> and the
    /// live <c>dualc_field --list</c> — not from memory.
    /// </summary>
    public sealed class OpSchema
    {
        public string Token { get; }
        /// <summary>The fewest children the op takes.</summary>
        public int MinChildren { get; }
        /// <summary>The most children the op takes (equal to <see cref="MinChildren"/> for an exact arity).</summary>
        public int MaxChildren { get; }
        public OpCategory Category { get; }
        public IReadOnlyList<ParamSpec> Params { get; }

        private readonly Dictionary<string, ParamSpec> _byName;

        public OpSchema(string token, int childCount, OpCategory category, params ParamSpec[] parameters)
            : this(token, childCount, childCount, category, parameters)
        {
        }

        public OpSchema(string token, int minChildren, int maxChildren, OpCategory category, params ParamSpec[] parameters)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            if (minChildren < 0 || maxChildren < minChildren)
                throw new ArgumentOutOfRangeException(nameof(maxChildren), "need 0 <= minChildren <= maxChildren");
            MinChildren = minChildren;
            MaxChildren = maxChildren;
            Category = category;
            Params = parameters ?? Array.Empty<ParamSpec>();
            _byName = new Dictionary<string, ParamSpec>(Params.Count, StringComparer.Ordinal);
            foreach (var p in Params) _byName[p.Name] = p;
        }

        public bool TryGetParam(string name, out ParamSpec spec) => _byName.TryGetValue(name, out spec!);
    }
}
