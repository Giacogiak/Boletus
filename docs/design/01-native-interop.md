# Native interop — `Boletus.Core` over the C ABI

The managed layer that lets C# code build a field from a field-graph string, contour it to an
in-memory mesh and export it to a file — with exceptions instead of integer status codes,
deterministic cleanup instead of raw pointers, and managed arrays instead of ABI-owned
buffers. It targets **netstandard2.0** and has no Rhino dependency. The contract it binds is
DualC's: the header [`capi/dualc_c.h`](../../../DualC/capi/dualc_c.h), the host guide
[`capi/README.md`](../../../DualC/capi/README.md) and the wrapper hand-off
[`capi/CSHARP_WRAPPER_HANDOFF.md`](../../../DualC/capi/CSHARP_WRAPPER_HANDOFF.md); the
function list, the status codes, the struct layouts and the ownership rules are theirs and are
not restated here.

## The public surface — `DualcField`

`src/Boletus.Core/DualcField.cs` is the one public type a caller needs:

- `Version()` reads the native version string (`dualc_version`).
- `FromExpr(text)` / `FromJson(json)` build a field from the `--expr` shorthand or canonical
  JSON — the same vocabulary `dualc_field` accepts. Either form yields the same native tree.
- `FromJson(json, meshSources)` is the **diskless** overload: the graph's `mesh` / `winding`
  leaves reference host geometry by `id`, and the `MeshBuffer`s arrive through
  `dualc_field_create_from_json_with_meshes`. An empty map falls back to the plain
  `FromJson`. The pinning rule is [§ Buffers are pinned for the call only](#buffers-are-pinned-for-the-call-only).
- `Contour(DualcContourParams)` returns a managed `DualcMeshData` — `float[]` positions and
  normals, `int[]` indices — copied out of the ABI-owned `DualcMesh`, which is released in a
  `finally` once the status was OK (a failed call hands out no mesh to release).
- `Export(path, params)` contours and writes a file; the format follows the extension
  (`.obj` / `.stl` / `.3mf`). `ExportTiledStl(path, params, tileDepth)` is the streaming
  binary-STL writer with bounded RAM.
- `Contour(params, out DualcDiagnostics)` and `Export(path, params, out DualcDiagnostics)` are
  the same two calls through the 0.4.0 `*_with_diagnostics` twins, handing back what the
  engine degraded silently. `EmptyContour` is the one that matters: a field with no surface
  inside the sampled region contours to **OK and a one-triangle placeholder**, and the flag is
  the only way to tell that placeholder from real geometry. With it come the output's vertex,
  triangle, boundary-edge and non-manifold-edge counts, `OutputWatertight`, `BoundsFallback`,
  `GridBoundsExceeded`, and `AnyIssue` folding them all. The `Input*` fields describe a mesh
  given to the ABI's mesh entry points; through the field entry points they are never
  inspected and read empty-false, counts zero, watertight-true. The managed struct is built
  only after the status was OK — a cancelled or failed call throws first — so a caller never
  sees the zeroed native struct, which would read "not watertight". `ExportTiledStl` has no
  diagnostics: the ABI gives the tiled writer none.
- Each of the three has an overload taking an `IProgress<DualcProgress>` and a
  `CancellationToken` — the cooperative cancel and coarse progress of ABI 0.5.0
  ([§ Cancel and progress](#cancel-and-progress--the-050-twins)); the two monolithic ones also
  with a trailing `out DualcDiagnostics`, which the 0.5.0 twin fills exactly as the 0.4.0 one
  does. A cancelled call throws `OperationCanceledException`; a library without those entry
  points throws `NotSupportedException`, which `SupportsProgress` lets a caller test first.
- `SupportsProgress` probes the loaded library once — a cancel token is created and
  destroyed; an `EntryPointNotFoundException` means an older ABI — so one managed build runs
  on the 0.5.0 library and on the older DLL alike ([07 § The version trap](07-invariants-and-limits.md#the-version-trap)).
- `SupportsDiagnostics` is the same kind of probe for the 0.4.0 twins, independent of the
  first: the contour twin is called once with every argument NULL, which a 0.4.0+ library
  answers with a usage status and writes nothing but the error text, an older one with
  `EntryPointNotFoundException`. The plain `out DualcDiagnostics` overloads throw
  `NotSupportedException` when it is false (the progress ones are gated by `SupportsProgress`
  instead — a 0.5.0 library carries the 0.4.0 twins too); the plain `Contour` and `Export`
  keep calling the original entry points, so the old DLL path is never routed through a
  newer twin.
- `Dispose()` releases the native field.

`DualcContourParams` (`DualcContourParams.cs`) is the idiomatic mirror of the ABI's flat
parameter struct — `MaxDepth`, `MinDepth`, `Collapse`, `HasBounds` with `BoundsMin` /
`BoundsMax`, `Manifold`, `NumThreads` — with `Default()` read from `dualc_default_params` and
`ToNative()` producing the blittable struct. `MaxDepth` is the **only proxy/export lever**: a
coarse depth is a cheap drawable proxy, the full depth is export-grade; there is no separate
proxy call ([07](07-invariants-and-limits.md#the-coarse-depth-rule)).

## The binding — `NativeMethods.cs`

`src/Boletus.Core/NativeMethods.cs` is the raw, `internal` P/Invoke surface: one
`[DllImport("dualc_capi")]` (no extension, so the runtime probes `dualc_capi.dll` on Windows
and `libdualc_capi.so` on Linux) with `CallingConvention.Cdecl` per bound entry point —
**all twenty** the header declares at the pinned commit: the nine of the original surface,
the two `*_with_meshes` in-memory create twins (0.3.0), the two `*_with_diagnostics` twins
(0.4.0), the four cancel-token functions and the three `*_with_progress` twins (0.5.0) — plus
one pointer-typed alias of the contour diagnostics twin (a second `[DllImport]` naming the
same `EntryPoint`) that exists only so `SupportsDiagnostics` can call it with NULLs. The two
monolithic 0.5.0 twins take a real `DualcDiagnosticsNative` out-struct, which the overloads
without the `out` parameter discard; the tiled one has no `diag` in the ABI. Every older call is a forwarder of its newer twin, so the binding is
additive and the Windows DLL that predates 0.4.0 still serves every original entry point
([`native/README.md`](../../native/README.md)). Every fallible call returns an `int` status and takes an `err` buffer; no C++
exception ever crosses the boundary. The four interop structs are **blittable** so no custom
marshaling runs and the layout matches the header exactly:

| Struct | Mirrors | The one layout rule |
| --- | --- | --- |
| `ContourParamsNative` | `DualcContourParams` | the two `double[3]` bounds are split into six scalars so the struct stays blittable |
| `MeshNative` | `DualcMesh` | ABI-owned; always passed back to `dualc_mesh_release` |
| `DualcMeshSourceNative` | `DualcMeshSource` | default sequential layout in the header's field order; `normals` is `IntPtr.Zero` (DualC derives normals) |
| `DualcDiagnosticsNative` | `DualcDiagnostics` | `int` booleans and `ulong` counts in the header's order; passed as an `out` parameter — zeroed by the runtime's local initialization, as the header asks of the host, and again by DualC on entry — and read only after an OK status |

A shifted golden contour count is the symptom of a layout regression here — the reason a
single exact equality is the marshaling gate ([07](07-invariants-and-limits.md#the-golden-contour-counts)).

## Lifetime — `DualcFieldHandle`

`DualcFieldHandle.cs` is a `SafeHandleZeroOrMinusOneIsInvalid` whose `ReleaseHandle` calls
`dualc_field_destroy`: release-exactly-once, finalizer-guaranteed even under async teardown,
and passed straight to the P/Invokes so the runtime ref-counts it across each call. A NULL
handle is invalid. On a failed create the (null) handle is disposed defensively and a
`DualcException` is thrown.

## Cancel and progress — the 0.5.0 twins

`DualcCancelTokenHandle.cs` is a second `SafeHandle`, over DualC's host-owned cancel token
(`dualc_cancel_token_create` / `_destroy`): one sticky atomic flag per job that any thread may
request, and the only object in the API with a lifetime rule — it must outlive every call
it was passed to. `DualcField.WithToken` is the one place that rule is kept, around each
`*_with_progress` call:

1. the token is created on the calling thread and disposed only after the call returned;
2. the managed `CancellationToken` is forwarded by a `CancellationTokenRegistration` whose
   callback calls `dualc_cancel_token_request` — from whichever thread cancels, typically the
   UI thread while the call blocks a worker — and that registration is disposed **before** the
   token, so a late cancel never touches a destroyed handle; an already-cancelled token fires
   the registration synchronously, which DualC honours at its first checkpoint;
3. the progress callback is a `DualcProgressFnNative` delegate
   (`[UnmanagedFunctionPointer(Cdecl)]`) kept rooted by `GC.KeepAlive` for the call, invoked
   by DualC on the calling thread only, and wrapped so that no exception from the consumer's
   `IProgress.Report` can unwind into native code;
4. `DUALC_CANCELLED` becomes an `OperationCanceledException` carrying the token; every other
   non-OK status goes through `Check` as before.

`DualcProgress.cs` is the report — a `DualcStage` (`Sample`, `Contour`, `Write`, `Tile`),
`Done`, `Total`, `Fraction` — with DualC's shape: monotonic within a stage, constant total,
last report `(Total, Total)`; a monolithic contour reports `Sample` then `Contour`, a
monolithic export adds `Write`, the tiled writer reports `Tile` only. What a cancel leaves on disk, and up to which stage it is honoured, is
DualC's contract ([`capi/README.md` § Cancellation](../../../DualC/capi/README.md#cancellation--progress-050)):
nothing at the path, because every export writes `path.part` and renames on success.

## Marshaling — `Native.cs`

netstandard2.0 has neither `[LibraryImport]` nor `Marshal.PtrToStringUTF8`, so `Native.cs`
does it by hand: strings go in as NUL-terminated UTF-8 `byte[]` (`Utf8`), come back either
from a `byte[]` buffer through `Encoding.UTF8.GetString` (`ReadUtf8(byte[])`, every error
message) or from a native pointer through `Marshal.ReadByte` (`ReadUtf8(IntPtr)`, the version
string), and `NewErrBuffer` allocates the error buffer every fallible call receives. `Check(rc, err)` maps a non-OK status to a `DualcException` carrying the
`DualcStatus` code and the native message — for a graph error, the message includes DualC's
locator (a JSON pointer such as `/root/in/0/radius`, or a character offset for `--expr`).
The code values are DualC's ([`capi/README.md` § Contract](../../../DualC/capi/README.md#contract)).

## Buffers are pinned for the call only

`FromJson(json, meshSources)` pins each `MeshBuffer`'s `Vertices` (`float[]`), `Triangles`
(`int[]`, bit-identical to `uint32*`) and UTF-8 `id` with `GCHandle.Alloc(…, Pinned)`, fills
a `DualcMeshSourceNative[]`, calls the native create, and frees every handle in a `finally`.
DualC **copies the buffers during the create call**, so they need not outlive the field —
the wrapper never keeps a pin past the call. The buffer's normals are not passed; DualC
derives them from geometry.

## The single-threaded rule

A `DualcField` is **single-threaded**: create one handle per Grasshopper solve, contour or
export, dispose — never share a handle across parallel solves. Independent handles are
independent. The `Write to File` terminal keeps the whole create → export → dispose lifetime
on its worker thread for this reason ([04](04-grasshopper-plugin.md#write-to-file--the-threading-model)).
A field may be contoured repeatedly at different depths; a contoured mesh's lifetime is
independent of the field's.

## Where the DLL is found

The library is a build output, never a committed file: `scripts/build_native.py` builds
DualC's C ABI from the git submodule `external/DualC` (or a checkout named by `--dualc` /
`DUALC_ROOT`) into the gitignored `native/<rid>/`, together with DualC's `dualc_field` CLI
and, on Windows, the viewer. `Boletus.Core.csproj` copies `native/x64/dualc_capi.dll` to its
output as an `Exists`-conditioned `<None>` item, so the DLL flows to every referencing
project (the test runner and the `.gha`) and the managed code still compiles before the
native build has run; the gate's `native-built` check fails with the build command when the
library is missing. Inside Rhino the OS loader searches `Rhino.exe`'s directory, not the
`.gha`'s, so the `.gha` installs a `NativeLibrary.SetDllImportResolver` against the **Core**
assembly — the one that holds the `[DllImport]`s
([04](04-grasshopper-plugin.md#the-native-dll-resolver)). On Linux the csproj copies
`native/linux-x64/libdualc_capi.so`, the same ABI for Linux, and the default probe finds it
beside the test assembly; there is no Rhino, hence no resolver. The pin is the submodule's
commit, the same for both platforms; why it is a commit rather than a version string is
[`native/README.md`](../../native/README.md).

---

← Back to the [design index](README.md) · the [docs index](../README.md)
