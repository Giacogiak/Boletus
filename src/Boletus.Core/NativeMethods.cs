using System;
using System.Runtime.InteropServices;

namespace Boletus.Core
{
    /// <summary>Blittable mirror of the C <c>DualcContourParams</c> struct.</summary>
    /// <remarks>
    /// The <c>double[3]</c> bounds arrays are flattened to scalars so the struct is
    /// blittable (no custom marshaling). Field order and natural 8-byte alignment
    /// match the C layout exactly (total 80 bytes): the implicit 4-byte pad after
    /// <c>hasBounds</c> before <c>bMinX</c> is reproduced identically by the default
    /// sequential layout on x64.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContourParamsNative
    {
        public int    maxDepth;
        public int    minDepth;
        public double collapse;
        public int    hasBounds;
        public double bMinX, bMinY, bMinZ;   // boundsMin[3]
        public double bMaxX, bMaxY, bMaxZ;   // boundsMax[3]
        public int    manifold;
        public uint   numThreads;
    }

    /// <summary>Blittable mirror of the ABI-owned C <c>DualcMesh</c> struct.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MeshNative
    {
        public IntPtr positions;     // float*   (3 * vertexCount)
        public IntPtr normals;       // float*   (3 * vertexCount)
        public IntPtr indices;       // uint32_t* (3 * triangleCount)
        public uint   vertexCount;
        public uint   triangleCount;
    }

    /// <summary>
    /// Blittable mirror of the C <c>DualcMeshSource</c> struct (DualC v0.3.0) — one host-owned
    /// in-memory mesh leaf the graph references by id (<c>mesh(id="…")</c> / <c>winding(id="…")</c>).
    /// </summary>
    /// <remarks>
    /// Field <b>order and types must match the C struct exactly</b> (default sequential layout):
    /// 48 bytes on x64, with an implicit 4-byte pad after each <c>uint</c> count before the next
    /// 8-byte pointer — reproduced identically by the C compiler (safe because this project is x64).
    /// The pointer fields must reference <b>pinned</b> managed buffers
    /// (<see cref="GCHandle"/> / <c>Pinned</c>) kept alive only for the create call: DualC reads and
    /// <b>copies</b> them during the call, so they need not outlive the field. <c>normals</c> is
    /// currently ignored by DualC — pass <see cref="IntPtr.Zero"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DualcMeshSourceNative
    {
        public IntPtr id;            // const char*    (UTF-8, NUL-terminated)
        public IntPtr vertices;      // const float*   (3 * vertexCount)
        public uint   vertexCount;
        public IntPtr indices;       // const uint32_t* (3 * triangleCount)
        public uint   triangleCount;
        public IntPtr normals;       // const float* — pass IntPtr.Zero (ignored)
    }

    /// <summary>
    /// Blittable mirror of the C <c>DualcDiagnostics</c> struct (ABI 0.4.0): what the engine
    /// degraded silently, reported instead of guessed. DualC zeroes it on entry and writes every
    /// field only when the call completes; on a non-OK return it stays zero, and a ZEROED struct
    /// reads "not watertight" — never read it unless the call returned <c>DUALC_OK</c>.
    /// </summary>
    /// <remarks>
    /// 80 bytes on x64: an implicit 4-byte pad after <c>inputEmpty</c> before the first
    /// <c>uint64_t</c>, none elsewhere (each run of four <c>int</c>s is 16 bytes). Reproduced
    /// identically by the default sequential layout. Booleans are <c>int</c> 0/1; counts are
    /// <c>uint64_t</c> so they cannot wrap on a dense part.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DualcDiagnosticsNative
    {
        public int   inputEmpty;              // offset  0 (+4 pad)
        public ulong inputBoundaryEdges;      //         8
        public ulong inputNonManifoldEdges;   //        16
        public int   inputWatertight;         //        24
        public int   boundsFallback;          //        28
        public int   gridBoundsExceeded;      //        32
        public int   emptyContour;            //        36
        public ulong outputVertices;          //        40
        public ulong outputTriangles;         //        48
        public ulong outputBoundaryEdges;     //        56
        public ulong outputNonManifoldEdges;  //        64
        public int   outputWatertight;        //        72
        public int   anyIssue;                //        76 → 80 bytes
    }

    /// <summary>
    /// The native progress callback (<c>DualcProgressFn</c>, ABI 0.5.0). Invoked by DualC on the
    /// thread that made the P/Invoke only — never on an engine worker. The managed delegate
    /// instance must stay rooted for the duration of the call (the thunk dies with it) and must
    /// never throw: an exception unwinding into native code crashes the process.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DualcProgressFnNative(IntPtr user, int stage, uint done, uint total);

