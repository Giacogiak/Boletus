# 10/02 — DualC as a submodule, the binaries built

Part of [10 — Public delivery](README.md). The headings below are the item's, verbatim.

## #33 DualC as a git submodule — the binaries built, never committed

**DONE — 2026-10-06.** Until this item the C ABI (`native/x64/dualc_capi.dll`, 0.3.0 at
`d6b2808`; `native/linux-x64/libdualc_capi.so`, 0.5.0 at `2fcd19f`) and the viewer
(`d6b2808`) were committed binaries with hand-written provenance tables ([02](../02-dependency-strategy.md),
D-01; [09/10](../09-docs-layers/10-linux-native.md), D-44) — and the Windows pair was two
ABI levels behind the pin, waiting for a Windows machine. Publishing binaries in a public
repo is the wrong shape (an opaque 4 MB in every clone, unverifiable, and behind the pin
whenever one platform cannot build), and DualC is public at
`https://github.com/Giacogiak/DualC` since 2026-10-03, so the dependency can be *source*.

**What landed.**
- `external/DualC`, a git submodule of the public DualC, pinned at `2fcd19f` — the commit
  `native/README.md` already pinned. The gitlink is the one home of the pin; no page restates
  the hash. At this date `2fcd19f` is on GitHub only through DualC's `ci/gate-workflow`
  branch (its `main` on GitHub is still the root `5989fb5`); the owner accepted that and
  re-pins to a `main` commit once DualC's `main` is pushed.
- `scripts/build_native.py`: the recipe `native/README.md` § How to refresh spelled out by
  hand, as one cross-platform script — DualC root from `--dualc`, `DUALC_ROOT`, else the
  submodule; `-DDUALC_BUILD_C_ABI=ON -DDUALC_BUILD_EXAMPLES=ON -DDUALC_BUILD_TESTS=OFF
  -DCMAKE_POSITION_INDEPENDENT_CODE=ON` (+ `FIELD_VIEW` on Windows or `--viewer`) into
  `build-native/`; targets `dualc_capi dualc_field dualc_c_demo` (+ `dualc_field_view`);
  DualC's own `dualc_c_demo … cancel` smoke test; `strip` on Linux; the outputs copied into
  `native/<rid>/` with a `BUILD-INFO.txt` (commit, host, flags, sha256, exports, `ldd`). This
  settles D-10 (`update-native.ps1` was promised 2026-06-17 and never written).
- The three binaries un-tracked (`git rm --cached`); `.gitignore` ignores `native/x64/`,
  `native/linux-x64/`, `build-native/` where it used to un-ignore the three files.
- `Boletus.Core.csproj` / `Boletus.Grasshopper.csproj`: the native `<None>` items are
  `Exists`-conditioned and platform-conditioned, so a checkout compiles its managed code
  before the native build; `dualc_field` rides along to the test output (copied by the test
  project alone since the seventh semantic-lint run, so it never lands beside the `.gha`).
- The tests: the four `FindCli()` copies collapsed into `TestPaths.cs` — `dualc_field` beside
  the assembly first, then `DUALC_FIELD_EXE`, then the historical `D:\` path; the samples
  from `DUALC_SAMPLES_DIR`, else the submodule's `examples/samples` found by walking up to
  `Boletus.sln`. On Linux the CLI-parity and fixture round-trips run for the first time
  without an environment variable.
- The gate: `native-built` (build tier) fails with the build command when the library is
  missing instead of `dotnet test` dying in the first P/Invoke; `dualc-links` reads the
  submodule first (`dualc_roots`), so citations are checked against the pinned docs rather
  than a sibling's newer ones; `external/DualC` is a summarized prefix of the `structure`
  check.
- `native/README.md` rewritten around the build; the vendored era's provenance rows are
  history here and in [09/10](../09-docs-layers/10-linux-native.md).

**Verification.** The Linux build from the submodule: `libdualc_capi.so` 2,052,776 bytes,
the same size as the committed one it replaced, 20 `dualc_` exports, `ldd` libstdc++ / libm /
libgcc_s / libc, `dualc_c_demo … cancel` green; then the gate on the prepared tree.
- Result: `--fast` 26 checks green (27 with `yak-version`), `--selftest` 30 fixture runs,
  `--build` 178 tests passed at the floor of 178 with 0 warnings — the CLI-parity and fixture
  round-trips among them, run against the `dualc_field` built from the submodule; `--docs
  --strict` green after the commit.

**Rejected.**
- *Keep vendoring, publish the binaries.* Opaque, unverifiable, and the Windows pair would
  have gone public two ABI levels behind the pin.
- *`add_subdirectory` of DualC from MSBuild* (a CMake invocation inside the csproj). DualC's
  CMake needs its examples on for the C ABI and defaults differ when it is not top-level;
  one script outside MSBuild keeps the two build systems apart and is what CI runs.
- *A DualC release artifact (a per-OS library published by DualC's CI).* DualC's hosted-CI
  plan names a release job out of scope; its trigger stands there (DualC #8's "CI artifact").
- *A local NuGet feed* (D-04): moot once the submodule is built in CI — DROPPED.

### The single-file `.gha` with embedded natives

**DEFERRED** — trigger: a user who needs one bare `.gha` where the `.yak` or the four-file
folder cannot be used (D-48). The owner asked whether everything could be one file with
DualC "statically linked": a `.gha` is a managed assembly and native code cannot be linked
into it; the two real single-file shapes are the `.yak` (Rhino's own install unit — the
`.gha`, `Boletus.Core.dll`, `dualc_capi.dll` and `dualc_field_view.exe` side by side, so the
resolver and the launcher work unchanged) and a `.gha` that embeds the natives as resources
and extracts them at first load into a per-user cache. The `.yak` was chosen (#34); the
embedded variant costs first-load extraction, an executable written to disk at runtime
(SmartScreen / antivirus noise) and a second path in the resolver and the launcher.

---

← Back to the [block index](README.md) · the [Roadmap index](../README.md).
