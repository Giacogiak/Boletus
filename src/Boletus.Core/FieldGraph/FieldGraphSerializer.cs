using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Boletus.Core.FieldGraph
{
    /// <summary>
    /// Emits a <see cref="FieldNode"/> tree as a canonical DualC field-graph JSON document.
    /// Hand-rolled (no JSON dependency) so the output matches DualC's own <c>--dump-json</c>
    /// emitter: <c>{ "version": 1, "units": "mm", "root": &lt;node&gt; }</c>, 2-space indented,
    /// with each node's keys in canonical order — <c>"op"</c> first, params sorted
    /// ASCII-ordinal-ascending, <c>"in"</c> last.
    /// </summary>
    /// <remarks>
    /// The correctness gate is a round-trip through DualC's <c>dualc_field --dump-json</c>
    /// (re-canonicalizes our output), so the exact key-sort and double formatting here are
    /// *not* load-bearing for correctness — only that DualC parses the JSON. The one
    /// genuinely load-bearing emitter detail is string escaping (Windows mesh temp-paths
    /// carry backslashes, invalid in JSON unless escaped).
    /// </remarks>
    public static class FieldGraphSerializer
    {
        private const string DefaultUnits = "mm";

        /// <summary>Serialize <paramref name="root"/> as a full canonical document.</summary>
        public static string Serialize(FieldNode root)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            var sb = new StringBuilder(256);
            sb.Append("{\n");
            sb.Append("  \"version\": 1,\n");
            sb.Append("  \"units\": \"").Append(DefaultUnits).Append("\",\n");
            sb.Append("  \"root\": ");
            WriteNode(sb, root, indent: 1);
            sb.Append("\n}");
            return sb.ToString();
        }

        private static void WriteNode(StringBuilder sb, FieldNode node, int indent)
        {
            string pad = Indent(indent);
            string padInner = Indent(indent + 1);

            sb.Append("{\n");

            // "op" always first.
            sb.Append(padInner).Append("\"op\": ");
            WriteString(sb, node.Op);

            // Params next, ASCII-ordinal-sorted (matches nlohmann std::map byte-wise ordering).
            var keys = new List<string>(node.Params.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                sb.Append(",\n").Append(padInner);
                WriteString(sb, key);
                sb.Append(": ");
                WriteValue(sb, node.Params[key], indent + 1);
            }

            // "in" last, only when there are children (sources omit it).
            if (node.Children.Count > 0)
            {
                sb.Append(",\n").Append(padInner).Append("\"in\": [\n");
                for (int i = 0; i < node.Children.Count; i++)
                {
                    sb.Append(Indent(indent + 2));
                    WriteNode(sb, node.Children[i], indent + 2);
                    if (i < node.Children.Count - 1) sb.Append(',');
                    sb.Append('\n');
                }
                sb.Append(padInner).Append(']');
            }

            sb.Append('\n').Append(pad).Append('}');
        }

        private static void WriteValue(StringBuilder sb, FieldValue value, int indent)
        {
            switch (value.Kind)
            {
                case FieldValueKind.Scalar:
                    sb.Append(FormatNumber(value.AsScalar));
                    break;
                case FieldValueKind.Text:
                    WriteString(sb, value.AsText);
                    break;
                case FieldValueKind.Vector:
                    WriteVector(sb, value.AsVector, indent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Unknown FieldValueKind.");
            }
        }

        private static void WriteVector(StringBuilder sb, IReadOnlyList<double> values, int indent)
        {
            if (values.Count == 0)
            {
                sb.Append("[]");
                return;
            }
            string padElem = Indent(indent + 1);
            sb.Append("[\n");
            for (int i = 0; i < values.Count; i++)
            {
                sb.Append(padElem).Append(FormatNumber(values[i]));
                if (i < values.Count - 1) sb.Append(',');
                sb.Append('\n');
            }
            sb.Append(Indent(indent)).Append(']');
        }

        /// <summary>
        /// Format a double the way DualC's emitter does: shortest round-trippable decimal,
        /// invariant culture, with a forced <c>.0</c> when the value is integral (DualC prints
        /// <c>2.0</c>, not <c>2</c>, for numeric params).
        /// </summary>
        private static string FormatNumber(double value)
        {
            // .NET Core 3.0+ (the net7/net9 hosts this runs under) yields the shortest
            // round-trippable representation from the default invariant formatting.
            string s = value.ToString(CultureInfo.InvariantCulture);
            if (s.IndexOf('.') < 0 && s.IndexOf('e') < 0 && s.IndexOf('E') < 0)
                s += ".0";
            return s;
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static string Indent(int level) => new string(' ', level * 2);
    }
}
