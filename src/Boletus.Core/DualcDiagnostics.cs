namespace Boletus.Core
{
    /// <summary>
    /// What the engine degraded silently during a contour or an export, reported instead of
    /// guessed (the ABI 0.4.0 <c>DualcDiagnostics</c>). Returned by the <c>out DualcDiagnostics</c>
    /// overloads of <see cref="DualcField"/>, which exist only on a library that
    /// <see cref="DualcField.SupportsDiagnostics"/> reports.
    /// </summary>
    /// <remarks>
    /// <para>A field with no surface inside the sampled region contours to <c>DUALC_OK</c> and a
    /// single placeholder triangle <c>(0,0,0) (1,0,0) (0,1,0)</c>, because the engine's mesh
    /// library rejects an empty polygon list. <see cref="EmptyContour"/> is the only way to tell
    /// that placeholder from a real one-triangle result; its counts then read 3 vertices, 1
    /// triangle, 3 boundary edges, not watertight.</para>
    /// <para>The <c>Input*</c> fields describe a mesh handed to the mesh entry points of the ABI.
    /// Through the field entry points Boletus binds they are never inspected and keep the
    /// engine's defaults: <see cref="InputEmpty"/> false, both counts 0, <see cref="InputWatertight"/>
    /// true.</para>
    /// <para>The struct is only ever produced from a call that returned OK: on a cancelled or
    /// failed call the overloads throw before constructing it, so a caller never sees the zeroed
    /// native struct (which would read "not watertight").</para>
    /// </remarks>
    public readonly struct DualcDiagnostics
    {
        /// <summary>The input mesh had no triangles (mesh entry points only).</summary>
        public bool InputEmpty { get; }

        /// <summary>Input edges with exactly one incident triangle (mesh entry points only).</summary>
        public ulong InputBoundaryEdges { get; }

        /// <summary>Input edges with three or more incident triangles (mesh entry points only).</summary>
        public ulong InputNonManifoldEdges { get; }

        /// <summary>Both input edge counts are zero (true through the field entry points).</summary>
        public bool InputWatertight { get; }

        /// <summary>The field's own bounds were unusable, so the unit cube was sampled.</summary>
        public bool BoundsFallback { get; }

        /// <summary>Explicit bounds sampled outside a root-level baked grid.</summary>
        public bool GridBoundsExceeded { get; }

        /// <summary>No surface in the sampled region: the result is the one-triangle placeholder.</summary>
        public bool EmptyContour { get; }

        /// <summary>Vertices of the output mesh (equal to the returned mesh's count).</summary>
        public ulong OutputVertices { get; }

        /// <summary>Triangles of the output mesh (equal to the returned mesh's count).</summary>
        public ulong OutputTriangles { get; }

        /// <summary>Output edges with exactly one incident triangle — an open mesh.</summary>
        public ulong OutputBoundaryEdges { get; }

        /// <summary>Output edges with three or more incident triangles.</summary>
        public ulong OutputNonManifoldEdges { get; }

        /// <summary>Both output edge counts are zero.</summary>
        public bool OutputWatertight { get; }

        /// <summary>
        /// The single flag a host can surface: <see cref="InputEmpty"/> or <see cref="BoundsFallback"/>
        /// or <see cref="GridBoundsExceeded"/> or <see cref="EmptyContour"/> or not
        /// <see cref="OutputWatertight"/>.
        /// </summary>
        public bool AnyIssue { get; }

        internal DualcDiagnostics(in DualcDiagnosticsNative n)
        {
            InputEmpty            = n.inputEmpty != 0;
            InputBoundaryEdges    = n.inputBoundaryEdges;
            InputNonManifoldEdges = n.inputNonManifoldEdges;
            InputWatertight       = n.inputWatertight != 0;
            BoundsFallback        = n.boundsFallback != 0;
            GridBoundsExceeded    = n.gridBoundsExceeded != 0;
            EmptyContour          = n.emptyContour != 0;
            OutputVertices        = n.outputVertices;
            OutputTriangles       = n.outputTriangles;
            OutputBoundaryEdges   = n.outputBoundaryEdges;
            OutputNonManifoldEdges = n.outputNonManifoldEdges;
            OutputWatertight      = n.outputWatertight != 0;
            AnyIssue              = n.anyIssue != 0;
        }

        public override string ToString() =>
            $"V={OutputVertices} T={OutputTriangles} boundary={OutputBoundaryEdges} " +
            $"nonManifold={OutputNonManifoldEdges} watertight={OutputWatertight} " +
            $"empty={EmptyContour} anyIssue={AnyIssue}";
    }
}
