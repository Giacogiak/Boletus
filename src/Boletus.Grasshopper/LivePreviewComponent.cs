using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Boletus.Core.LivePreview;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Launches a GPU side-car window as a separate process and points it at the connected
    /// <see cref="Volume"/>: either DualCEditor in exploration mode (<c>editor\DualCEditor.exe</c>
    /// beside the <c>.gha</c>, with its Qt runtime) or DualC's reference viewer
    /// <c>dualc_field_view.exe</c>. Unlike the in-Rhino <see cref="ProxyPreviewComponent"/> (a
    /// coarse, capped LOD mesh that drops thin lattice walls), both sphere-trace the <em>exact</em>
    /// field — RAM bounded by the window, not the lattice.
    ///
    /// Grasshopper is the only input. Each solve serializes the volume to a temp <c>graph.json</c>
    /// (materializing any in-memory mesh leaf to a temp OBJ via <see cref="MeshMaterializingResolver"/>,
    /// since a separate process cannot see RAM buffers), with the <c>Min</c>/<c>Max</c> box as an
    /// optional top-level <c>view</c> key (<see cref="LivePreviewDocument"/>). Both viewers watch
    /// that file and reload on change, keeping their camera — no keypress, no IPC. Nothing flows
    /// back: the editor never writes to the file, and a tweak in its Parameters dock is overwritten
    /// by the next solve's reload. The Rhino camera is not bound.
    ///
    /// The executables are built from the two submodules by <c>scripts/build_native.py</c>, never
    /// committed; <c>BOLETUS_EDITOR_EXE</c> points at a DualCEditor build elsewhere for co-development.
    /// </summary>
    public sealed class LivePreviewComponent : GH_Component
    {
        private Process? _viewer;
        private ViewerKind _runningKind;
        private string? _lastJson;
        private readonly StringBuilder _stderr = new StringBuilder();
        private readonly EventHandler _onProcessExit;
        /// <summary>Set by the editor's Exited handler on a non-zero exit; posted on the next solve.</summary>
        private volatile string? _exitReport;
        /// <summary>Temp OBJs the previous solve stopped referencing; deleted one solve later, once the
        /// viewer's debounce and reload have passed.</summary>
        private string[] _staleObjs = Array.Empty<string>();

        // The editor's two switches are passed once the pinned editor has them (roadmap 12 #42);
        // gated by the pin, never probed at runtime — the version-trap rule.
        private const bool EditorExplore = false;
        private const string? EditorTitle = null;

        public LivePreviewComponent()
            : base("Live Preview", "LivePreview",
                   "Launch an external GPU raymarch window for the connected volume — the exact field, " +
                   "not a coarse proxy. Grasshopper stays the input: the window explores, it never edits. " +
                   "Independent camera. Toggle On to launch/close.",
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

            var viewer = new Param_Integer
            {
                Name = "Viewer",
                NickName = "Vw",
                Description =
                    "Which side-car window to launch: 0 Field view (dualc_field_view — DualC's reference " +
                    "viewer, keyboard controls, bounds at launch only), 1 Editor (DualCEditor — section " +
                    "toolbar, Objects / Parameters / Log docks, bounds live; falls back to Field view when " +
                    "the editor is not shipped beside the .gha).",
            };
            viewer.AddNamedValue("Field view", (int)ViewerKind.FieldView);
            viewer.AddNamedValue("Editor", (int)ViewerKind.Editor);
            viewer.SetPersistentData((int)ViewerKind.FieldView);
            pManager.AddParameter(viewer);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Launcher only: no outputs. The preview is the external window, not a Rhino draw.
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            // A side-car that died since the last solve (the editor cannot start without its Qt
            // DLLs, say) is reported once, here, and not relaunched until an input changes.
            string? exitReport = _exitReport;
            _exitReport = null;
            if (exitReport != null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exitReport);

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
            if (hasMin ^ hasMax)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Provide both Min and Max to set bounds; ignoring the single corner (auto-bounds).");
            ViewBounds? bounds = null;
            if (hasMin && hasMax)
            {
                var b = new ViewBounds(min.X, min.Y, min.Z, max.X, max.Y, max.Z);
                if (b.IsValid) bounds = b;
                else AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Min/Max do not form a box (each Max must exceed its Min); ignoring them (auto-bounds).");
            }

            int viewerIndex = (int)ViewerKind.FieldView;
            da.GetData(4, ref viewerIndex);
            if (viewerIndex != (int)ViewerKind.FieldView && viewerIndex != (int)ViewerKind.Editor)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Unknown viewer {viewerIndex} (expected 0 = Field view, 1 = Editor).");
                return;
            }
            var requested = (ViewerKind)viewerIndex;

            // Surface validator findings before launching anything. An Error leaves a running
            // window on its last good graph (the viewers keep it themselves); On = false closes it.
            foreach (ValidationIssue issue in FieldGraphValidator.Validate(goo.Value.Core))
            {
                GH_RuntimeMessageLevel level = issue.Severity == ValidationSeverity.Error
                    ? GH_RuntimeMessageLevel.Error
                    : GH_RuntimeMessageLevel.Warning;
                AddRuntimeMessage(level, $"{issue.Path}: {issue.Message}");
                if (issue.Severity == ValidationSeverity.Error) return;
            }

            string pluginDir = PluginDir();
            var paths = ViewerLocator.Locate(pluginDir,
                Environment.GetEnvironmentVariable(ViewerLocator.EditorOverrideVariable));
            ViewerChoice? resolved = ViewerLocator.Resolve(requested, paths);
            if (resolved is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "No viewer found beside the .gha (dualc_field_view.exe, or editor\\DualCEditor.exe) — the " +
                    "viewers are built, never committed: run scripts/build_native.py or install the CI .yak.");
                return;
            }
            ViewerChoice choice = resolved.Value;
            if (choice.FellBack)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"DualCEditor not found under {Path.Combine(pluginDir, ViewerLocator.EditorFolderName)}\\ — " +
                    "falling back to dualc_field_view.exe.");

            string tempDir = TempDir();
            string graphPath = Path.Combine(tempDir, "graph.json");

            string json;
            try
            {
                Directory.CreateDirectory(tempDir);
                // A separate process can't read in-memory mesh buffers, so write mesh/winding leaves to
                // temp OBJs and rewrite mem:// → path=. Mesh-free graphs pass through untouched.
                FieldNode root = MeshMaterializingResolver.MaterializeToDisk(goo.Value, tempDir);
                json = LivePreviewDocument.Compose(root, bounds);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to serialize volume: {ex.Message}");
                return;
            }

            // Skip-if-unchanged: only rewrite the graph when it actually changed, so an unrelated canvas
            // solve doesn't churn the file (and, under the file-watch, doesn't recompile). A Min/Max
            // change is a change: the view block is in the text.
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
            SweepStaleObjs(tempDir, json);

            // (Re)launch if the window isn't up (first solve, the user closed it) or the viewer kind
            // changed. When it is up and the graph changed, the atomic rewrite above is enough — the
            // file-watch reloads on its own, no message needed.
            bool alive = _viewer is { HasExited: false };
            if (alive && _runningKind != choice.Kind)
            {
                KillViewer(keepGraph: true);
                alive = false;
            }
            if (!alive)
            {
                if (exitReport != null) return; // reported above; wait for an input change before retrying
                Launch(choice, graphPath, bounds);
            }
        }

        private void Launch(ViewerChoice choice, string graphPath, ViewBounds? bounds)
        {
            bool editor = choice.Kind == ViewerKind.Editor;
            var psi = new ProcessStartInfo(choice.Exe)
            {
                UseShellExecute = false,
                RedirectStandardError = true,   // drained async below so the child never blocks on a full pipe
                // The field view's console carries its live control echoes ('selected param',
                // 'wavelength = …', section-plane state, reload confirmations) worth seeing; the
                // editor is a GUI application with a Log dock and gets no console.
                CreateNoWindow = editor,
            };
            foreach (string a in ViewerArguments.Build(choice.Kind, graphPath, bounds, EditorTitle, EditorExplore))
                psi.ArgumentList.Add(a);

            try
            {
                lock (_stderr) _stderr.Clear();
                var p = new Process { StartInfo = psi };
                p.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data == null) return;
                    lock (_stderr) { if (_stderr.Length < 4000) _stderr.AppendLine(e.Data); }
                };
                if (editor)
                {
                    // The editor opens its window even on a graph it cannot load (the error goes to
                    // its status bar and Log) and never exits by itself, so an exit is a launch
                    // failure — a missing Qt DLL, typically. No blocking wait: Qt start-up can take
                    // longer than a solve should; the exit is reported on the next solve instead.
                    p.EnableRaisingEvents = true;
                    p.Exited += (_, __) => OnEditorExited(p);
                }
                p.Start();
                p.BeginErrorReadLine();
                _viewer = p;
                _runningKind = choice.Kind;

                if (!editor)
                {
                    // If it dies immediately (parse error, or an infinite field with no bounds), report why.
                    // A healthy viewer never exits, so this blocks the solve thread the full timeout on every
                    // successful launch — kept short (launch is infrequent; ~¼s freeze is acceptable).
                    if (p.WaitForExit(250) && p.ExitCode != 0)
                    {
                        string err;
                        lock (_stderr) err = _stderr.ToString().Trim();
                        if (err.Length == 0)
                            err = "the field may be unbounded — set Min/Max (a bare TPMS/plane/repeat has no finite bounds).";
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Viewer exited immediately: {err}");
                        p.Dispose();
                        _viewer = null;
                        return;
                    }
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Side-car viewer running — updates live as you edit the graph. " +
                        "Orbit: left-drag · dolly: scroll · Esc quit.");
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Explorer running — updates live as you edit the graph; parameter tweaks in its dock " +
                        "reset on the next solve. Orbit: left-drag · dolly: scroll · F frame.");
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to launch viewer: {ex.Message}");
                _viewer = null;
            }
        }

        /// <summary>The editor's Exited handler (a thread-pool thread): a non-zero exit is kept for the
        /// next solve, which is scheduled from the UI thread; a clean close (code 0) is the user's.</summary>
        private void OnEditorExited(Process p)
        {
            int code;
            try { code = p.ExitCode; } catch { return; }
            if (code == 0) return;
            string err;
            lock (_stderr) err = _stderr.ToString().Trim();
            if (err.Length == 0)
                err = "no message — a missing Qt DLL beside DualCEditor.exe is the usual cause; rebuild with scripts/build_native.py.";
            _exitReport = $"Viewer exited (code 0x{code:X}): {err}";
            try
            {
                RhinoApp.InvokeOnUiThread((Action)(() =>
                    OnPingDocument()?.ScheduleSolution(50, _ => ExpireSolution(false))));
            }
            catch { /* Rhino shutting down */ }
        }

        /// <summary>Delete the OBJs the previous solve stopped referencing (and this one still does
        /// not), then remember this solve's unreferenced ones for the next. One solve of delay keeps
        /// a file alive across the viewer's debounce and reload, which re-reads meshes.</summary>
        private void SweepStaleObjs(string tempDir, string json)
        {
            foreach (string f in _staleObjs)
            {
                if (json.Contains(f.Replace('\\', '/'))) continue;
                try { File.Delete(f); } catch { /* best effort; retried next solve if still stale */ }
            }
            try
            {
                var stale = new System.Collections.Generic.List<string>();
                foreach (string f in Directory.GetFiles(tempDir, "mesh_*.obj"))
                    if (!json.Contains(f.Replace('\\', '/'))) stale.Add(f);
                _staleObjs = stale.ToArray();
            }
            catch { _staleObjs = Array.Empty<string>(); }
        }

        private void KillViewer(bool keepGraph = false)
        {
            var p = _viewer;
            _viewer = null;
            if (!keepGraph) { _lastJson = null; _staleObjs = Array.Empty<string>(); }
            if (p == null) return;
            try { p.EnableRaisingEvents = false; } catch { /* already gone */ }   // our own kill is not a failure to report
            try { if (!p.HasExited) p.Kill(); } catch { /* already gone */ }
            finally { p.Dispose(); }
        }

        /// <summary>The viewers sit beside the .gha (csproj copy items), found via the assembly's own
        /// directory — the same idiom <see cref="BoletusPriority"/> uses to locate the native DLL.</summary>
        private static string PluginDir() =>
            Path.GetDirectoryName(typeof(LivePreviewComponent).Assembly.Location) ?? string.Empty;

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
