# 05 — Phase 3b: The contour diagnostics

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`. The first consumer of DualC's ABI 0.4.0 diagnostics, named by D-37's trigger and inventoried at the pin in [07 § 9](../07-upstream-coordination/03-export-callback-and-strut-sync.md#9-dualc-at-the-pin--what-2fcd19f-offers-that-boletus-does-not-use); landed on 2026-10-05, the day Phase C closed.

## Implemented — the contour diagnostics

2026-10-05. DualC's C ABI answers a field with no surface inside the sampled region with
`DUALC_OK` and a **one-triangle placeholder** `(0,0,0) (1,0,0) (0,1,0)` — its mesh library
rejects an empty polygon list (DualC [14/04](../../../../DualC/docs/roadmap/14-c-abi/04-abi-0-4-0.md)).
Before this session Boletus could not tell that from real geometry: `Proxy preview` drew a
stray triangle or nothing visible, `Write to File` wrote a 134-byte STL and reported "Done".
The 0.4.0 `DualcDiagnostics` struct exists for exactly this (`emptyContour` among its
thirteen fields), the vendored Linux library at the pin exports the two `*_with_diagnostics`
twins, and the 0.5.0 `*_with_progress` twins Boletus already bound take the same pointer and
received NULL. The session bound it end to end, Core → tests → the two terminals → the docs.

### What was built

- **Core.** A blittable `DualcDiagnosticsNative` (80 bytes, a 4-byte pad after `inputEmpty`),
  the public `readonly struct DualcDiagnostics` (thirteen get-only properties, built only from
  a call that returned OK), the two 0.4.0 twins bound, the two 0.5.0 twins changed from
  `IntPtr diag` to an `out` struct, and four overloads with a trailing `out DualcDiagnostics`:
  `Contour` and `Export`, each plain and with progress + cancel. The existing progress
  overloads delegate with `out _`, so every caller compiled unchanged. `SupportsDiagnostics`
  is a second entry-point probe beside `SupportsProgress`; the plain `Contour` / `Export`
  keep calling the 0.3.0 entry points, so the old Windows DLL path never touches a newer twin.
  The usage contract is [design 01](../../design/01-native-interop.md#the-public-surface--dualcfield).
- **`Proxy preview`.** On a library that supports it, the contour goes through the
  diagnostics twin: an empty contour posts a warning and draws nothing (the cache stays
  empty, so the placeholder triangle is never shown); an open proxy posts a remark with its
  boundary and non-manifold edge counts. On an older DLL the plain contour runs, silently.
- **`Write to File`.** The two monolithic paths (with and without progress) take the
  diagnostics and, after a completed write whose contour was empty, post a warning naming the
  file that holds only the placeholder. The tiled path has no diagnostics in the ABI and
  reports nothing; cancel and restart are untouched because the warning exists only on
  success. The strings are on [command reference 04](../../command_reference/04-terminals.md)
  and in the [troubleshooting table](../../command_reference/00-shared-behaviour.md#troubleshooting).

### Design choices and the rejected probes

- **An `out` parameter, not a result record.** The ABI's own shape is "the same mesh, plus an
  out-param"; `out` keeps `DualcMeshData` the one mesh type, overloads by arity against the
  existing four methods, and `Export` returns `void` so a return-type change was never an
  option. A record would be justified only if diagnostics became the default path, and they
  do not: the plain calls must keep serving the DLL that predates 0.4.0.
- **The probe is a real no-op call.** `dualc_field_contour_with_diagnostics` is called once
  with every argument NULL through a second, pointer-typed `[DllImport]` naming the same
  `EntryPoint`; DualC writes nothing and answers `DUALC_ERR_USAGE`, and only whether the
  export resolved is observed — the same `EntryPointNotFoundException` path `SupportsProgress`
  proved on the old DLL. **Rejected: `Marshal.Prelink`** — present on netstandard2.0, but its
  throw on a missing export is implementation behaviour rather than documented contract, it
  cannot be exercised on the Rhino .NET Framework host by the net9 test suite, it needs a
  reflection lookup that an overloaded extern would break, and it is a no-op on Mono.
  **Rejected: catch-and-fall-back on the first real call** — Core would have to invent a
  diagnostics value for the fallback and there is no honest one (a default struct reads "not
  watertight", a synthetic all-clear lies), and the try/catch would smear into the solve and
  the writer's worker thread where an eager static property is what both already consume.
- **The 0.5.0 twins pass a real struct.** DualC then counts boundary and non-manifold edges on
  every progress call; negligible next to the contour, and it is what lets the writer's
  cancellable path report an empty contour at no extra entry point.
- **Zeroed is not "nothing to report".** DualC zeroes the struct on entry and fills it only
  on completion, so on `DUALC_CANCELLED` or an error it stays zero — and zero reads
  `outputWatertight == 0`. The overloads throw before constructing the managed struct, so no
  caller ever sees it; in the components "no diagnostics" is a null `DualcDiagnostics?`,
  never `default`.
- **Levels.** The empty contour is a Warning on both terminals (actionable, non-fatal: the
  component solved with what it was given); the open proxy is a Remark (an expected note
  under a bounds cut) — [design 06 § Message levels](../../design/06-conventions.md#message-levels).

### Files changed (contour diagnostics)

`src/Boletus.Core/NativeMethods.cs` (the struct, the two twins, the probe alias, the two
progress twins' `diag`), `src/Boletus.Core/DualcDiagnostics.cs` (new),
`src/Boletus.Core/DualcField.cs` (the probe, four overloads, two delegations),
`src/Boletus.Grasshopper/ProxyPreviewComponent.cs`, `src/Boletus.Grasshopper/WriteToFileComponent.cs`,
`tests/Boletus.Core.Tests/DiagnosticsTests.cs` (new), `scripts/check_data.json` (the floor),
and the docs owners: design 01 / 04 / 06 / 07, command reference 00 / 04, 07 § 9, the
decisions index (D-37 settled), the roadmap index, `STRUCTURE.md`, `native/README.md`.

### Verification (contour diagnostics)

- *Automated, Linux, the vendored `2fcd19f` library:* `dotnet build Boletus.sln` 0 warnings;
  `dotnet test` **178/178** (was 171; the floor in `scripts/check_data.json` raised to 178).
  The seven new facts: the vendored library supports diagnostics; the far-away sphere
  (DualC's own `test_diagnostics.cpp` case — `sphere(radius=0.4)` sampled in
  `(10,10,10)–(11,11,11)` at depth 5) reports `EmptyContour`, `AnyIssue`, 3 vertices, 1
  triangle, 3 boundary edges, not watertight, and its mesh really is a one-triangle mesh (a
  count check alone could never tell); the analytic gyroid∩box at depth 6 hits the golden
  counts through the diagnostics twin, in the struct and in the mesh, with a closed output and
  the input fields at the engine's defaults — the layout test, since `outputVertices` sits
  past the int/pad region; the progress overloads return the same thirteen values as the
  0.4.0 twins, for contour and for export; an export of the healthy field writes a binary STL
  whose facet count at byte 80 equals `OutputTriangles`; an export of the empty field writes
  exactly 84 + 50 bytes, one facet, flagged. The full gate green.
- *Pending manual Rhino smoke, on Windows after the DLL rebuild* ([roadmap index § Next up](../README.md#next-up)):
  (1) `Proxy preview` of a small sphere with `Min`/`Max` set far from it — the warning shows,
  nothing is drawn, Zoom Extents has nothing to frame; (2) the same volume with bounds cutting
  the sphere — the open-mesh remark with non-zero boundary edges; (3) `Write to File`
  monolithic of the far-away case — "Done", the file 134 bytes, the warning posted; tiled of
  the same — no warning (by design); (4) on the old `d6b2808` DLL, before the rebuild: both
  terminals behave as before, no exception, `SupportsDiagnostics` false.

### What it leaves

The Rhino smoke above. `DualcDiagnostics` is bound but only two of its facts are surfaced;
`BoundsFallback` and `GridBoundsExceeded` have no consumer yet (no Boletus graph bakes a root
grid, and every unbounded field is refused before the contour). The tiled writer stays
without diagnostics until the ABI gives it some — a DualC ask that joins the tiled-3MF one
(D-32) if it is ever needed.

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
