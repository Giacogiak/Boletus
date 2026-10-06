using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Boletus.Core;
using Boletus.Core.Export;
using Boletus.Core.FieldGraph;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.Geometry;
using Timer = System.Windows.Forms.Timer;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Terminal exporter: writes a <see cref="Volume"/> to an STL/3MF file on disk. Unlike a normal
    /// component this <b>never writes on auto-solve</b> — the write is launched only by the on-canvas
    /// <em>Write</em> button, and it runs on a background thread so a heavy part (minutes of tiled-STL
    /// streaming) does not freeze the Grasshopper UI. While a write is in flight the component shows a
    /// live "Writing… …" status with the engine's progress; if the definition changes underneath it,
    /// it reports <b>BUSY</b> and refuses to auto-restart.
    ///
    /// With a DualC library at ABI 0.5.0 or later (<see cref="DualcField.SupportsProgress"/>) the
    /// button reads <em>Cancel ■</em> while running and stops the write cooperatively at the engine's
    /// next checkpoint — nothing is left at the path (DualC writes <c>.part</c> and renames only on
    /// success) — and the label carries a real percentage. With an older library the component
    /// degrades to the Phase-A behaviour: an indeterminate "Writing… Ns", and a click while running
    /// queues one restart that fires on completion (the native call cannot be interrupted).
    ///
    /// Preview is intentionally <em>not</em> this component's job — use <see cref="ProxyPreviewComponent"/>
    /// or <see cref="LivePreviewComponent"/> for that. Here the only outputs are a status string and the
    /// written file path. The record is <c>docs/roadmap/05-phase3-grasshopper-components/06-write-to-file.md</c>.
    /// </summary>
    public sealed class WriteToFileComponent : GH_Component
    {
        internal enum RunState { Idle, Running, Done, Failed, Cancelled }

        private readonly object _gate = new object();
        private RunState _state = RunState.Idle;
        private Task? _writeTask;
        private string? _runSnapshotHash;     // input hash captured when the running write launched
        private bool _runRequested;           // set by the Write button (Idle → launch)
        private bool _restartRequested;       // old-library path only: Write clicked while running → relaunch on completion
        private bool _cancelRequested;        // 0.5.0 path: Cancel clicked while running
        private CancellationTokenSource? _cts; // the running write's cancel source (0.5.0 path)
        private volatile string? _progress;   // the engine's last progress report, formatted; written on the worker
        private string? _resultInfo;
        private string? _resultFile;
        private string? _errorMsg;
        private string? _resultWarning;       // the engine's empty-contour advisory from the last completed write
        private DateTime _startedUtc;
        private Timer? _tick;                 // UI-thread ticker for the running label
        private volatile bool _removed;

        public WriteToFileComponent()
            : base("Write to File", "Write",
                   "Export a volume to an STL or 3MF mesh file on disk.\n\n" +
                   "This terminal is ON-DEMAND and ASYNCHRONOUS — it does NOT write automatically. " +
                   "Click the 'Write ▶' button on the component to launch; the write runs on a " +
                   "background thread so Grasshopper stays responsive even on a heavy part that takes " +
                   "minutes. The button and the 'Info' output show the progress while it runs.\n\n" +
                   "While a write runs the button reads 'Cancel ■': click it to stop the write at the " +
                   "engine's next checkpoint — nothing is left on disk (a file already at the path is " +
                   "untouched). If you change the definition while a write is running, the component " +
                   "reports BUSY and keeps writing the OLD geometry (it never auto-restarts): cancel, " +
                   "then click 'Write ▶' again.\n\n" +
                   "With a DualC library older than ABI 0.5.0 a running write cannot be stopped and " +
                   "always runs to completion; a click while running then queues ONE restart with the " +
                   "new inputs.\n\n" +
                   "No mesh is output — use 'Proxy preview' or 'Live Preview' to see the volume.",
                   "Boletus", "Terminals")
        {
        }

        public override void CreateAttributes() => Attributes = new WriteToFileAttributes(this);

        internal bool IsBusy
        {
            get { lock (_gate) { return _state == RunState.Running; } }
        }

        internal string ButtonLabel
        {
            get
            {
                lock (_gate)
                {
                    if (_state != RunState.Running) return "Write ▶";
                    if (!DualcField.SupportsProgress) return "Writing…";
                    return _cancelRequested ? "Cancelling…" : "Cancel ■";
                }
            }
        }

        /// <summary>Called by the on-canvas button. A click while idle launches on the next solve; a
        /// click while running cancels the write (ABI 0.5.0) or, with an older library, queues a
        /// restart (single-flight — never two concurrent native writes).</summary>
        public void RequestRun()
        {
            bool expire;
            lock (_gate)
            {
                if (_state == RunState.Running)
                {
                    if (DualcField.SupportsProgress)
                    {
                        if (!_cancelRequested)
                        {
                            _cancelRequested = true;
                            _cts?.Cancel();          // forwards into the native token from this thread
                            Message = "Cancelling…";
                        }
                        expire = false;              // the completion callback schedules the solve
                    }
                    else
                    {
                        _restartRequested = true;
                        expire = true;
                    }
                }
                else
                {
                    _runRequested = true;
                    expire = true;
                }
            }
            if (expire) ExpireSolution(true);
            else global::Grasshopper.Instances.RedrawCanvas();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "The volume (field graph) to dual-contour and write to disk. This is the only geometry " +
                "input; any imported mesh leaves inside it are streamed to DualC in memory (no temp file).",
                GH_ParamAccess.item);
            pManager.AddIntegerParameter("Depth", "D",
                "Maximum octree depth of the dual-contour (mesh resolution). Higher = finer mesh but " +
                "exponentially more triangles, memory and time on a dense lattice. 6 is the recommended " +
                "default; above 8 the component warns. Keep it coarse unless you truly need the detail.",
                GH_ParamAccess.item, 6);
            pManager.AddTextParameter("Path", "P",
                "REQUIRED. Output file path. A bare name (no extension) is fine — the extension is set " +
                "automatically from the Format input, and a conflicting typed extension is replaced (with " +
                "a warning). The parent folder must already exist. Example: 'C:\\tmp\\lattice' with " +
                "Format = 0 writes 'C:\\tmp\\lattice.stl'.",
                GH_ParamAccess.item);
            pManager[2].Optional = false;   // mandatory
            pManager.AddIntegerParameter("Format", "F",
                "Output file format (integer):\n" +
                "  0 = STL  (binary; compact, widely supported — the default)\n" +
                "  1 = 3MF  (modern container; 1 unit = 1 mm)\n" +
                "This selector, not the typed Path extension, decides the file's extension.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Mode", "Md",
                "Write strategy (integer):\n" +
                "  0 = Tiled       (streams the mesh to disk tile-by-tile; peak RAM ≈ one tile, so a " +
                "deep export can't run you out of memory — best for heavy lattices. STL only.)\n" +
                "  1 = Monolithic  (builds the whole mesh in RAM, then writes it once.)\n" +
                "3MF has no tiled writer, so 3MF is always Monolithic (Tiled + 3MF warns and falls back).",
                GH_ParamAccess.item, 0);
            pManager.AddPointParameter("Min", "Min",
                "OPTIONAL bounds minimum corner. Only needed for an UNBOUNDED field (a bare TPMS / plane / " +
                "repeat with no boundary clip); a finite field auto-fits its bounds. Provide BOTH Min and " +
                "Max or neither — a single corner is ignored.",
                GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager.AddPointParameter("Max", "Max",
                "OPTIONAL bounds maximum corner. See Min — provide both or neither. Only needed for an " +
                "unbounded field.",
                GH_ParamAccess.item);
            pManager[6].Optional = true;
            // The two selectors carry their options as named values too (the dropdown convention
            // every type/kind input follows), not only in the Description.
            var fmt = (Param_Integer)pManager[3];
            fmt.AddNamedValue("STL", 0);
            fmt.AddNamedValue("3MF", 1);
            var md = (Param_Integer)pManager[4];
            md.AddNamedValue("Tiled", 0);
            md.AddNamedValue("Monolithic", 1);
            pManager.AddIntegerParameter("Tile depth", "T",
                "OPTIONAL. Sub-grid depth for the Tiled STL writer (used only when Format = 0 and " +
                "Mode = 0; ignored otherwise). Default = Depth − 2. Lower = smaller tiles = less peak RAM " +
                "but more CPU; ≥4 is recommended. If Tile depth ≥ Depth the tiling degenerates to a " +
                "single pass (no RAM benefit) and the component warns.",
                GH_ParamAccess.item);
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Info", "I",
                "Human-readable status of the component: 'Idle — click Write to export.', " +
                "'Writing… tile 3/27 (11%) · 12s' (or 'sampling 40%', 'contouring 80%', 'writing file' " +
                "for a monolithic write; 'Writing… Ns' before the first report or with a library older " +
                "than ABI 0.5.0), 'Cancelling… …' after a Cancel click, 'Done → file.stl (… MB, …s)' on " +
                "success, 'Cancelled — nothing written; file.stl untouched.' after a cancel, 'Inputs " +
                "changed since the last write (…). Click Write to re-export.', or the failure text " +
                "('Failed (unbounded field)' / 'Failed'; the error itself is posted as a message). " +
                "A definition change mid-write is reported as a BUSY warning, not here.",
                GH_ParamAccess.item);
            pManager.AddTextParameter("File", "F",
                "Resolved absolute path of the written file (with the extension applied). Populated only " +
                "after a successful write; empty while idle, writing, or after a cancel or a failure.",
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? goo = null;
            if (!da.GetData(0, ref goo) || goo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }

            bool canCancel = DualcField.SupportsProgress;
            if (!canCancel)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "The vendored DualC library predates ABI 0.5.0: no progress or cancel — a running " +
                    "write always finishes; a Write click while running queues one restart.");

            int depth = 6;
            da.GetData(1, ref depth);
            if (depth > 8)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Depth {depth} on a lattice can be very expensive (exponential per level).");

            string? path = null;
            da.GetData(2, ref path);
            int format = 0; da.GetData(3, ref format);
            int mode = 0; da.GetData(4, ref mode);

            Point3d min = default, max = default;
            bool hasMin = da.GetData(5, ref min);
            bool hasMax = da.GetData(6, ref max);
            bool hasBounds = hasMin && hasMax;
            if (hasMin ^ hasMax)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Provide both Min and Max to set bounds; ignoring the single corner (auto-bounds).");

            int tileInput = int.MinValue;
            bool hasTile = da.GetData(7, ref tileInput);

            // Surface validator findings before touching the engine (same gate as before).
            foreach (ValidationIssue issue in FieldGraphValidator.Validate(goo.Value.Core))
            {
                GH_RuntimeMessageLevel level = issue.Severity == ValidationSeverity.Error
                    ? GH_RuntimeMessageLevel.Error
                    : GH_RuntimeMessageLevel.Warning;
                AddRuntimeMessage(level, $"{issue.Path}: {issue.Message}");
                if (issue.Severity == ValidationSeverity.Error) return;
            }

            // Plan the file target (extension from Format, tiled/monolithic strategy). Pure + native-free.
            ExportPlanResult plan;
            try
            {
                plan = ExportPlan.Resolve(path, format, mode, depth, hasTile ? tileInput : (int?)null);
            }
            catch (ArgumentException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                return;
            }
            foreach (string w in plan.Warnings)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, w);

            // Resolve the graph diskless (mem:// → id=) and serialize now — cheap, and needed for the
            // change-detection hash and the worker payload.
            string json;
            IReadOnlyDictionary<string, MeshBuffer> meshes;
            try
            {
                var (root, m) = VolumeResolver.Resolve(goo.Value);
                json = root.ToJson();
                meshes = m;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to serialize volume: {ex.Message}");
                return;
            }

            string inputHash = InputHash(json, depth, plan, hasBounds, min, max);

            bool launch = false;
            lock (_gate)
            {
                if (_state == RunState.Running)
                {
                    // Old-library path: a double-click (or a Write click with no changes) queued a
                    // restart for inputs identical to what's already writing — drop it so it doesn't
                    // re-export needlessly.
                    if (_restartRequested && inputHash == _runSnapshotHash)
                        _restartRequested = false;

                    if (inputHash != _runSnapshotHash)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, canCancel
                            ? "BUSY — a write is running with older inputs. Click Cancel ■ to stop it, " +
                              "then Write ▶ to re-export with the new inputs, or wait for it to finish."
                            : "BUSY — a write is running with older inputs. Click Write to queue a restart " +
                              "with the new inputs, or wait for it to finish.");
                    EmitRunningStatus(da);
                    return;                               // never launch a second write on auto-solve
                }

                if (_runRequested)
                {
                    _runRequested = false;
                    _runSnapshotHash = inputHash;
                    _state = RunState.Running;
                    _startedUtc = DateTime.UtcNow;
                    _cancelRequested = false;
                    _progress = null;
                    _cts = canCancel ? new CancellationTokenSource() : null;
                    launch = true;
                }
            }

            if (launch)
            {
                StartTicker();
                Message = "Writing…";
                LaunchWrite(json, meshes, plan, BuildParams(depth, hasBounds, min, max));
                EmitRunningStatus(da);
                return;
            }

            // Idle / done / failed / cancelled: publish the last result.
            lock (_gate)
            {
                switch (_state)
                {
                    case RunState.Done:
                        // If the definition changed since the write, don't imply the current volume is
                        // on disk — the file still holds what was written at launch time.
                        if (inputHash != _runSnapshotHash)
                        {
                            Message = "Ready";
                            da.SetData(0, $"Inputs changed since the last write ({Path.GetFileName(_resultFile)}). Click Write to re-export.");
                        }
                        else
                        {
                            Message = "Done";
                            da.SetData(0, _resultInfo);
                        }
                        // The file on disk holds the placeholder either way, so the advisory stands.
                        if (_resultWarning != null)
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, _resultWarning);
                        da.SetData(1, _resultFile);
                        break;
                    case RunState.Failed:
                        Message = "Failed";
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _errorMsg ?? "Export failed.");
                        da.SetData(0, _resultInfo ?? "Failed");
                        break;
                    case RunState.Cancelled:
                        Message = "Cancelled";
                        da.SetData(0, _resultInfo ?? "Cancelled");
                        break;
                    default:
                        Message = null;
                        da.SetData(0, "Idle — click Write to export.");
                        break;
                }
            }
        }

        /// <summary>Runs the whole native field lifetime on a worker thread (create → export →
        /// dispose — the single-threaded-handle rule), then republishes on the UI thread. With a
        /// 0.5.0 library the export takes the running write's cancel token and a progress sink that
        /// formats the engine's reports into <see cref="_progress"/> (delivered on this worker — the
        /// thread that made the P/Invoke — and read by the UI-thread ticker).</summary>
        private void LaunchWrite(string json, IReadOnlyDictionary<string, MeshBuffer> meshes,
            ExportPlanResult plan, DualcContourParams prm)
        {
            CancellationTokenSource? cts;
            lock (_gate) { cts = _cts; }

            // A dedicated long-running thread, not a pool thread: the native write blocks for minutes,
            // and hogging a ThreadPool thread that long can starve other Rhino/GH async work.
            _writeTask = Task.Factory.StartNew(() =>
            {
                string info;
                string? file = null;
                string? err = null;
                string? warn = null;
                bool ok = false;
                bool cancelled = false;
                var sw = Stopwatch.StartNew();
                DualcField? field = null;
                try
                {
                    field = DualcField.FromJson(json, meshes);
                    // The monolithic paths take the engine's diagnostics (ABI 0.4.0+) so an empty
                    // contour — a file holding one placeholder triangle — is reported, not "Done".
                    // The tiled path has no diagnostics in the ABI; an old DLL has none at all.
                    DualcDiagnostics? diag = null;
                    if (cts != null)
                    {
                        var sink = new ProgressSink(p => _progress = FormatProgress(p));
                        if (plan.Strategy == ExportStrategy.TiledStl)
                        {
                            field.ExportTiledStl(plan.Path, prm, plan.TileDepth, sink, cts.Token);
                        }
                        else
                        {
                            field.Export(plan.Path, prm, sink, cts.Token, out DualcDiagnostics d);
                            diag = d;
                        }
                    }
                    else
                    {
                        if (plan.Strategy == ExportStrategy.TiledStl)
                        {
                            field.ExportTiledStl(plan.Path, prm, plan.TileDepth);
                        }
                        else if (DualcField.SupportsDiagnostics)
                        {
                            field.Export(plan.Path, prm, out DualcDiagnostics d);
                            diag = d;
                        }
                        else
                        {
                            field.Export(plan.Path, prm);
                        }
                    }
                    sw.Stop();
                    double mb = new FileInfo(plan.Path).Length / 1024.0 / 1024.0;
                    info = $"Done → {Path.GetFileName(plan.Path)} ({mb:0.0} MB, {sw.Elapsed.TotalSeconds:0}s)";
                    file = plan.Path;
                    ok = true;
                    if (diag is { EmptyContour: true })
                        warn = $"No surface in the sampled region — {Path.GetFileName(plan.Path)} holds only a placeholder triangle. " +
                               "Check Min/Max against the volume's position, or the volume's size.";
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    info = $"Cancelled — nothing written; {Path.GetFileName(plan.Path)} untouched.";
                }
                catch (DualcException dex) when (dex.Code == DualcStatus.Bounds)
                {
                    err = "Field is unbounded — connect a boundary (Onion/Boolean) or set Min/Max.";
                    info = "Failed (unbounded field)";
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                    info = "Failed";
                }
                finally
                {
                    field?.Dispose();
                }

                RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    bool relaunch;
                    lock (_gate)
                    {
                        _state = ok ? RunState.Done : cancelled ? RunState.Cancelled : RunState.Failed;
                        _resultInfo = info;
                        _resultFile = file;
                        _errorMsg = err;
                        _resultWarning = warn;
                        _progress = null;
                        _cancelRequested = false;
                        if (ReferenceEquals(_cts, cts)) _cts = null;
                        StopTicker();
                        Message = ok ? "Done" : cancelled ? "Cancelled" : "Failed";
                        relaunch = _restartRequested;
                        if (relaunch) { _restartRequested = false; _runRequested = true; }
                    }
                    cts?.Dispose();
                    if (_removed) return;
                    OnPingDocument()?.ScheduleSolution(5, _ => ExpireSolution(false));
                }));
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>An <see cref="IProgress{T}"/> that invokes its callback inline on the reporting
        /// thread — unlike <see cref="Progress{T}"/>, which posts to the captured synchronization
        /// context — so a report costs one field write on the worker.</summary>
        private sealed class ProgressSink : IProgress<DualcProgress>
        {
            private readonly Action<DualcProgress> _onReport;
            public ProgressSink(Action<DualcProgress> onReport) => _onReport = onReport;
            public void Report(DualcProgress value) => _onReport(value);
        }

        private static string FormatProgress(DualcProgress p)
        {
            int pct = (int)Math.Round(p.Fraction * 100);
            switch (p.Stage)
            {
                case DualcStage.Tile:    return $"tile {p.Done}/{p.Total} ({pct}%)";
                case DualcStage.Sample:  return $"sampling {pct}%";
                case DualcStage.Contour: return $"contouring {pct}%";
                default:                 return "writing file";
            }
        }

        private string RunningLabel()
        {
            int secs;
            bool cancelling;
            lock (_gate)
            {
                secs = (int)(DateTime.UtcNow - _startedUtc).TotalSeconds;
                cancelling = _cancelRequested;
            }
            string head = cancelling ? "Cancelling…" : "Writing…";
            string? stage = _progress;
            return stage is null ? $"{head} {secs}s" : $"{head} {stage} · {secs}s";
        }

        private void EmitRunningStatus(IGH_DataAccess da) => da.SetData(0, RunningLabel());

        private void StartTicker()
        {
            StopTicker();
            _tick = new Timer { Interval = 1000 };
            _tick.Tick += (_, __) =>
            {
                lock (_gate) { if (_state != RunState.Running) return; }
                Message = RunningLabel();
                global::Grasshopper.Instances.RedrawCanvas();
            };
            _tick.Start();
        }

        private void StopTicker()
        {
            _tick?.Stop();
            _tick?.Dispose();
            _tick = null;
        }

        private static DualcContourParams BuildParams(int depth, bool hasBounds, Point3d min, Point3d max)
        {
            DualcContourParams prm = DualcContourParams.Default();
            prm.MaxDepth = depth;
            if (hasBounds)
            {
                prm.HasBounds = true;
                prm.BoundsMin = (min.X, min.Y, min.Z);
                prm.BoundsMax = (max.X, max.Y, max.Z);
            }
            return prm;
        }

        // Equality-only "hash": the raw concatenation is enough to detect any change to the write inputs.
        private static string InputHash(string json, int depth, ExportPlanResult plan,
            bool hasBounds, Point3d min, Point3d max)
        {
            var sb = new StringBuilder(json.Length + 64);
            sb.Append(json).Append('|').Append(depth).Append('|').Append(plan.Path).Append('|')
              .Append(plan.Strategy).Append('|').Append(plan.TileDepth).Append('|').Append(hasBounds);
            if (hasBounds) sb.Append('|').Append(min).Append('|').Append(max);
            return sb.ToString();
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            _removed = true;
            StopTicker();
            // A write in flight is cancelled at the engine's next checkpoint (0.5.0: nothing is left
            // at the path); with an older library it finishes harmlessly in the background. Either
            // way its completion callback no-ops via _removed.
            lock (_gate) { _cancelRequested = true; _cts?.Cancel(); }
            base.RemovedFromDocument(document);
        }

        // A fresh GUID (not the old Contour/Export …030): this is conceptually a different, export-only
        // component. A new GUID makes an old canvas show a loud "unrecognized component" placeholder
        // rather than silently remapping wires by index onto the reordered inputs.
        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000033");

        protected override Bitmap? Icon => BoletusIcons.ContourExport;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }

    /// <summary>Draws the momentary <em>Write</em> / <em>Cancel</em> button under the component
    /// capsule and routes a left-click to <see cref="WriteToFileComponent.RequestRun"/>. Momentary
    /// (not a toggle) is what makes the manual-trigger semantics fall out: auto-solves never launch
    /// work — only a click does.</summary>
    internal sealed class WriteToFileAttributes : GH_ComponentAttributes
    {
        private const int ButtonHeight = 22;
        private RectangleF _button;

        public WriteToFileAttributes(WriteToFileComponent owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + ButtonHeight);
            _button = new RectangleF(Bounds.X + 2, Bounds.Bottom - ButtonHeight + 1,
                Bounds.Width - 4, ButtonHeight - 3);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects) return;

            var comp = (WriteToFileComponent)Owner;
            GH_Palette palette = comp.IsBusy ? GH_Palette.Warning : GH_Palette.Black;
            GH_Capsule capsule = GH_Capsule.CreateTextCapsule(_button, _button, palette, comp.ButtonLabel, 2, 0);
            capsule.Render(graphics, Selected, Owner.Locked, false);
            capsule.Dispose();
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && _button.Contains(e.CanvasLocation))
            {
                ((WriteToFileComponent)Owner).RequestRun();
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
