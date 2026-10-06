# The full gate on Linux — a Linux build of the C ABI

Part of the [Docs layers record](README.md) (roadmap 09). A post-close child, the sequel of
[08](08-gate-portability.md): that entry left the full gate red on Linux, **99 passed, 18
failed of 117**, because only the Windows `dualc_capi.dll` was vendored. Item **#31**,
decision [D-44](../../decisions/01-settled.md).

**DONE (2026-10-03).**

## What was done

- **Tools, user-local.** No `sudo` on the machine, no `cmake`. `uv tool install cmake ninja`
  put CMake 4.4.3 and Ninja 1.13.2 in `~/.local/bin`; `gcc`/`g++` 15.2 were already there.
- **The pinned commit.** DualC was republished on 2026-10-03 with a fresh root (`5989fb5`,
  version 0.5.0); `d6b2808`, the commit `native/README.md` pins, exists only in the history
  bundle `~/Documents/DualC-history-2026-10-03.zip`. It was cloned from the bundle into a
  session scratch folder (no remote, nothing under `~/Documents`), checked out at
  `d6b2808`, and built against the sibling `geometry-central` (`-DDUALC_GC_DIR`). The sibling
  0.5.0 checkout was not used: a re-vendor to 0.5.0 is Phase C's step
  ([07 § 7](../07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c)),
  not a platform port's.
- **Two upstream gaps, worked around at configure time, not patched:**
  1. `capi/dualc_c_demo.c` is C, and `project(dualc … LANGUAGES CXX)` at `d6b2808` never
     enables C (MSVC tolerates it, Ninja/GCC does not: `CMAKE_C_COMPILE_OBJECT` missing).
     Passed `-DCMAKE_PROJECT_dualc_INCLUDE=<file with enable_language(C)>`. DualC HEAD has
     `enable_language(C)` already.
  2. `libdualc_capi.so` links the static `libdualc.a` and the example libraries, which are
     not built position-independent: `relocation R_X86_64_PC32 … recompile with -fPIC`.
     Passed `-DCMAKE_POSITION_INDEPENDENT_CODE=ON`. DualC HEAD sets PIC on the
     `dualc_capi` target only, so the gap is still there — reported in
     [§ DualC handoff](README.md#dualc-handoff).
- **The configure line** (Release, Ninja): `-DDUALC_BUILD_C_ABI=ON -DDUALC_BUILD_EXAMPLES=ON
  -DDUALC_BUILD_TESTS=OFF` plus the two flags above; targets `dualc_capi` and `dualc_field`.
  The post-build copy of the demo meshes fails (`data/cube.obj` is not in that tree); both
  binaries link before it.

## What changed

| File | Change |
| --- | --- |
| `native/linux-x64/libdualc_capi.so` | new, vendored: the C ABI of `d6b2808`, stripped; 11 `dualc_` exports, the same 11 the `[DllImport]`s bind; depends on libstdc++/libc only |
| `native/README.md` | its provenance table and the Linux refresh recipe |
| `.gitignore` | un-ignores the `.so`, and the fixtures' `Release/` folders (the `[Rr]elease/` rule had hidden the new `fail` fixture from git) |
| `src/Boletus.Core/Boletus.Core.csproj` | copies the `.so` to the output on Linux; `[DllImport("dualc_capi")]` probes `libdualc_capi.so` there, no resolver |
| `scripts/check.py`, `check_data.json` | `dotnet.non_windows_args` (`-p:EnableWindowsTargeting=true`), passed by the build tier off Windows |
| `scripts/check.py`, `scripts/check_fixtures/dualc-links/` | a cited build output is stat'ed only when its own folder exists — the sibling DualC got a Linux build at 11:47 the same day, whose single-config `build/examples/` has no `Release/`, and the D-42 rule read the six Windows citations as missing |
| `tests/…/FieldGraphTests.cs` | `SamplesDir` takes `DUALC_SAMPLES_DIR` before `D:\DualC\examples\samples` |

## Verified

- `python3 scripts/check.py`, no environment variable: **28 checks, 0 failed**;
  `dotnet-build` 0 warnings, `dotnet-test` **117 passed** (floor 117).
- `--selftest`: **28 fixture runs, 0 wrong**; `--docs --strict` green.
- With `DUALC_FIELD_EXE` = the Linux `dualc_field` of `d6b2808` and `DUALC_SAMPLES_DIR` its
  `examples/samples`: **117 passed** — the CLI-parity, round-trip and fixture tests run, not
  skip. Proven by sabotage: `DUALC_FIELD_EXE=/usr/bin/false` fails them (the fixture pair 2 of
  2). Without the variables they return early, as on a Windows machine with no DualC build.
- **Not verifiable on Linux:** anything that needs Rhino. The `.gha` compiles against the
  Grasshopper reference packages, but no Rhino 8 runs on Linux (Windows and macOS only), so the
  manual smoke tests the 05 records list as pending stay pending. `dualc_field_view.exe` and
  the Live Preview side-car are not built for Linux: the side-car is launched by a Grasshopper
  component, and the viewer needs the sibling `polyscope`.

**The `.so` rebuilt at the pin (2026-10-05).** The second Linux build, from the sibling
checkout at `2fcd19f` (ABI 0.5.0) for `Write to File` Phase C: the `enable_language(C)`
workaround above is no longer needed (DualC #49), the PIC flag still is, geometry-central
came from the copy DualC's own build had fetched. Because this machine cannot build the
Windows DLL, the "same commit on both platforms" clause of D-44 is suspended until the
Windows rebuild; the provenance, the gap and the runtime probe that bridges it are
[`native/README.md`](../../../native/README.md) and
[07 § 7](../07-upstream-coordination/03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c).

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
