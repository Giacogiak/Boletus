using System;
using System.Collections.Generic;

namespace Boletus.Core.FieldGraph
{
    /// <summary>
    /// One node of a DualC field graph: an op token, a bag of named parameters, and ordered
    /// children. Immutable. The natural mapping for Grasshopper — each component builds one
    /// node; wiring builds the tree; the terminal node serializes to canonical JSON
    /// (see <see cref="FieldGraphSerializer"/>).
    /// </summary>
    /// <remarks>
    /// Parameter insertion order is irrelevant: the serializer emits keys in DualC's canonical
    /// order (<c>op</c> first, params ASCII-ordinal-sorted, <c>in</c> last). Arity rules
    /// (sources: 0 children; decorators/domain-ops: 1; booleans + <c>graded-onion</c>: 2) are
    /// not enforced here — that is the validator's job — so partial trees can be built up.
    /// </remarks>
    public sealed class FieldNode
    {
        public string Op { get; }
        public IReadOnlyDictionary<string, FieldValue> Params { get; }
        public IReadOnlyList<FieldNode> Children { get; }

        public FieldNode(
            string op,
            IReadOnlyDictionary<string, FieldValue>? parameters = null,
            IReadOnlyList<FieldNode>? children = null)
        {
            Op = op ?? throw new ArgumentNullException(nameof(op));
            if (op.Length == 0) throw new ArgumentException("Op token must be non-empty.", nameof(op));

            // Defensive copies so the node is genuinely immutable regardless of caller mutation.
            Params = parameters is null
                ? EmptyParams
                : new Dictionary<string, FieldValue>(CountedDictionary(parameters), StringComparer.Ordinal);

            Children = children is null ? Array.Empty<FieldNode>() : CopyChildren(children);
        }

        private static readonly IReadOnlyDictionary<string, FieldValue> EmptyParams =
            new Dictionary<string, FieldValue>(0, StringComparer.Ordinal);

        private static IDictionary<string, FieldValue> CountedDictionary(IReadOnlyDictionary<string, FieldValue> source)
        {
            var d = new Dictionary<string, FieldValue>(source.Count, StringComparer.Ordinal);
            foreach (var kv in source) d[kv.Key] = kv.Value;
            return d;
        }

        private static FieldNode[] CopyChildren(IReadOnlyList<FieldNode> children)
        {
            var arr = new FieldNode[children.Count];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = children[i] ?? throw new ArgumentException("Child node must not be null.", nameof(children));
            return arr;
        }

        /// <summary>Serialize this node as the root of a canonical DualC field-graph document.</summary>
        public string ToJson() => FieldGraphSerializer.Serialize(this);

        public override string ToString() => $"{Op}({Params.Count} params, {Children.Count} children)";
    }
}