    /// <summary>
    /// Raw P/Invoke surface over <c>dualc_capi.dll</c>: the 9 entry points of the original
    /// surface, the two v0.3.0 <c>*_with_meshes</c> in-memory-mesh create twins, the two 0.4.0
    /// <c>*_with_diagnostics</c> twins, and the 0.5.0 cancel token (four functions) with the three
    /// <c>*_with_progress</c> twins — every export of the pinned library bound, plus one
    /// pointer-typed alias of the contour diagnostics twin that exists only to probe for it.
    /// All <c>const char*</c> params are passed as NUL-terminated UTF-8 byte[] (netstandard2.0
    /// has no <c>[LibraryImport]</c> / <c>LPUTF8Str</c>).
    /// </summary>
    internal static class NativeMethods
    {
        private const string Dll = "dualc_capi"; // dualc_capi.dll on the load path

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dualc_version();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void dualc_default_params(out ContourParamsNative outParams);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_create_from_json(
            byte[] json, out DualcFieldHandle outField, byte[] err, int errlen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_create_from_expr(
            byte[] expr, out DualcFieldHandle outField, byte[] err, int errlen);

        // In-memory mesh sources (v0.3.0). `meshes` carries pointers into PINNED managed buffers
        // that must stay alive for the duration of the call only (DualC copies them eagerly).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_create_from_json_with_meshes(
            byte[] json, [In] DualcMeshSourceNative[] meshes, int meshCount,
            out DualcFieldHandle outField, byte[] err, int errlen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_create_from_expr_with_meshes(
            byte[] expr, [In] DualcMeshSourceNative[] meshes, int meshCount,
            out DualcFieldHandle outField, byte[] err, int errlen);

        // Used only by DualcFieldHandle.ReleaseHandle (raw pointer, single release).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void dualc_field_destroy(IntPtr field);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_contour(
            DualcFieldHandle field, in ContourParamsNative p,
            out MeshNative outMesh, byte[] err, int errlen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void dualc_mesh_release(ref MeshNative mesh);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_export(
            DualcFieldHandle field, byte[] path, in ContourParamsNative p,
            byte[] err, int errlen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_export_tiled_stl(
            DualcFieldHandle field, byte[] path, in ContourParamsNative p,
            int tileDepth, byte[] err, int errlen);

        // ---- ABI 0.4.0: the *_with_diagnostics twins -----------------------------------------
        // Same mesh / same file as the originals, plus a filled-in DualcDiagnostics. A library
        // older than 0.4.0 lacks these exports: DualcField.SupportsDiagnostics probes once through
        // the alias below. `diag` is an out-struct: DualC zeroes it on entry and fills it on OK.

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_contour_with_diagnostics(
            DualcFieldHandle field, in ContourParamsNative p, out MeshNative outMesh,
            out DualcDiagnosticsNative diag, byte[] err, int errlen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_export_with_diagnostics(
            DualcFieldHandle field, byte[] path, in ContourParamsNative p,
            out DualcDiagnosticsNative diag, byte[] err, int errlen);

        // Probe alias — the SAME export, pointer-typed so every argument can be NULL: DualC
        // writes nothing (both out-pointers are NULL) and answers DUALC_ERR_USAGE "null argument";
        // the only observable is whether the export resolves (EntryPointNotFoundException if not).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl,
                   EntryPoint = "dualc_field_contour_with_diagnostics")]
        public static extern int dualc_field_contour_with_diagnostics_probe(
            IntPtr field, IntPtr p, IntPtr outMesh, IntPtr diag, byte[] err, int errlen);

        // ---- ABI 0.5.0: the cancel token and the *_with_progress twins ----------------------
        // A library older than 0.5.0 lacks these exports: the first call throws
        // EntryPointNotFoundException, which DualcField.SupportsProgress probes once.

        // Returned as a SafeHandle so the marshaller constructs DualcCancelTokenHandle around the
        // raw pointer; NULL (allocation failure) becomes an invalid handle.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern DualcCancelTokenHandle dualc_cancel_token_create();

        // Thread-safe and sticky; the one token function that may run concurrently with a call
        // using the token. Safe on NULL natively, but the SafeHandle marshaller rejects a null.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void dualc_cancel_token_request(DualcCancelTokenHandle token);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_cancel_token_is_requested(DualcCancelTokenHandle token);

        // Used only by DualcCancelTokenHandle.ReleaseHandle (raw pointer, single release).
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void dualc_cancel_token_destroy(IntPtr token);

        // `diag` is the 0.4.0 DualcDiagnostics out-param, filled exactly as by the 0.4.0 twin.
        // `progress` may be null (marshals as a NULL function pointer); `cancel` must not be.
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_contour_with_progress(
            DualcFieldHandle field, in ContourParamsNative p, out MeshNative outMesh,
            out DualcDiagnosticsNative diag, DualcCancelTokenHandle cancel, DualcProgressFnNative? progress, IntPtr user,
            byte[] err, int errlen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_export_with_progress(
            DualcFieldHandle field, byte[] path, in ContourParamsNative p,
            out DualcDiagnosticsNative diag, DualcCancelTokenHandle cancel, DualcProgressFnNative? progress, IntPtr user,
            byte[] err, int errlen);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int dualc_field_export_tiled_stl_with_progress(
            DualcFieldHandle field, byte[] path, in ContourParamsNative p, int tileDepth,
            DualcCancelTokenHandle cancel, DualcProgressFnNative? progress, IntPtr user,
            byte[] err, int errlen);
    }
}
