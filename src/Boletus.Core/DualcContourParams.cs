namespace Boletus.Core
{
    /// <summary>
    /// Idiomatic sampling/contour settings (the <c>dualc_field</c> CLI flags).
    /// Always start from <see cref="Default"/> then override — notably
    /// <see cref="MaxDepth"/>, which is the proxy lever (coarse = cheap drawable
    /// proxy; full = export-grade). A coarse proxy of a <em>lattice</em> is lossy
    /// (thin walls drop out).
    /// </summary>
    public struct DualcContourParams
    {
        /// <summary>Octree max depth — the proxy lever. Cost ~4–8× per level.</summary>
        public int MaxDepth;
        public int MinDepth;
        /// <summary>Adaptive-collapse QEF error (0 = off). Incompatible with tiled STL export.</summary>
        public double Collapse;
        /// <summary>When true, <see cref="BoundsMin"/>/<see cref="BoundsMax"/> are used;
        /// required for an unbounded field (bare TPMS, plane, infinite primitive, repeat).</summary>
        public bool HasBounds;
        public (double X, double Y, double Z) BoundsMin;
        public (double X, double Y, double Z) BoundsMax;
        /// <summary>Manifold dual contouring (default on).</summary>
        public bool Manifold;
        /// <summary>Worker threads; 0 = all hardware threads.</summary>
        public uint NumThreads;

        /// <summary>Library defaults (maxDepth 7, minDepth 3, manifold on, auto bounds).</summary>
        public static DualcContourParams Default()
        {
            NativeMethods.dualc_default_params(out var n);
            return new DualcContourParams
            {
                MaxDepth   = n.maxDepth,
                MinDepth   = n.minDepth,
                Collapse   = n.collapse,
                HasBounds  = n.hasBounds != 0,
                Manifold   = n.manifold != 0,
                NumThreads = n.numThreads,
                BoundsMin  = (n.bMinX, n.bMinY, n.bMinZ),
                BoundsMax  = (n.bMaxX, n.bMaxY, n.bMaxZ),
            };
        }

        internal ContourParamsNative ToNative() => new ContourParamsNative
        {
            maxDepth   = MaxDepth,
            minDepth   = MinDepth,
            collapse   = Collapse,
            hasBounds  = HasBounds ? 1 : 0,
            manifold   = Manifold ? 1 : 0,
            numThreads = NumThreads,
            bMinX = BoundsMin.X, bMinY = BoundsMin.Y, bMinZ = BoundsMin.Z,
            bMaxX = BoundsMax.X, bMaxY = BoundsMax.Y, bMaxZ = BoundsMax.Z,
        };
    }
}
