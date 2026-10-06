using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Boletus.Core
{
    /// <summary>
    /// A built DualC field-graph. Create one from a field-graph string (canonical
    /// JSON or the terse <c>--expr</c> shorthand — the same vocabulary
    /// <c>dualc_field</c> accepts), then <see cref="Contour"/> it to an in-memory
    /// mesh and/or <see cref="Export"/> it to a file. The <c>out DualcDiagnostics</c>
    /// overloads additionally report what the engine degraded silently — an empty
    /// contour above all — and need a library that <see cref="SupportsDiagnostics"/>.
    /// </summary>
    /// <remarks>
    /// A single instance is <b>single-threaded</b> — create one handle per
    /// Grasshopper solve; do not share across parallel solves. Independent handles
    /// are independent. Dispose to release the native field (and any meshes it
    /// loaded from disk).
    /// </remarks>
    public sealed class DualcField : IDisposable
    {
        private readonly DualcFieldHandle _handle;

        private DualcField(DualcFieldHandle handle) => _handle = handle;

        /// <summary>The native library/ABI version, e.g. <c>"dualc 0.5.0"</c>.</summary>
        public static string Version() => Native.ReadUtf8(NativeMethods.dualc_version());

        private static bool? _supportsProgress;

        /// <summary>
        /// Whether the loaded native library exports the ABI 0.5.0 cancel token and
        /// <c>*_with_progress</c> twins — the entry points behind the <see cref="IProgress{T}"/> /
        /// <see cref="CancellationToken"/> overloads. Probed once by creating and destroying a
        /// token: an older library throws <see cref="EntryPointNotFoundException"/> there, which
        /// is the one reliable capability signal (the version string is not pinned per op).
        /// </summary>
        public static bool SupportsProgress
        {
            get
            {
                if (_supportsProgress is null)
                {
                    try
                    {
                        using (NativeMethods.dualc_cancel_token_create()) { }
                        _supportsProgress = true;
                    }
                    catch (EntryPointNotFoundException)
                    {
                        _supportsProgress = false;
                    }
                }
                return _supportsProgress.Value;
            }
        }

        private static void RequireProgressAbi()
        {
            if (!SupportsProgress)
                throw new NotSupportedException(
                    "The loaded dualc_capi library predates ABI 0.5.0: no cancel token or progress " +
                    "callback. Use the overloads without IProgress / CancellationToken, or re-vendor the library.");
        }

        private static bool? _supportsDiagnostics;

        /// <summary>
        /// Whether the loaded native library exports the ABI 0.4.0 <c>*_with_diagnostics</c> twins
        /// — the entry points behind the <c>out <see cref="DualcDiagnostics"/></c> overloads. Probed
        /// once, independently of <see cref="SupportsProgress"/>, by calling the contour twin with
        /// every argument NULL: a 0.4.0+ library writes nothing and answers "null argument", an
        /// older one throws <see cref="EntryPointNotFoundException"/> — the one reliable capability
        /// signal (the version string is not pinned per op).
        /// </summary>
        public static bool SupportsDiagnostics
        {
            get
            {
                if (_supportsDiagnostics is null)
                {
                    try
                    {
                        var err = Native.NewErrBuffer();
                        NativeMethods.dualc_field_contour_with_diagnostics_probe(
                            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, err, err.Length); // DUALC_ERR_USAGE, by design
                        _supportsDiagnostics = true;
                    }
                    catch (EntryPointNotFoundException)
                    {
                        _supportsDiagnostics = false;
                    }
                }
                return _supportsDiagnostics.Value;
            }
        }

        private static void RequireDiagnosticsAbi()
        {
            if (!SupportsDiagnostics)
                throw new NotSupportedException(
                    "The loaded dualc_capi library predates ABI 0.4.0: no *_with_diagnostics entry points. " +
                    "Use the overloads without the DualcDiagnostics out-parameter, or re-vendor the library.");
        }

        /// <summary>Build a field from the terse <c>--expr</c> shorthand string.</summary>
        public static DualcField FromExpr(string expr) => Create(shorthand: true, expr);

        /// <summary>Build a field from a canonical-JSON field-graph string.</summary>
        public static DualcField FromJson(string json) => Create(shorthand: false, json);

        /// <summary>
        /// Build a field from canonical JSON whose <c>mesh</c>/<c>winding</c> leaves resolve to
        /// host-provided in-memory geometry (keyed by id, referenced as <c>mesh(id="…")</c>) instead
        /// of files — the diskless path the Grasshopper plugin uses. Requires DualC v0.3.0+
        /// (<c>dualc_field_create_from_json_with_meshes</c>). An empty/absent map falls back to the
        /// plain <see cref="FromJson(string)"/>.
        /// </summary>
        /// <remarks>
        /// The buffers are <b>copied by DualC during the create call</b>, so they are pinned only for
        /// the call's duration and need not outlive the returned field.
        /// </remarks>
        public static DualcField FromJson(string json, IReadOnlyDictionary<string, MeshBuffer> meshSources)
        {
            if (meshSources is null) throw new ArgumentNullException(nameof(meshSources));
            if (meshSources.Count == 0) return FromJson(json);

            // Pin each buffer's geometry + UTF-8 id for the create call only; DualC copies eagerly.
            var natives = new DualcMeshSourceNative[meshSources.Count];
            var pins = new List<GCHandle>(meshSources.Count * 3);
            try
            {
                int i = 0;
                foreach (var kv in meshSources)
                {
                    var buffer = kv.Value ?? throw new ArgumentException(
                        $"Mesh source '{kv.Key}' has a null buffer.", nameof(meshSources));

                    byte[] idUtf8 = Native.Utf8(kv.Key);
                    var idPin = GCHandle.Alloc(idUtf8, GCHandleType.Pinned);
                    var vPin = GCHandle.Alloc(buffer.Vertices, GCHandleType.Pinned);
                    var tPin = GCHandle.Alloc(buffer.Triangles, GCHandleType.Pinned); // int[] ≡ uint32*
                    pins.Add(idPin); pins.Add(vPin); pins.Add(tPin);

                    natives[i++] = new DualcMeshSourceNative
                    {
                        id            = idPin.AddrOfPinnedObject(),
                        vertices      = vPin.AddrOfPinnedObject(),
                        vertexCount   = (uint)buffer.VertexCount,
                        indices       = tPin.AddrOfPinnedObject(),
                        triangleCount = (uint)buffer.TriangleCount,
                        normals       = IntPtr.Zero, // DualC derives normals from geometry.
                    };
                }

                var err = Native.NewErrBuffer();
                int rc = NativeMethods.dualc_field_create_from_json_with_meshes(
                    Native.Utf8(json), natives, natives.Length, out var handle, err, err.Length);

                if (rc != (int)DualcStatus.Ok)
                {
                    handle?.Dispose();          // NULL handle on failure, but be defensive.
                    throw new DualcException(rc, Native.ReadUtf8(err));
                }
                return new DualcField(handle);
            }
            finally
            {
                foreach (var h in pins)
                    if (h.IsAllocated) h.Free();
            }
        }

        private static DualcField Create(bool shorthand, string text)
        {
            var err = Native.NewErrBuffer();
            int rc = shorthand
                ? NativeMethods.dualc_field_create_from_expr(Native.Utf8(text), out var handle, err, err.Length)
                : NativeMethods.dualc_field_create_from_json(Native.Utf8(text), out handle, err, err.Length);

            if (rc != (int)DualcStatus.Ok)
            {
                handle?.Dispose();          // NULL handle on failure, but be defensive.
                throw new DualcException(rc, Native.ReadUtf8(err));
            }
            return new DualcField(handle);
        }

        /// <summary>
        /// Contour the field into a managed mesh. Pass a coarse
        /// <see cref="DualcContourParams.MaxDepth"/> for a cheap drawable proxy, the
        /// full depth for export-grade geometry. Throws <see cref="DualcException"/>
        /// with <see cref="DualcStatus.Bounds"/> for an unbounded field unless
        /// <see cref="DualcContourParams.HasBounds"/> is set. A field with no surface in
        /// the sampled region returns OK with a one-triangle placeholder; the
        /// <c>out <see cref="DualcDiagnostics"/></c> overload is the way to detect it.
        /// </summary>
        public DualcMeshData Contour(DualcContourParams parameters)
        {
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            Native.Check(
                NativeMethods.dualc_field_contour(_handle, in np, out var mesh, err, err.Length),
                err);
            try
            {
                return DualcMeshData.CopyFrom(in mesh);
            }
            finally
            {
                NativeMethods.dualc_mesh_release(ref mesh); // ALWAYS release the native copy.
            }
        }

        /// <summary>
        /// <see cref="Contour(DualcContourParams)"/> plus the engine's <see cref="DualcDiagnostics"/>
        /// for the call (ABI 0.4.0). Throws <see cref="NotSupportedException"/> when
        /// <see cref="SupportsDiagnostics"/> is false; <paramref name="diagnostics"/> is assigned
        /// only when the call returned OK.
        /// </summary>
        public DualcMeshData Contour(DualcContourParams parameters, out DualcDiagnostics diagnostics)
        {
            RequireDiagnosticsAbi();
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            Native.Check(
                NativeMethods.dualc_field_contour_with_diagnostics(
                    _handle, in np, out var mesh, out var nd, err, err.Length),
                err);
            diagnostics = new DualcDiagnostics(in nd);
            try
            {
                return DualcMeshData.CopyFrom(in mesh);
            }
            finally
            {
                NativeMethods.dualc_mesh_release(ref mesh); // ALWAYS release the native copy.
            }
        }

        /// <summary>Contour and write to <paramref name="path"/>; format chosen by the
        /// extension: <c>.obj</c> / <c>.stl</c> / <c>.3mf</c> (1 unit = 1 mm).</summary>
        public void Export(string path, DualcContourParams parameters)
        {
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            Native.Check(
                NativeMethods.dualc_field_export(_handle, Native.Utf8(path), in np, err, err.Length),
                err);
        }

        /// <summary>
        /// <see cref="Export(string, DualcContourParams)"/> plus the engine's
        /// <see cref="DualcDiagnostics"/> describing the mesh that was written (ABI 0.4.0). Throws
        /// <see cref="NotSupportedException"/> when <see cref="SupportsDiagnostics"/> is false.
        /// </summary>
        public void Export(string path, DualcContourParams parameters, out DualcDiagnostics diagnostics)
        {
            RequireDiagnosticsAbi();
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            Native.Check(
                NativeMethods.dualc_field_export_with_diagnostics(
                    _handle, Native.Utf8(path), in np, out var nd, err, err.Length),
                err);
            diagnostics = new DualcDiagnostics(in nd);
        }

        /// <summary>Streaming/tiled binary-STL export (bounded RAM for dense parts).
        /// <paramref name="path"/> should end in <c>.stl</c>; <paramref name="tileDepth"/>
        /// must be ≤ <see cref="DualcContourParams.MaxDepth"/> and cannot combine with
        /// <see cref="DualcContourParams.Collapse"/> &gt; 0.</summary>
        public void ExportTiledStl(string path, DualcContourParams parameters, int tileDepth)
        {
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            Native.Check(
                NativeMethods.dualc_field_export_tiled_stl(_handle, Native.Utf8(path), in np, tileDepth, err, err.Length),
                err);
        }

        // ---- ABI 0.5.0: cooperative cancel + coarse progress -------------------------------

        /// <summary>
        /// <see cref="Contour(DualcContourParams)"/> with a cancel token and a progress callback
        /// (stages <see cref="DualcStage.Sample"/> then <see cref="DualcStage.Contour"/>). Throws
        /// <see cref="OperationCanceledException"/> when <paramref name="ct"/> is cancelled before
        /// or during the call — DualC returns at its next checkpoint, well under a second — and
        /// <see cref="NotSupportedException"/> when <see cref="SupportsProgress"/> is false.
        /// </summary>
        public DualcMeshData Contour(DualcContourParams parameters, IProgress<DualcProgress>? progress, CancellationToken ct)
            => Contour(parameters, progress, ct, out _);

        /// <summary>
        /// <see cref="Contour(DualcContourParams, IProgress{DualcProgress}, CancellationToken)"/> plus
        /// the engine's <see cref="DualcDiagnostics"/> — the 0.5.0 twin fills the same struct as the
        /// 0.4.0 one. <paramref name="diagnostics"/> is assigned only when the call returned OK;
        /// a cancelled or failed call throws first.
        /// </summary>
        public DualcMeshData Contour(DualcContourParams parameters, IProgress<DualcProgress>? progress,
            CancellationToken ct, out DualcDiagnostics diagnostics)
        {
            RequireProgressAbi();
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            MeshNative mesh = default;
            DualcDiagnosticsNative nd = default;   // a local: an `out` parameter cannot be captured
            int rc = WithToken(progress, ct, (token, cb) =>
                NativeMethods.dualc_field_contour_with_progress(
                    _handle, in np, out mesh, out nd, token, cb, IntPtr.Zero, err, err.Length));
            ThrowIfCancelled(rc, err, ct);   // on DUALC_CANCELLED the mesh is zeroed: nothing to release
            Native.Check(rc, err);
            diagnostics = new DualcDiagnostics(in nd);
            try
            {
                return DualcMeshData.CopyFrom(in mesh);
            }
            finally
            {
                NativeMethods.dualc_mesh_release(ref mesh); // ALWAYS release the native copy.
            }
        }

        /// <summary>
        /// <see cref="Export(string, DualcContourParams)"/> with a cancel token and a progress
        /// callback (stages Sample, Contour, then Write as (0,1)/(1,1)). On cancellation nothing
        /// exists at <paramref name="path"/>: DualC writes <c>path + ".part"</c> and renames only
        /// on success, so a file already there is left as it was. Cancel is honoured up to the
        /// end of the contour; the write phase itself is not interrupted.
        /// </summary>
        public void Export(string path, DualcContourParams parameters, IProgress<DualcProgress>? progress, CancellationToken ct)
            => Export(path, parameters, progress, ct, out _);

        /// <summary>
        /// <see cref="Export(string, DualcContourParams, IProgress{DualcProgress}, CancellationToken)"/>
        /// plus the engine's <see cref="DualcDiagnostics"/> describing the mesh that was written.
        /// <paramref name="diagnostics"/> is assigned only when the call returned OK.
        /// </summary>
        public void Export(string path, DualcContourParams parameters, IProgress<DualcProgress>? progress,
            CancellationToken ct, out DualcDiagnostics diagnostics)
        {
            RequireProgressAbi();
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            byte[] utf8Path = Native.Utf8(path);
            DualcDiagnosticsNative nd = default;   // a local: an `out` parameter cannot be captured
            int rc = WithToken(progress, ct, (token, cb) =>
                NativeMethods.dualc_field_export_with_progress(
                    _handle, utf8Path, in np, out nd, token, cb, IntPtr.Zero, err, err.Length));
            ThrowIfCancelled(rc, err, ct);
            Native.Check(rc, err);
            diagnostics = new DualcDiagnostics(in nd);
        }

        /// <summary>
        /// <see cref="ExportTiledStl(string, DualcContourParams, int)"/> with a cancel token and a
        /// progress callback (stage <see cref="DualcStage.Tile"/> only: (0,T) before the loop,
        /// (i,T) as tile i starts, (T,T) after the rename). Cancellation is polled at every tile
        /// and inside every tile's contour; the same no-partial-file guarantee as
        /// <see cref="Export(string, DualcContourParams, IProgress{DualcProgress}, CancellationToken)"/>.
        /// </summary>
        public void ExportTiledStl(string path, DualcContourParams parameters, int tileDepth,
            IProgress<DualcProgress>? progress, CancellationToken ct)
        {
            RequireProgressAbi();
            var np = parameters.ToNative();
            var err = Native.NewErrBuffer();
            byte[] utf8Path = Native.Utf8(path);
            int rc = WithToken(progress, ct, (token, cb) =>
                NativeMethods.dualc_field_export_tiled_stl_with_progress(
                    _handle, utf8Path, in np, tileDepth, token, cb, IntPtr.Zero, err, err.Length));
            ThrowIfCancelled(rc, err, ct);
            Native.Check(rc, err);
        }

        private delegate int NativeCallWithToken(DualcCancelTokenHandle token, DualcProgressFnNative? cb);

        /// <summary>
        /// The one place the 0.5.0 lifetime rules are enforced: the token is created on this (the
        /// calling) thread and lives until the call returned; the managed cancel is forwarded by a
        /// <see cref="CancellationTokenRegistration"/> that is disposed <b>before</b> the token
        /// (so a late cancel never touches a destroyed token); the progress delegate is rooted
        /// for the call and swallows every exception (nothing may unwind into native code).
        /// </summary>
        private static int WithToken(IProgress<DualcProgress>? progress, CancellationToken ct, NativeCallWithToken call)
        {
            using (DualcCancelTokenHandle token = NativeMethods.dualc_cancel_token_create())
            {
                if (token.IsInvalid)
                    throw new DualcException((int)DualcStatus.Unknown, "dualc_cancel_token_create returned NULL");

                DualcProgressFnNative? cb = null;
                if (progress != null)
                {
                    cb = (user, stage, done, total) =>
                    {
                        try { progress.Report(new DualcProgress((DualcStage)stage, done, total)); }
                        catch { /* never let an exception cross into native code */ }
                    };
                }

                int rc;
                // Register forwards a cancel from any thread into the native token; if `ct` is
                // already cancelled the callback runs synchronously here, which DualC honours
                // at its first checkpoint (a pre-requested token cancels before any work).
                using (ct.Register(() => RequestQuietly(token)))
                {
                    rc = call(token, cb);
                }
                GC.KeepAlive(cb);
                return rc;
            }
        }

        private static void RequestQuietly(DualcCancelTokenHandle token)
        {
            try { NativeMethods.dualc_cancel_token_request(token); }
            catch (ObjectDisposedException) { /* the call already returned; nothing to cancel */ }
        }

        private static void ThrowIfCancelled(int rc, byte[] err, CancellationToken ct)
        {
            if (rc == (int)DualcStatus.Cancelled)
                throw new OperationCanceledException(
                    $"DualC cancelled: {Native.ReadUtf8(err)}", ct);
        }

        public void Dispose() => _handle.Dispose();
    }
}
