using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Viewport-only proxy preview: dual-contours a <see cref="Volume"/> into a coarse drawable LOD
    /// and draws it directly in the viewport (no bakeable Mesh output). The contour depth is clamped
    /// under a hard ceiling (<see cref="MaxProxyDepth"/>) so it can <em>never</em> OOM Rhino — a
    /// depth ceiling is the only mechanism that prevents the blow-up rather than detecting it after
    /// the mesh is already built (<see cref="DualcField.Contour"/> is atomic). On a library that
    /// <see cref="DualcField.SupportsDiagnostics"/>, a contour with no surface in the sampled region
    /// (the engine's one-triangle placeholder) draws nothing and posts a warning instead; an open
    /// proxy is remarked on. An older DLL takes the plain contour, silently.
    ///
    /// This is deliberately distinct from <see cref="ContourExportComponent"/>: that terminal hands
    /// back a bakeable / exportable proxy mesh; this one is a disposable viewport aid. A coarse
    /// contour of a dense lattice is lossy (thin walls drop out) — true lattice visualization is the
    /// raymarch side-car's job (roadmap Phase 5), not this mesh.
    /// </summary>
    public sealed class ProxyPreviewComponent : GH_Component
    {
        /// <summary>Hard octree-depth ceiling — the OOM safety cap (DualC's default maxDepth).</summary>
        private const int MaxProxyDepth = 7;

        private Mesh? _proxy;
        private BoundingBox _clip = BoundingBox.Empty;

        public ProxyPreviewComponent()
            : base("Proxy preview", "Proxy",
                   "Draw a coarse, depth-capped proxy mesh of a volume in the viewport (preview only, " +
                   "no output). Capped so it can never OOM; not a fidelity surface — use the raymarch side-car for that.",
                   "Boletus", "Terminals")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "Volume to preview.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Depth", "D",
                $"Max octree depth (coarse! clamped to a hard ceiling of {MaxProxyDepth} so the preview can never OOM).",
                GH_ParamAccess.item, 5);
            pManager.AddPointParameter("Min", "Min",
                "Optional bounds minimum corner (needed only for an unbounded field).",
                GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddPointParameter("Max", "Max",
                "Optional bounds maximum corner (needed only for an unbounded field).",
                GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Viewport-only: no outputs. The proxy is drawn in DrawViewportMeshes, never handed
            // downstream (that is ContourExport's job).
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            // Clear any cached mesh up front so a failed/empty solve never leaves a stale draw.
            _proxy = null;
            _clip = BoundingBox.Empty;

            VolumeGoo? goo = null;
            if (!da.GetData(0, ref goo) || goo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }

            int requested = 5;
            da.GetData(1, ref requested);
            int depth = Math.Min(Math.Max(1, requested), MaxProxyDepth);
            if (requested > MaxProxyDepth)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Proxy depth capped at {MaxProxyDepth} (preview is a coarse LOD; use the raymarch side-car for fidelity).");

            Point3d min = default, max = default;
            bool hasMin = da.GetData(2, ref min);
            bool hasMax = da.GetData(3, ref max);
            bool hasBounds = hasMin && hasMax;
            if (hasMin ^ hasMax)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Provide both Min and Max to set bounds; ignoring the single corner (auto-bounds).");

            // Surface validator findings before touching the native engine.
            foreach (ValidationIssue issue in FieldGraphValidator.Validate(goo.Value.Core))
            {
                GH_RuntimeMessageLevel level = issue.Severity == ValidationSeverity.Error
                    ? GH_RuntimeMessageLevel.Error
                    : GH_RuntimeMessageLevel.Warning;
                AddRuntimeMessage(level, $"{issue.Path}: {issue.Message}");
                if (issue.Severity == ValidationSeverity.Error) return;
            }

            DualcField? field = null;
            try
            {
                // Hand any in-memory mesh leaves to DualC in RAM (mesh(id=…) + *_with_meshes); the
                // graph stays meshless and no temp file is written. Same path as Contour/Export.
                var (root, meshes) = VolumeResolver.Resolve(goo.Value);
                string json = root.ToJson();
                field = DualcField.FromJson(json, meshes);

                DualcContourParams prm = DualcContourParams.Default();
                prm.MaxDepth = depth;
                if (hasBounds)
                {
                    prm.HasBounds = true;
                    prm.BoundsMin = (min.X, min.Y, min.Z);
                    prm.BoundsMax = (max.X, max.Y, max.Z);
                }

                // Contour ONCE here and cache; DrawViewportMeshes only draws the cached mesh. With a
                // 0.4.0+ library the diagnostics twin tells a no-surface placeholder from real
                // geometry; an older DLL falls back to the plain call silently (no diagnostics, no
                // message), so the same build runs on both.
                DualcMeshData data;
                DualcDiagnostics? diag = null;
                if (DualcField.SupportsDiagnostics)
                {
                    data = field.Contour(prm, out DualcDiagnostics d);
                    diag = d;
                }
                else
                {
                    data = field.Contour(prm);
                }

                if (diag is { EmptyContour: true })
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "No surface in the sampled region — the volume crosses zero nowhere inside the bounds, " +
                        "so there is nothing to draw. Check Min/Max against the volume's position, or the volume's size.");
                    return;   // _proxy stays null and _clip Empty: the placeholder triangle is never drawn
                }

                _proxy = RhinoMeshConvert.ToRhinoMesh(data);
                _clip = _proxy.GetBoundingBox(false);

                if (diag is { OutputWatertight: false } open)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        $"Proxy mesh is open ({open.OutputBoundaryEdges} boundary, {open.OutputNonManifoldEdges} non-manifold edges) " +
                        "— expected where Min/Max cut through the surface; otherwise the volume itself is open.");
                }
            }
            catch (DualcException ex) when (ex.Code == DualcStatus.Bounds)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Field is unbounded — connect a boundary (e.g. clip via Onion/Boolean) or set Min/Max.");
            }
            catch (DualcException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"DualC error ({ex.Code}): {ex.Message}");
            }
            finally
            {
                field?.Dispose();        // release the native handle
            }
        }

        public override bool IsPreviewCapable => true;

        // So Zoom-Extents frames the proxy even though it is not a data output.
        public override BoundingBox ClippingBox => _clip;

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (_proxy == null) return;
            var material = Attributes.Selected ? args.ShadeMaterial_Selected : args.ShadeMaterial;
            args.Display.DrawMeshShaded(_proxy, material);
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000031");

        protected override Bitmap? Icon => BoletusIcons.ProxyPreview;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
