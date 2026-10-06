# 10 — Public delivery: the repo as others build it

**DONE** — 2026-10-06, three items; the publication step itself is dated in § Update. The
record of taking Boletus public: the fresh root and the offline archive of the history that
preceded it, DualC turned from vendored binaries into a git submodule the build compiles,
and the gate hosted as CI with one `.yak` as its artifact. Born 2026-10-06 from the owner's
decision to publish, on the model of DualC's own `docs/roadmap/20-public-delivery.md`
(2026-10-03). Headings are frozen at ID + title; status, date and evidence live on the body
lines. The present-tense rules live in [design 06 § Local only](../design/06-conventions.md#local-only)
and [design 06 § Provenance by commit](../design/06-conventions.md#provenance-by-commit); the
decisions are [D-46, D-47 and D-10](../decisions/01-settled.md), [D-48 and
D-04](../decisions/README.md). The plan as drafted is
[raw/2026-10-06](../raw/2026-10-06-public-delivery-plan.md).

## #32 Published with a fresh root, the history archived offline

**DONE — 2026-10-06.** For legal reasons the public repository carries no pre-publication
history: one root commit, "Initial public release of Boletus", made from the final prepared
tree, and the remote `https://github.com/Giacogiak/Boletus` created empty by the owner and
pushed once that commit existed. The procedure is DualC's, step for step:

1. Every preparation of this block (#33, #34, the license, the docs) landed as ordinary
   gate-green commits on the old `main`, so the archive holds the finished work too.
2. `git bundle create … main` of that `main` — one ref, like DualC's bundle — verified with
   `git bundle verify`, zipped as `~/Documents/Boletus-history-2026-10-06.zip` (one file in
   the zip, no folder, the loose bundle deleted), test-cloned once with `-b main`.
3. `git checkout --orphan`, one commit of the same tree, `git branch -M … main`, reflog
   expired, `git gc --prune=now`; `git rev-list --all --max-parents=0` prints one hash.
4. The remote added, `main` pushed, `git ls-remote` showing `refs/heads/main` alone.

The restore procedure — a separate read-only clone of the bundle, or a frozen
`refs/archive/` ref inside the working repo with an untracked `pre-push` guard that refuses
any push whose root is not the public one — is the owner's guide
`~/Documents/git-history-restore-guide.md`, one file for DualC and Boletus, outside every
repo. Two Boletus-specific notes it carries: the archive holds the submodule as a gitlink only
(a restored clone needs `git submodule update --init`, which fetches DualC from GitHub), and
the old history still has the binaries committed, so a restored tree is never pushed.

**What it means for the docs.** Every commit hash cited in `docs/` before this date names a
commit of the archive, not of the public history — the same note DualC's record carries.
The `D:\DualC` paths in dated roadmap text are the historical sibling checkout and stay as
written; the rewritable pages point at the submodule.

**Added with the root.** `LICENSE` (MIT, the same text as DualC's) and `CITATION.cff`
(version `0.1.0`, the plugin's own, equal to `BoletusInfo.Version` and the Yak manifest —
the gate's `yak-version` check keeps the last two equal).

**Update — publication.** *(filled in by the session that pushes: the root hash, the archive's
tip hash and commit count, the date.)*

## #33 DualC as a git submodule — the binaries built, never committed

**DONE — 2026-10-06.** Until this item the C ABI (`native/x64/dualc_capi.dll`, 0.3.0 at
`d6b2808`; `native/linux-x64/libdualc_capi.so`, 0.5.0 at `2fcd19f`) and the viewer
(`d6b2808`) were committed binaries with hand-written provenance tables ([02](02-dependency-strategy.md),
D-01; [09/10](09-docs-layers/10-linux-native.md), D-44) — and the Windows pair was two
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
  before the native build; `dualc_field` rides along to the test output.
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
  history here and in [09/10](09-docs-layers/10-linux-native.md).

**Verification.** The Linux build from the submodule: `libdualc_capi.so` 2,052,776 bytes,
the same size as the committed one it replaced, 20 `dualc_` exports, `ldd` libstdc++ / libm /
libgcc_s / libc, `dualc_c_demo … cancel` green; the gate's build tier — recorded on the line
below once the run that follows this record finishes.
- Result: *(filled by the closing gate run.)*

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

## #34 CI — the gate as a GitHub Actions workflow and one `.yak`

**DONE — 2026-10-06** (the first green run is recorded in § Update of #32).
`.github/workflows/ci.yml`: on every push, pull request and manual dispatch, concurrency per
ref. Two jobs, each `actions/checkout` with `submodules: true`, the DualC gitlink read with
`git rev-parse HEAD:external/DualC` as the cache key's second half:

- **`linux`** (`ubuntu-24.04`): `cmake ninja-build g++`, .NET 9, `actions/cache` on
  `build-native/` keyed on OS + the pin + the script's hash (the DualC tree changes only on
  a pin bump, so caching its objects is the right trade — the opposite of DualC's own
  workflow, whose source changes every push); `build_native.py`; `check.py` (the full
  gate), `check.py --docs --strict`, `check.py --selftest`.
- **`windows`** (`windows-2022`): the same build and gate with Visual Studio 2022, the
  viewer included; then a Release build of the `.gha`, the four files staged with
  `yak/manifest.yml`, McNeel's standalone `yak.exe` downloaded and `yak build --platform win`
  run, and two artifacts uploaded: `boletus-yak` (the package) and `boletus-gha-folder`
  (the bare folder for `%APPDATA%\Grasshopper\Libraries`). No `yak push`, no release job, no
  secrets.

Rhino itself never runs in CI: the `.gha` is compiled and packaged, and the Rhino smoke tests
stay a manual step on a Windows machine — on the CI `.yak` from here on.

---

← Back to the [Roadmap index](README.md).
