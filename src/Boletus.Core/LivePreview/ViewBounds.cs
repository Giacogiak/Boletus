using System;
using System.Globalization;

namespace Boletus.Core.LivePreview
{
    /// <summary>
    /// The trace box a side-car viewer renders inside — <c>Live Preview</c>'s <c>Min</c> /
    /// <c>Max</c> corners. Required for an unbounded field (a bare TPMS, plane or repeat),
    /// optional otherwise. Reaches <c>dualc_field_view</c> as its <c>--bounds</c> flag and
    /// DualCEditor as the <c>view.bounds</c> key of the graph document
    /// (<see cref="LivePreviewDocument"/>).
    /// </summary>
    public readonly struct ViewBounds
    {
        public double MinX { get; }
        public double MinY { get; }
        public double MinZ { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double MaxZ { get; }

        public ViewBounds(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            MinX = minX; MinY = minY; MinZ = minZ;
            MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        }

        /// <summary>Six finite numbers with <c>min &lt; max</c> on every axis — what both viewers accept.</summary>
        public bool IsValid =>
            IsFinite(MinX) && IsFinite(MinY) && IsFinite(MinZ) &&
            IsFinite(MaxX) && IsFinite(MaxY) && IsFinite(MaxZ) &&
            MinX < MaxX && MinY < MaxY && MinZ < MaxZ;

        /// <summary>The six numbers in order, as every consumer reads them.</summary>
        public double[] ToArray() => new[] { MinX, MinY, MinZ, MaxX, MaxY, MaxZ };

        /// <summary>
        /// The <c>dualc_field_view --bounds</c> argument: <c>x0,y0,z0,x1,y1,z1</c>, invariant
        /// culture, shortest round-trippable doubles.
        /// </summary>
        public string ToFlagValue()
        {
            var ci = CultureInfo.InvariantCulture;
            return string.Join(",", Array.ConvertAll(ToArray(), v => v.ToString("R", ci)));
        }

        private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
