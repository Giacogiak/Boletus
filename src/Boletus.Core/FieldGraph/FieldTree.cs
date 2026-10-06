using System;
using System.Collections.Generic;

namespace Boletus.Core.FieldGraph
{
    /// <summary>
    /// Pure tree-transform helpers over the immutable <see cref="FieldNode"/> graph. Kept Rhino-free
    /// (and DualC-free) so the rewrites the Grasshopper terminal depends on are unit-testable on a
    /// plain runner.
    /// </summary>
    public static class FieldTree
    {
        /// <summary>
        /// Bottom-up immutable rewrite. Each node's children are rewritten first; <paramref name="map"/>
        /// is then applied to that node (already carrying any rewritten children) and returns either
        /// the same instance (leave it unchanged) or a replacement. Subtrees that <paramref name="map"/>
        /// leaves untouched are returned <b>by reference</b> — no allocation — so a no-op rewrite
        /// yields the original root.
        /// </summary>
        public static FieldNode Rewrite(FieldNode node, Func<FieldNode, FieldNode> map)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));
            if (map is null) throw new ArgumentNullException(nameof(map));

            FieldNode[]? newChildren = null;
            for (int i = 0; i < node.Children.Count; i++)
            {
                var rewritten = Rewrite(node.Children[i], map);
                if (!ReferenceEquals(rewritten, node.Children[i]))
                {
                    newChildren ??= CopyChildren(node.Children);
                    newChildren[i] = rewritten;
                }
            }

            FieldNode current = newChildren is null
                ? node
                : new FieldNode(node.Op, node.Params, newChildren);
            return map(current);
        }

        /// <summary>A shallow, mutable, ordinal-keyed copy of a node's params for a <c>map</c> to edit
        /// before building a replacement node.</summary>
        public static Dictionary<string, FieldValue> CopyParams(IReadOnlyDictionary<string, FieldValue> p)
        {
            if (p is null) throw new ArgumentNullException(nameof(p));
            var d = new Dictionary<string, FieldValue>(p.Count, StringComparer.Ordinal);
            foreach (var kv in p) d[kv.Key] = kv.Value;
            return d;
        }

        private static FieldNode[] CopyChildren(IReadOnlyList<FieldNode> children)
        {
            var arr = new FieldNode[children.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = children[i];
            return arr;
        }
    }
}
