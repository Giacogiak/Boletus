using System;
using System.Globalization;
using System.Text;
using Boletus.Core.FieldGraph;

namespace Boletus.Core.LivePreview
{
    /// <summary>
    /// The text <c>Live Preview</c> writes to its temp <c>graph.json</c>: the canonical graph
    /// document (<see cref="FieldGraphSerializer.Serialize"/>, byte-identical to DualC's
    /// <c>--dump-json</c>) plus, only when bounds are given, one optional top-level
    /// <c>"view"</c> key that carries them to DualCEditor.
    /// </summary>
    /// <remarks>
    /// DualC's graph parser reads the top-level <c>root</c> only and ignores every other
    /// top-level key, so the same file still feeds the C ABI and <c>dualc_field_view</c>
    /// unchanged (unknown keys <em>inside a node</em> are rejected; this one is outside).
    /// The key rides in the same atomically swapped file, so a <c>Min</c> / <c>Max</c> change
    /// reaches a running editor through the one file-watch, live, with no second channel.
    /// <see cref="FieldGraphSerializer"/> itself is not touched: the splice happens here, after
    /// the opening brace, and <see cref="Compose"/> with no bounds returns the serializer's
    /// text as is.
    /// </remarks>
    public static class LivePreviewDocument
    {
        /// <summary>The optional top-level key and its one member.</summary>
        public const string ViewKey = "view";
        public const string BoundsKey = "bounds";

        /// <summary>Serialize <paramref name="root"/>; with <paramref name="bounds"/>, add the <c>view</c> block.</summary>
        public static string Compose(FieldNode root, ViewBounds? bounds)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            string json = FieldGraphSerializer.Serialize(root);
            if (bounds is null) return json;
            if (!bounds.Value.IsValid)
                throw new ArgumentException("View bounds need six finite numbers with min < max per axis.", nameof(bounds));

            const string open = "{\n";
            if (!json.StartsWith(open, StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected serializer output: the document does not open with '{'.");

            var sb = new StringBuilder(json.Length + 96);
            sb.Append(open);
            sb.Append("  \"").Append(ViewKey).Append("\": { \"").Append(BoundsKey).Append("\": [ ");
            double[] b = bounds.Value.ToArray();
            for (int i = 0; i < b.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatNumber(b[i]));
            }
            sb.Append(" ] },\n");
            sb.Append(json, open.Length, json.Length - open.Length);
            return sb.ToString();
        }

        /// <summary>The serializer's number rule, repeated rather than exposed: shortest
        /// round-trippable, invariant culture, an integral value written with <c>.0</c>.</summary>
        private static string FormatNumber(double value)
        {
            string s = value.ToString(CultureInfo.InvariantCulture);
            if (s.IndexOf('.') < 0 && s.IndexOf('e') < 0 && s.IndexOf('E') < 0)
                s += ".0";
            return s;
        }
    }
}
