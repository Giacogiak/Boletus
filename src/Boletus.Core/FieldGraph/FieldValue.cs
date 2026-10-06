using System;
using System.Collections.Generic;

namespace Boletus.Core.FieldGraph
{
    /// <summary>The kind a <see cref="FieldValue"/> carries.</summary>
    public enum FieldValueKind
    {
        /// <summary>A single double (e.g. <c>radius</c>, <c>thickness</c>).</summary>
        Scalar,
        /// <summary>An ordered list of doubles (a grouped key like <c>center</c>/<c>min</c>,
        /// or the universal flat <c>params</c> array).</summary>
        Vector,
        /// <summary>A string (e.g. <c>path</c>, <c>sign</c>, <c>fn</c>, or a string
        /// <c>axis</c> like <c>"z"</c>).</summary>
        Text,
    }

    /// <summary>
    /// One field-graph parameter value. DualC's canonical JSON renders every numeric value
    /// as a double, so a scalar/vector hold doubles; <see cref="FieldValueKind.Text"/> holds
    /// strings (paths, enum-like tokens). Immutable.
    /// </summary>
    public readonly struct FieldValue : IEquatable<FieldValue>
    {
        public FieldValueKind Kind { get; }

        private readonly double _scalar;
        private readonly IReadOnlyList<double>? _vector;
        private readonly string? _text;

        private FieldValue(FieldValueKind kind, double scalar, IReadOnlyList<double>? vector, string? text)
        {
            Kind = kind;
            _scalar = scalar;
            _vector = vector;
            _text = text;
        }

        public static FieldValue Scalar(double value) =>
            new FieldValue(FieldValueKind.Scalar, value, null, null);

        public static FieldValue Vector(params double[] values) =>
            new FieldValue(FieldValueKind.Vector, 0, (double[])values.Clone(), null);

        public static FieldValue Vector(IReadOnlyList<double> values) =>
            new FieldValue(FieldValueKind.Vector, 0, CopyOf(values), null);

        public static FieldValue Text(string value) =>
            new FieldValue(FieldValueKind.Text, 0, null, value ?? throw new ArgumentNullException(nameof(value)));

        /// <summary>The scalar payload. Throws if this value is not a <see cref="FieldValueKind.Scalar"/>.</summary>
        public double AsScalar =>
            Kind == FieldValueKind.Scalar ? _scalar : throw new InvalidOperationException($"FieldValue is {Kind}, not Scalar.");

        /// <summary>The vector payload. Throws if this value is not a <see cref="FieldValueKind.Vector"/>.</summary>
        public IReadOnlyList<double> AsVector =>
            Kind == FieldValueKind.Vector ? _vector! : throw new InvalidOperationException($"FieldValue is {Kind}, not Vector.");

        /// <summary>The string payload. Throws if this value is not a <see cref="FieldValueKind.Text"/>.</summary>
        public string AsText =>
            Kind == FieldValueKind.Text ? _text! : throw new InvalidOperationException($"FieldValue is {Kind}, not Text.");

        private static double[] CopyOf(IReadOnlyList<double> values)
        {
            if (values is null) throw new ArgumentNullException(nameof(values));
            var copy = new double[values.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = values[i];
            return copy;
        }

        public bool Equals(FieldValue other)
        {
            if (Kind != other.Kind) return false;
            switch (Kind)
            {
                case FieldValueKind.Scalar:
                    return _scalar.Equals(other._scalar);
                case FieldValueKind.Text:
                    return string.Equals(_text, other._text, StringComparison.Ordinal);
                case FieldValueKind.Vector:
                    var a = _vector!;
                    var b = other._vector!;
                    if (a.Count != b.Count) return false;
                    for (int i = 0; i < a.Count; i++)
                        if (!a[i].Equals(b[i])) return false;
                    return true;
                default:
                    return false;
            }
        }

        public override bool Equals(object? obj) => obj is FieldValue v && Equals(v);

        public override int GetHashCode()
        {
            switch (Kind)
            {
                case FieldValueKind.Scalar: return _scalar.GetHashCode();
                case FieldValueKind.Text: return _text!.GetHashCode();
                case FieldValueKind.Vector:
                    int h = 17;
                    foreach (var d in _vector!) h = unchecked(h * 31 + d.GetHashCode());
                    return h;
                default: return 0;
            }
        }
    }
}
