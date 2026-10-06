# Native binaries — built from the DualC submodule, never committed

This folder receives the **DualC C ABI** that `Boletus.Core` P/Invokes, DualC's
**`dualc_field` CLI** (the oracle of the parity tests) and, on Windows, the **GPU raymarch
viewer** the Phase-5 live-preview side-car launches. Everything below `x64/` and
`linux-x64/` is a build output and gitignored: nothing prebuilt is in the repository, on the
developer's machine or in CI. Rationale and alternatives:
[`../docs/roadmap/02-dependency-strategy.md`](../docs/roadmap/02-dependency-strategy.md) and
[`../docs/roadmap/10-public-delivery.md`](../docs/roadmap/10-public-delivery.md).

## The pin

Boletus pins DualC by **commit**, and the commit is the one the git submodule
`external/DualC` points at — one home, read with:

```
git -C external/DualC rev-parse --short HEAD      # or: git ls-tree HEAD external/DualC
```

Moving the pin is a submodule bump (`git -C external/DualC checkout <commit>` + `git add
external/DualC`), rebuilt with the script below and recorded in the roadmap. The Windows
and Linux libraries are therefore always built from the same commit; the plugin still probes
the loaded library's **ABI level** by entry point (`DualcField.SupportsProgress`,
`SupportsDiagnostics`), never by version string — see § The version trap.

## How to build

```
git submodule update --init                       # once per clone
python scripts/build_native.py                    # Linux: python3
```

The script (`scripts/build_native.py`) is the one home of the recipe; CI runs the same
command. It takes the DualC tree from `--dualc PATH`, else `DUALC_ROOT`, else the submodule;
configures `build-native/` (gitignored; `--build-dir` overrides) with
`-DDUALC_BUILD_C_ABI=ON -DDUALC_BUILD_EXAMPLES=ON -DDUALC_BUILD_TESTS=OFF
-DCMAKE_POSITION_INDEPENDENT_CODE=ON` (Visual Studio 2022 x64 on Windows, Ninja Release
elsewhere; the viewer with `-DDUALC_BUILD_FIELD_VIEW=ON` on Windows, or `--viewer`); builds
`dualc_capi`, `dualc_field`, `dualc_c_demo` (and `dualc_field_view`); runs DualC's own
`dualc_c_demo … cancel` check against the fresh library; strips the `.so`; and copies:

| Platform | Into | Files |
| --- | --- | --- |
| Windows | `x64/` | `dualc_capi.dll`, `dualc_field.exe`, `dualc_field_view.exe` |
| Linux | `linux-x64/` | `libdualc_capi.so`, `dualc_field` |

plus a `BUILD-INFO.txt` beside them: the DualC commit, host, flags, the library's sha256 and
size, and on Linux its `dualc_` export count and `ldd`. That file is the provenance of the
build on this machine; it is gitignored like the binaries.

Prerequisites: CMake, a C++17 toolchain (Visual Studio 2022; or `g++` + Ninja, `cmake
ninja-build g++` on Ubuntu), network on the first configure (geometry-central, Eigen; GLFW
for the viewer). The viewer off Windows also needs the X11 development headers DualC's
README lists. `dualc_field` is not vendored anywhere: the csproj copies it from here beside
the test assembly, and the CLI-backed tests run instead of returning early; `DUALC_FIELD_EXE`
and `DUALC_SAMPLES_DIR` still override.

The csproj copy items are `Exists`-conditioned, so the managed projects compile before the
native build has run; the gate's `native-built` check fails with the command above when the
library is missing, instead of `dotnet test` failing inside the first P/Invoke.

## What the library is

`dualc_capi` is a `SHARED` library that statically links `libdualc`, the examples'
field-graph parser and the STL/3MF/OBJ writers, so it is self-contained apart from the C++
runtime (`MSVCP140` / `VCRUNTIME140*` + the UCRT on Windows — roadmap
[06](../docs/roadmap/06-phase4-distribution-and-packaging.md) /
[07](../docs/roadmap/07-upstream-coordination/README.md); `libstdc++`, `libm`, `libgcc_s`,
`libc` on Linux). It exports every `dualc_` entry point `capi/dualc_c.h` declares at the pin,
each bound by one `[DllImport]` in `Boletus.Core` — the count is
[design 01](../docs/design/01-native-interop.md). `[DllImport("dualc_capi")]` names no
extension, so the runtime probes `dualc_capi.dll` on Windows and `libdualc_capi.so` on
Linux; the `.gha` adds a resolver so Rhino finds the DLL beside the plugin.

`dualc_field_view` is the GPU field-graph raymarch viewer (opt-in in DualC's build): a
self-contained executable (GLFW/glad static) that needs an OpenGL 3.3 driver, auto-reloads
the graph file on change, and is launched by `LivePreviewComponent` as a separate process —
not part of the C ABI, no P/Invoke. Why/how in DualC's `docs/roadmap/12-field-graph-and-app/`
and `docs/command_reference/12-dualc_field_view/README.md`.

## The version trap

`dualc_version()` moves with the **ABI** (0.3.0 → 0.4.0 diagnostics → 0.5.0 cancel/progress),
not with the field-graph **vocabulary** — the parser is compiled *inside* this library
(`capi/CMakeLists.txt` links `dualc_examples_fieldgraph` PRIVATE), and two DualC commits with
the same version string once accepted different op sets (`e345bf3` rejected the strut-lattice
ops `d6b2808` parses; the record is [07 § 8](../docs/roadmap/07-upstream-coordination/03-export-callback-and-strut-sync.md#8-strut-lattice-vocabulary-sync-d6b2808--done-boletus-side-2026-07-10)).
So the pin is a commit, never a version, and a vocabulary change in DualC is a submodule
bump plus the `Ops.cs` registry, in one commit.

## Contract

The authoritative interface is `dualc_c.h` in the DualC tree (`external/DualC/capi/dualc_c.h`).
Treat it as the pinned contract the wrapper targets. `Boletus.Core` does **not** assert
`DualcField.Version()` at startup — an assert on a string that does not change between the
commits Boletus pins would prove nothing (the decision row is in
`docs/decisions/README.md`). What it does instead is probe the **ABI level** once, by entry
point: `DualcField.SupportsProgress` is true when the loaded library exports the 0.5.0
cancel token, false (an `EntryPointNotFoundException`, caught) on an older build — so one
managed build runs on both and the component chooses its path at runtime.

The provenance tables of the vendored era (the `d6b2808` DLL, the `2fcd19f` `.so` with its
sha256) are history: roadmap [09/10](../docs/roadmap/09-docs-layers/10-linux-native.md) and
[10 #33](../docs/roadmap/10-public-delivery.md).
