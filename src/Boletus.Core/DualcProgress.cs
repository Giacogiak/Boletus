namespace Boletus.Core
{
    /// <summary>The coarse stage a progress report belongs to (the ABI's <c>DUALC_STAGE_*</c> values).</summary>
    /// <remarks>
    /// A monolithic call (<see cref="DualcField.Contour(DualcContourParams, System.IProgress{DualcProgress}, System.Threading.CancellationToken)"/>,
    /// <see cref="DualcField.Export(string, DualcContourParams, System.IProgress{DualcProgress}, System.Threading.CancellationToken)"/>)
    /// reports <see cref="Sample"/>, then <see cref="Contour"/>, then — for an export —
    /// <see cref="Write"/> as (0,1)/(1,1); the tiled writer reports <see cref="Tile"/> only.
    /// </remarks>
    public enum DualcStage
    {
        Sample  = 0, // DUALC_STAGE_SAMPLE  -- octree build
        Contour = 1, // DUALC_STAGE_CONTOUR -- QEF solve + traversal
        Write   = 2, // DUALC_STAGE_WRITE   -- monolithic export: (0,1) then (1,1)
        Tile    = 3, // DUALC_STAGE_TILE    -- tiled export: (i, T)
    }

    /// <summary>
    /// One progress report from a <c>*_with_progress</c> call. Within a stage <see cref="Done"/>
    /// is monotonic, <see cref="Total"/> constant and the last report is <c>(Total, Total)</c>;
    /// inside a parallel region reports arrive at most every ~100 ms. Reports are delivered on
    /// the thread that made the native call — never on an engine worker — so a consumer that
    /// updates a UI marshals from there.
    /// </summary>
    public readonly struct DualcProgress
    {
        public DualcStage Stage { get; }
        public uint Done { get; }
        public uint Total { get; }

        public DualcProgress(DualcStage stage, uint done, uint total)
        {
            Stage = stage;
            Done = done;
            Total = total;
        }

        /// <summary><c>Done / Total</c> in <c>[0, 1]</c>; 0 when the stage has no total yet.</summary>
        public double Fraction => Total == 0 ? 0.0 : (double)Done / Total;

        public override string ToString() => $"{Stage} {Done}/{Total}";
    }
}
