using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Launches DualC's standalone GPU raymarch viewer (<c>dualc_field_view.exe</c>) as a separate
    /// side-car process and points it at the connected <see cref="Volume"/>. Unlike the in-Rhino
    /// <see cref="ProxyPreviewComponent"/> (a coarse, capped LOD mesh that drops thin lattice walls),
    /// the viewer sphere-traces the <em>exact</em> field — RAM bounded by the window, not the lattice —
    /// so it is the true-fidelity preview the roadmap reserves for Phase 5.
    ///
    /// The viewer is a separate process fed a <b>file</b>, so each solve serializes the volume to a
    /// temp <c>.json</c> (materializing any in-memory mesh leaf to a temp OBJ via
    /// <see cref="MeshMaterializingResolver"/>, since a separate process can't see RAM buffers). The
    /// camera is the viewer's own independent orbit/dolly — Rhino's camera is not bound.
    ///
    /// The vendored viewer <b>watches the temp file's mtime</b> (DualC Phase-5b file-watch), so
    /// rewriting it each solve makes the window update on its own — no keypress. Inside the window
    /// <c>l</c> still forces a manual reload.
    /// </summary>
    public sealed class LivePreviewComponent : GH_Component
    {
        private Process? _viewer;
        private string? _lastJson;
        private readonly StringBuilder _stderr = new StringBuilder();
        private readonly EventHandler _onProcessExit;

        public LivePreviewComponent()
            : base("Live Preview", "LivePreview",
                   "Launch an external GPU raymarch viewport for the connected volume — the exact field, " +
                   "not a coarse proxy. Independent camera (orbit/dolly). Toggle On to launch/close.",
                   "Boletus", "Terminals")
        {
            // Best-effort: don't leave an orphaned viewer window if Rhino exits.
            _onProcessExit = (_, __) => KillViewer();
            AppDomain.CurrentDomain.ProcessExit += _onProcessExit;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "Volume to preview in the raymarch side-car.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("On", "On",
                "Launch/keep the side-car window open (true) or close it (false). Off by default so the " +
                "window opens only when you ask.", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Min", "Min",
                "Optional bounds minimum corner. Required for an unbounded field (a bare TPMS / plane / repeat).",
                GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddPointParameter("Max", "Max",
                "Optional bounds maximum corner. Required for an unbounded field (a bare TPMS / plane / repeat).",
                GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Launcher only: no outputs. The preview is the external window, not a Rhino draw.
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? goo = null;
            if (!da.GetData(0, ref goo) || goo?.Value?.Core is null)
            {
                KillViewer();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }

            bool on = false;
            da.GetData(1, ref on);
            if (!on)
            {
                KillViewer();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Off — connect a volume and set On to launch the viewer.");
                return;
            }

            Point3d min = default, max = default;
            bool hasMin = da.GetData(2, ref min);
            bool hasMax = da.GetData(3, ref max);
            bool hasBounds = hasMin && hasMax;
            if (hasMin ^ hasMax)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Provide both Min and Max to set bounds; ignoring the single corner (auto-bounds).");

            // Surface validator findings before launching anything.
            foreach (ValidationIssue issue in FieldGraphValidator.Validate(goo.Value.Core))
            {
                GH_RuntimeMessageLevel level = issue.Severity == ValidationSeverity.Error
                    ? GH_RuntimeMessageLevel.Error
                    : GH_RuntimeMessageLevel.Warning;
                AddRuntimeMessage(level, $"{issue.Path}: {issue.Message}");
                if (issue.Severity == ValidationSeverity.Error) return;
            }

            string exe = ViewerExePath();
            if (!File.Exists(exe))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "dualc_field_view.exe not found next to the .gha — the viewer is built, never committed: run scripts/build_native.py or install the CI .yak.");
                return;
            }

            string tempDir = TempDir();
            string graphPath = Path.Combine(tempDir, "graph.json");

            string json;
            try
            {
                Directory.CreateDirectory(tempDir);
                // A separate process can't read in-memory mesh buffers, so write mesh/winding leaves to
                // temp OBJs and rewrite mem:// → path=. Mesh-free graphs pass through untouched.
                FieldNode root = MeshMaterializingResolver.MaterializeToDisk(goo.Value, tempDir);
                json = root.ToJson();
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to serialize volume: {ex.Message}");
                return;
            }

            // Skip-if-unchanged: only rewrite the graph when it actually changed, so an unrelated canvas
            // solve doesn't churn the file (and, under the Phase-5b file-watch, doesn't recompile).
            bool changed = json != _lastJson;
            if (changed)
            {
                try
                {
                    string tmp = graphPath + ".tmp";
                    File.WriteAllText(tmp, json, new UTF8Encoding(false));
                    File.Move(tmp, graphPath, overwrite: true); // atomic swap — the watcher never sees a half-written file
                    _lastJson = json;
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to write graph file: {ex.Message}");
                    return;
                }
            }

            // (Re)launch if the window isn't up (first solve, or the user closed it). When it is up and
            // the graph changed, the atomic rewrite above is enough — the viewer's file-watch reloads
            // on its own, no message needed.
            bool alive = _viewer is { HasExited: false };
            if (!alive)
                Launch(exe, graphPath, hasBounds, min, max);
        }

        private void Launch(string exe, string graphPath, bool hasBounds, Point3d min, Point3d max)
        {
            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                RedirectStandardError = true,   // drained async below so the child never blocks on a full pipe
                // Intentional: the viewer's console carries the live control echoes ('selected param',
                // 'wavelength = …', section-plane state, reload confirmations) worth seeing.
                CreateNoWindow = false,
            };
            psi.ArgumentList.Add(graphPath);
            if (hasBounds)
            {
                var ci = CultureInfo.InvariantCulture;
                psi.ArgumentList.Add("--bounds");
                psi.ArgumentList.Add(string.Format(ci, "{0},{1},{2},{3},{4},{5}",
                    min.X, min.Y, min.Z, max.X, max.Y, max.Z));
            }

            try
            {
                lock (_stderr) _stderr.Clear();
                _viewer = new Process { StartInfo = psi };
                _viewer.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data == null) return;
                    lock (_stderr) { if (_stderr.Length < 4000) _stderr.AppendLine(e.Data); }
                };
                _viewer.Start();
                _viewer.BeginErrorReadLine();

                // If it dies immediately (parse error, or an infinite field with no bounds), report why.
                // A healthy viewer never exits, so this blocks the solve thread the full timeout on every
                // successful launch — kept short (launch is infrequent; ~¼s freeze is acceptable).
                if (_viewer.WaitForExit(250) && _viewer.ExitCode != 0)
                {
                    string err;
                    lock (_stderr) err = _stderr.ToString().Trim();
                    if (err.Length == 0)
                        err = "the field may be unbounded — set Min/Max (a bare TPMS/plane/repeat has no finite bounds).";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Viewer exited immediately: {err}");
                    _viewer.Dispose();
                    _viewer = null;
                    return;
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Side-car viewer running — updates live as you edit the graph. " +
                    "Orbit: left-drag · dolly: scroll · Esc quit.");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to launch viewer: {ex.Message}");
                _viewer = null;
            }
        }

        private void KillViewer()
        {
            var p = _viewer;
            _viewer = null;
            _lastJson = null;
            if (p == null) return;
            try { if (!p.HasExited) p.Kill(); } catch { /* already gone */ }
            finally { p.Dispose(); }
        }

        /// <summary>The viewer sits next to the .gha (a csproj copy item), found via the assembly's own
        /// directory — the same idiom <see cref="BoletusPriority"/> uses to locate the native DLL.</summary>
        private static string ViewerExePath()
        {
            string? dir = Path.GetDirectoryName(typeof(LivePreviewComponent).Assembly.Location);
            return Path.Combine(dir ?? string.Empty, "dualc_field_view.exe");
        }

        /// <summary>A per-instance temp dir (keyed by the component's InstanceGuid) so the viewer watches
        /// one stable path and multiple Live Preview components never collide.</summary>
        private string TempDir() =>
            Path.Combine(Path.GetTempPath(), "Boletus", "LivePreview", InstanceGuid.ToString("N"));

        public override void RemovedFromDocument(GH_Document document)
        {
            KillViewer();
            AppDomain.CurrentDomain.ProcessExit -= _onProcessExit;
            try { if (Directory.Exists(TempDir())) Directory.Delete(TempDir(), recursive: true); }
            catch { /* best-effort temp cleanup */ }
            base.RemovedFromDocument(document);
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000032");

        protected override Bitmap? Icon => BoletusIcons.LivePreview;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
