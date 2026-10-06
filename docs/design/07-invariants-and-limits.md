# Invariants and limits

What the wrapper and the plugin guarantee, the rules of thumb that follow from how DualC
samples, and every numeric constant that decides a behaviour — each stated once, with the
code that enforces it. A page that depends on one of these links here rather than restating
the number. The engine's own invariants — watertightness, the ≥ 2–3 cells resolution rule,
thread-count determinism, tiled-equals-monolithic at the engine level — are DualC's
[design 10](../../../DualC/docs/design/10-invariants-and-tolerances.md).

## The golden contour counts

DualC pins exact contour counts for two graphs at `maxDepth = 6` with default parameters
(the record is its [14 § 8](../../../DualC/docs/roadmap/14-c-abi/03-implementation-and-verification.md#8-tests--verification-record)),
and Boletus makes a single exact equality on them its **marshaling gate**: a silent
struct-padding or pinning bug shifts the counts rather than failing to compile, so one
assertion validates struct layout, string marshaling and array extraction at once.

| Graph | Vertices | Triangles |
| --- | --- | --- |
| `intersection(onion(gyroid(wavelength=0.5),thickness=0.12),box(min=[-1,-1,-1],max=[1,1,1]))` — analytic | **101 476** | **163 740** |
| `intersection(onion(gyroid(wavelength=0.5),thickness=0.1),mesh(…unit cube…))` — mesh path | **70 032** | **120 612** |

The mesh-path counts are the **same** whether the cube reaches DualC from disk
(`mesh(path="cube.obj")`, the fixture in `tests/Boletus.Core.Tests/`) or in RAM
(`mesh(id="cube")` through `FromJson(json, meshes)`), which is the gate on the
`*_with_meshes` boundary. They are asserted in `WrapperTests.cs` (analytic and disk path),
`MeshBufferTests.cs` (disk path, in-memory path, and the analytic graph as the exact
tree the `Onion` component emits with a boundary — the onion-before-clip gate), and again
through the hooked and the diagnostics twins in `ProgressAndCancelTests.cs` and
`DiagnosticsTests.cs`. This page
is the one home of the numbers; a test or a record cites it.

## Tiled equals monolithic

`ExportTiledStl` streams exactly the triangles the monolithic contour would emit:
`WrapperTests.cs` asserts that the binary-STL facet count of the analytic graph at
`tileDepth = 4` is **163 740** — the golden triangle count — and that the file size is
`84 + 50 · facets`. The exact equality holds because the box is integer-aligned at a
power-of-two depth (a dyadic grid); a non-dyadic box would agree only to micron rounding.
The same test exercises `tileDepth` marshaling and auto-bounds through the tiled writer.

## Serializer output equals `--dump-json`

Every op's emitted shape is pinned per op by a round-trip through `dualc_field --dump-json`
(`FieldGraphVocabularyTests.cs`, a data-driven theory), the shipped sample graphs are rebuilt
from the builders and diffed (`FieldGraphTests.cs`), and the four-step example composition
byte-matches its `--expr` equivalent (`FieldGraphExampleTests.cs`). The wrapper's STL export
is byte-identical to the CLI's for the same graph (`CliParityTests.cs`, SHA-1). The CLI-gated
tests **skip, not fail**, when the CLI is not found — `DUALC_FIELD_EXE`, else DualC's Windows
build path (`dualc_field.exe`; on Linux the variable is the only way in, the binary
`dualc_field`) — and the sample-graph tests also when DualC's `examples/samples` is not
(`DUALC_SAMPLES_DIR`, else its `D:\` path); the pure guards and the end-to-end contours run
regardless.

## The coarse-depth rule

`maxDepth` is the only proxy/export lever, and a contour of a dense lattice is exponential in
it — roughly 4–8× more triangles, memory and time per level — deep enough to exhaust the
machine's RAM. So: **keep contours coarse**. Every golden count runs at `maxDepth = 6`, and
a test that only needs a contour to exist (each catalog shape, the empty-contour case) runs
at 5; `Write to File` defaults `Depth` to **6** and warns above **8**; `Proxy preview`
defaults to **5** under its ceiling. A
coarse contour of a *lattice* is lossy — thin walls drop out — which is faithful for framing
and for solid parts and is why lattice fidelity is the side-car's job, never the proxy's.

## `MaxProxyDepth`

`ProxyPreviewComponent.cs`: `private const int MaxProxyDepth = 7`; the requested depth is
clamped to `[1, 7]` and a remark is posted when the clamp bites. A **depth ceiling** is the
only mechanism that *prevents* an out-of-memory rather than detecting it too late:
the plain `DualcField.Contour` the proxy calls is atomic — no mid-run abort, no pre-count
(the cancellable overload stops at the engine's checkpoints, but only when asked) — so a vertex or triangle
budget could only be checked after building the mesh that had already blown up. At a capped
depth the leaf-cell count is bounded by about `8^depth`, so memory is bounded for any field.

## Unbounded fields

A bare TPMS, a strut lattice, a plane, an infinite primitive or a `repeat` has no finite
extent: DualC refuses to contour it without bounds (`DUALC_ERR_BOUNDS`, surfaced as
`DualcStatus.Bounds`). The terminals auto-fit a finite field and take Min / Max only for an
unbounded one — **both corners or neither**; a single corner is ignored with a warning — and
turn the status into a friendly message naming the two fixes (a boundary clip, or bounds).
Bounds are a contour-time flag, never a graph node.

## Single-flight writes and the cancel constraint

**A native export cannot be hard-cancelled from .NET**: there is no `Thread.Abort` on net7,
so a write stops only where the engine agrees to stop. With a library at ABI 0.5.0 that is
**cooperative cancel**: a host-owned token the engine polls at every tile and inside every
tile's contour, honoured up to the end of the contour (the final write phase of a
monolithic export runs to completion — DualC D-45), and never a file at the path afterwards
([01 § Cancel and progress](01-native-interop.md#cancel-and-progress--the-050-twins)). The
invariant that holds on every library is **never two concurrent native writes** on one
component ([04](04-grasshopper-plugin.md#write-to-file--the-threading-model)): a cancel
ends the write before another can start, and on a library older than 0.5.0 — where the
export calls take no token and no callback, so a running write always finishes — a second
click is a queued restart that fires on completion, and **progress is indeterminate** there,
elapsed seconds instead of the engine's percentage. Which path runs is decided at runtime
by `DualcField.SupportsProgress`, never by the version string. The whole `DualcField`
lifetime of a write stays on its worker thread
([01 § The single-threaded rule](01-native-interop.md#the-single-threaded-rule)).

## The version trap

`dualc_version()` reports the same string for DLLs built from different DualC commits when
the project version was not bumped, while the field-graph **parser is compiled inside the
DLL** — so a DLL can reject an op (`DUALC_ERR_GRAPH`, "unknown op") that a same-version DLL
from a later commit accepts, and **no runtime check on the version string can detect a
vocabulary gap**. Hence the DLL is pinned and refreshed by commit
([`native/README.md`](../../native/README.md)), `Boletus.Core` asserts nothing at startup,
and reaching a new DualC op always means re-vendoring the DLL, not only teaching `Ops.cs`
the token. The version string does move with the **ABI** (each additive set of entry points
bumped it), but Boletus does not read it for that either: the ABI level is probed by entry
point (`DualcField.SupportsProgress` for 0.5.0, `DualcField.SupportsDiagnostics` for 0.4.0 —
one probe per capability, each a call that an older library cannot resolve), which cannot
lie. A version bump on a vocabulary
change or a capability query stays an open upstream ask ([decisions](../decisions/README.md)).

## Layout facts the gate stands on

`DualcMeshSourceNative` mirrors `DualcMeshSource`'s field order in default sequential layout
(48 bytes on x64); `ContourParamsNative` mirrors the flat parameter struct with its
`double[3]` pairs split into scalars (80 bytes on x64, a 4-byte pad after `hasBounds`);
`DualcDiagnosticsNative` mirrors `DualcDiagnostics` with `int` booleans and `ulong` counts
(80 bytes on x64, a 4-byte pad after `inputEmpty`), and the diagnostics' vertex count sitting
past that pad region is what the golden counts check through it. The
header is the authority ([`capi/dualc_c.h`](../../../DualC/capi/dualc_c.h)); the golden
counts are what notices a mismatch.

## The test floor

The Core suite's size is a **floor the gate asserts** — `dotnet.tests_expected_min` in
`scripts/check_data.json`, raised whenever tests are added and never lowered; an unchanged
count across a docs-only change is the proof that no code was edited to match the docs.
The number is not restated here: the floor and the dated records own it.

---

← Back to the [design index](README.md) · the [docs index](../README.md)
