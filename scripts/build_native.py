#!/usr/bin/env python3
"""Build DualC's C ABI (and, on Windows, the GPU viewer and the DualCEditor side-car) from
the pinned submodules and drop the binaries into native/<rid>/ where the csproj copy items
expect them.

This is the one home of the recipe native/README.md used to spell out by hand; CI runs the
same script. The DualC tree is, in order of precedence:

    --dualc PATH            an explicit checkout (co-development against a sibling)
    $DUALC_ROOT             the same, from the environment
    external/DualC          the git submodule -- the pin (git submodule update --init)

and the DualCEditor tree likewise: --editor-dir PATH, $DUALCEDITOR_ROOT, external/DualCEditor.

The editor is built when Qt 6 is found (QT_ROOT_DIR / Qt6_DIR / CMAKE_PREFIX_PATH, or qmake
on PATH) on Windows and the editor tree is checked out; --editor forces it on (and fails
loudly without Qt), --no-editor off. The editor is a second CMake tree, build-native-editor/:
the editor's CMakeLists on top, DualC added from the same pinned submodule
(-DDUALCEDITOR_DUALC_DIR), and only the DualCEditor target built there -- DualC's own
targets come from the first tree. (One tree with everything was tried and dropped: the
editor adds DualC EXCLUDE_FROM_ALL, and the Visual Studio generator keeps excluded targets
out of the solution, so `cmake --build --target dualc_capi` finds no project; Ninja has no
such limit, which is why it passed on Linux.) windeployqt then lays the Qt runtime beside
DualCEditor.exe under native/x64/editor/.

Outputs (all gitignored):
    native/linux-x64/libdualc_capi.so, dualc_field           on Linux
    native/x64/dualc_capi.dll, dualc_field.exe,
               dualc_field_view.exe                          on Windows
    native/x64/editor/DualCEditor.exe + Qt runtime + LICENSES/   on Windows with Qt
    native/<rid>/BUILD-INFO.txt                              provenance of that build

Usage:
    python3 scripts/build_native.py                 # configure + build + copy + smoke test
    python3 scripts/build_native.py --viewer        # also build dualc_field_view off Windows
    python3 scripts/build_native.py --no-editor     # skip DualCEditor even when Qt is found
    python3 scripts/build_native.py --build-dir D   # default: build-native/ at the repo root
    python3 scripts/build_native.py --editor-build-dir D   # default: build-native-editor/
    python3 scripts/build_native.py --jobs N
"""
from __future__ import annotations

import argparse
import hashlib
import os
import platform
import shutil
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SUBMODULE = ROOT / "external" / "DualC"
EDITOR_SUBMODULE = ROOT / "external" / "DualCEditor"
IS_WINDOWS = platform.system() == "Windows"
RID = "x64" if IS_WINDOWS else "linux-x64"
EXE = ".exe" if IS_WINDOWS else ""

# Qt plugin folders windeployqt lays down that a Widgets + OpenGL window never loads;
# pruned after the deploy so the shipped folder stays small (the plan's footprint rule).
UNNEEDED_QT_PLUGIN_DIRS = ("imageformats", "iconengines", "networkinformation", "tls", "generic")


def die(msg: str) -> "NoReturn":  # noqa: F821 - typing only
    print("build_native: " + msg, file=sys.stderr)
    sys.exit(1)


def run(cmd: list[str], cwd: Path | None = None) -> None:
    print("+ " + " ".join(str(c) for c in cmd), flush=True)
    subprocess.run([str(c) for c in cmd], cwd=str(cwd) if cwd else None, check=True)


def run_captured(cmd: list[str], cwd: Path, env: dict[str, str], timeout: int) -> str:
    """run() with the output piped and a deadline: for the editor's --version smoke. Piped
    standard handles are what makes a Qt program on Windows print --version to the console
    instead of showing a message box when no console is attached (the message box has no
    one to click it in CI and waits forever, 2026-10-23); the deadline turns any other hang
    into a failure with a message."""
    print("+ " + " ".join(str(c) for c in cmd), flush=True)
    try:
        r = subprocess.run([str(c) for c in cmd], cwd=str(cwd), env=env, timeout=timeout,
                           capture_output=True, text=True, errors="replace")
    except subprocess.TimeoutExpired:
        die("%s did not exit within %d s" % (cmd[0], timeout))
    out = (r.stdout + r.stderr).strip()
    print(out, flush=True)
    if r.returncode != 0:
        die("%s exited %d: %s" % (cmd[0], r.returncode, out or "no output"))
    return out


def capture(cmd: list[str], cwd: Path | None = None) -> str:
    return subprocess.run([str(c) for c in cmd], cwd=str(cwd) if cwd else None,
                          check=True, capture_output=True, text=True).stdout.strip()


def source_tree(explicit: str | None, env_var: str, submodule: Path, what: str) -> Path | None:
    """The checkout of `what`: the flag, the environment, then the submodule (None if not
    checked out and nothing else is given)."""
    for label, cand in (("flag", explicit), (env_var, os.environ.get(env_var))):
        if cand:
            p = Path(cand).expanduser().resolve()
            if not (p / "CMakeLists.txt").is_file():
                die("%s=%s is not a %s tree (no CMakeLists.txt)" % (label, p, what))
            return p
    return submodule if (submodule / "CMakeLists.txt").is_file() else None


def git_commit(src: Path) -> str:
    try:
        return capture(["git", "-C", src, "rev-parse", "--short=7", "HEAD"])
    except (subprocess.CalledProcessError, FileNotFoundError):
        return "unknown"


def sha256(p: Path) -> str:
    h = hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def qt_prefix() -> Path | None:
    """The Qt installation prefix (the folder holding bin/ and lib/cmake/Qt6/), from the
    environment jurplel/install-qt-action and a hand install leave: QT_ROOT_DIR, Qt6_DIR
    (the prefix, or its lib/cmake/Qt6), CMAKE_PREFIX_PATH entries, then qmake on PATH."""
    cands: list[Path] = []
    for var in ("QT_ROOT_DIR", "Qt6_DIR", "QTDIR"):
        v = os.environ.get(var)
        if v:
            p = Path(v)
            cands += [p, p.parent.parent.parent]  # <prefix>/lib/cmake/Qt6 -> <prefix>
    for entry in os.environ.get("CMAKE_PREFIX_PATH", "").split(os.pathsep):
        if entry:
            cands.append(Path(entry))
    qmake = shutil.which("qmake") or shutil.which("qmake6")
    if qmake:
        cands.append(Path(qmake).resolve().parent.parent)
    for c in cands:
        if (c / "lib" / "cmake" / "Qt6" / "Qt6Config.cmake").is_file():
            return c.resolve()
    return None


def folder_size(p: Path) -> int:
    return sum(f.stat().st_size for f in p.rglob("*") if f.is_file())


def deploy_editor(exe: Path, out_dir: Path, qt: Path) -> None:
    """Copy DualCEditor.exe into out_dir and lay the Qt runtime beside it with windeployqt
    (release, no translations, no software-GL fallback, no D3D compiler, no VC runtime);
    then prune the plugin folders a Widgets + OpenGL window never loads."""
    if out_dir.exists():
        shutil.rmtree(out_dir)
    out_dir.mkdir(parents=True)
    shutil.copy2(exe, out_dir / exe.name)
    deploy = qt / "bin" / ("windeployqt" + EXE)
    if not deploy.is_file():
        found = shutil.which("windeployqt")
        if not found:
            die("windeployqt not found under %s/bin or on PATH" % qt)
        deploy = Path(found)
    run([deploy, "--release", "--no-translations", "--no-opengl-sw", "--no-system-d3d-compiler",
         "--no-compiler-runtime", out_dir / exe.name])
    for name in UNNEEDED_QT_PLUGIN_DIRS:
        d = out_dir / name
        if d.is_dir():
            shutil.rmtree(d)


def write_editor_licenses(out_dir: Path, qt: Path, editor_src: Path) -> list[str]:
    """editor/LICENSES/: Qt's LGPL text (the DLLs are LGPL, linked dynamically, shipped
    unmodified -- the notice the licence asks for) and DualCEditor's own licence when its
    tree has one. Returns the notes for BUILD-INFO."""
    lic = out_dir / "LICENSES"
    lic.mkdir(exist_ok=True)
    notes: list[str] = []
    # aqtinstall and the Qt installer keep the texts two levels above the prefix (<Qt>/Licenses/).
    qt_lic_dir = qt.parent.parent / "Licenses"
    copied = []
    for name in ("LICENSE.LGPL3", "LICENSE.LGPLv3", "LICENSE.GPL3-EXCEPT", "LICENSE.GPL3"):
        src = qt_lic_dir / name
        if src.is_file():
            shutil.copy2(src, lic / name)
            copied.append(name)
    notes.append("Qt licence texts: " + (", ".join(copied) if copied else "none found under %s" % qt_lic_dir))
    ed_lic = editor_src / "LICENSE"
    if ed_lic.is_file():
        shutil.copy2(ed_lic, lic / "LICENSE.DualCEditor")
        notes.append("DualCEditor licence: LICENSE.DualCEditor")
    else:
        notes.append("DualCEditor licence: no LICENSE file in the editor tree (its README says MIT)")
    (lic / "THIRD-PARTY.md").write_text(
        "# Third-party notices for the DualCEditor side-car\n\n"
        "`DualCEditor.exe` is DualCEditor (https://github.com/Giacogiak/DualCEditor), MIT, built by\n"
        "Boletus from its pinned submodule against Boletus's pinned DualC (MIT). The `Qt6*.dll`\n"
        "files and the `platforms/` and `styles/` plugins beside it are the Qt 6 framework\n"
        "(https://www.qt.io), used under the GNU Lesser General Public License v3 and linked\n"
        "dynamically; they are unmodified and may be replaced by any compatible Qt 6 build. The\n"
        "licence texts are in this folder when the Qt installation that produced this build\n"
        "shipped them; otherwise see https://www.gnu.org/licenses/lgpl-3.0.html.\n",
        encoding="utf-8")
    return notes


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--dualc", help="DualC checkout (default: $DUALC_ROOT, then external/DualC)")
    ap.add_argument("--editor-dir", help="DualCEditor checkout (default: $DUALCEDITOR_ROOT, then external/DualCEditor)")
    ap.add_argument("--build-dir", default=str(ROOT / "build-native"),
                    help="DualC's CMake build tree (default: build-native/ at the repo root)")
    ap.add_argument("--editor-build-dir", default=str(ROOT / "build-native-editor"),
                    help="DualCEditor's CMake build tree (default: build-native-editor/)")
    ap.add_argument("--viewer", action="store_true",
                    help="build dualc_field_view too (always on Windows; needs GL headers elsewhere)")
    ap.add_argument("--editor", dest="editor", action="store_true", default=None,
                    help="build DualCEditor too (default: on Windows when Qt 6 and the editor tree are found)")
    ap.add_argument("--no-editor", dest="editor", action="store_false",
                    help="skip DualCEditor even when Qt 6 is found")
    ap.add_argument("--jobs", type=int, default=0, help="parallel build jobs (default: cmake's)")
    ap.add_argument("--no-smoke", action="store_true", help="skip DualC's dualc_c_demo cancel check")
    args = ap.parse_args(argv)

    src = source_tree(args.dualc, "DUALC_ROOT", SUBMODULE, "DualC")
    if src is None:
        die("the submodule external/DualC is not checked out; run\n"
            "    git submodule update --init\n"
            "or point --dualc / DUALC_ROOT at a DualC checkout")
    editor_src = source_tree(args.editor_dir, "DUALCEDITOR_ROOT", EDITOR_SUBMODULE, "DualCEditor")
    qt = qt_prefix()
    if args.editor is None:
        editor = IS_WINDOWS and qt is not None and editor_src is not None
        if IS_WINDOWS and not editor:
            print("build_native: DualCEditor skipped (%s); Live Preview falls back to dualc_field_view"
                  % ("no Qt 6 found -- set QT_ROOT_DIR or CMAKE_PREFIX_PATH" if qt is None
                     else "external/DualCEditor is not checked out -- git submodule update --init"))
    else:
        editor = args.editor
    if editor:
        if qt is None:
            die("Qt 6 not found: set QT_ROOT_DIR (or Qt6_DIR / CMAKE_PREFIX_PATH) or pass --no-editor")
        if editor_src is None:
            die("the submodule external/DualCEditor is not checked out; run\n"
                "    git submodule update --init\n"
                "or point --editor-dir / DUALCEDITOR_ROOT at a DualCEditor checkout, or pass --no-editor")
        if not IS_WINDOWS:
            die("the editor is deployed with windeployqt and shipped for Rhino on Windows only; pass --no-editor")

    bd = Path(args.build_dir).resolve()
    ebd = Path(args.editor_build_dir).resolve()
    viewer = IS_WINDOWS or args.viewer
    out = ROOT / "native" / RID
    out.mkdir(parents=True, exist_ok=True)

    if shutil.which("cmake") is None:
        die("cmake is not on PATH")

    gen = ["-G", "Visual Studio 17 2022", "-A", "x64"] if IS_WINDOWS else (
        ["-G", "Ninja", "-DCMAKE_BUILD_TYPE=Release"] if shutil.which("ninja")
        else ["-DCMAKE_BUILD_TYPE=Release"])
    flags = [
        "-DDUALC_BUILD_C_ABI=ON",
        "-DDUALC_BUILD_EXAMPLES=ON",        # the C ABI links the examples' field-graph parser
        "-DDUALC_BUILD_TESTS=OFF",
        "-DCMAKE_POSITION_INDEPENDENT_CODE=ON",  # the static libs the .so links need PIC
        "-DDUALC_BUILD_FIELD_VIEW=" + ("ON" if viewer else "OFF"),
    ]
    print("build_native: python %s, cmake %s, editor %s" % (
        sys.version.split()[0], capture(["cmake", "--version"]).split("\n")[0], "on" if editor else "off"))
    run(["cmake", "-S", src, "-B", bd] + gen + flags)

    targets = ["dualc_capi", "dualc_field", "dualc_c_demo"] + (["dualc_field_view"] if viewer else [])
    parallel = ["--parallel", str(args.jobs)] if args.jobs else ([] if IS_WINDOWS else ["--parallel"])
    run(["cmake", "--build", bd, "--config", "Release", "--target"] + targets + parallel)

    editor_flags: list[str] = []
    if editor:
        # The second tree: the editor's CMakeLists on top, DualC add_subdirectory()'d from
        # the same submodule (the one-pin rule), only the editor's own target built.
        editor_flags = [
            "-DDUALCEDITOR_DUALC_DIR=" + src.as_posix(),   # CMake-side paths: forward slashes
            "-DDUALCEDITOR_BUILD_TESTS=OFF",
            "-DDUALC_BUILD_TESTS=OFF",
            "-DCMAKE_PREFIX_PATH=" + qt.as_posix(),  # type: ignore[union-attr]  (qt is set when editor is)
        ]
        run(["cmake", "-S", editor_src, "-B", ebd] + gen + editor_flags)
        run(["cmake", "--build", ebd, "--config", "Release", "--target", "DualCEditor"] + parallel)

    # Where the targets land: multi-config generators add a Release/ level.
    cfg = "Release" if IS_WINDOWS else ""
    capi_dir = bd / "capi" / cfg
    ex_dir = bd / "examples" / cfg
    lib_name = "dualc_capi.dll" if IS_WINDOWS else "libdualc_capi.so"
    copies = [
        (capi_dir / lib_name, out / lib_name),
        (ex_dir / ("dualc_field" + EXE), out / ("dualc_field" + EXE)),
    ]
    if viewer:
        copies.append((ex_dir / ("dualc_field_view" + EXE), out / ("dualc_field_view" + EXE)))

    if not IS_WINDOWS and shutil.which("strip"):
        run(["strip", "--strip-unneeded", capi_dir / lib_name])

    if not args.no_smoke:
        demo = capi_dir / ("dualc_c_demo" + EXE)
        with tempfile.TemporaryDirectory() as td:
            run([demo, Path(td) / "c_abi_cancel.stl", "cancel"], cwd=capi_dir)

    for s, d in copies:
        if not s.is_file():
            die("expected build output missing: %s" % s)
        shutil.copy2(s, d)
        print("copied %s -> %s" % (s.relative_to(bd), d.relative_to(ROOT)))

    editor_notes: list[str] = []
    if editor:
        exe = next((p for p in (ebd / "bin" / cfg / "DualCEditor.exe", ebd / "bin" / "DualCEditor.exe")
                    if p.is_file()), None)
        if exe is None:
            die("expected build output missing: %s" % (ebd / "bin" / cfg / "DualCEditor.exe"))
        editor_out = out / "editor"
        deploy_editor(exe, editor_out, qt)  # type: ignore[arg-type]  (qt is set when editor is)
        editor_notes += write_editor_licenses(editor_out, qt, editor_src)  # type: ignore[arg-type]
        if not args.no_smoke:
            env = dict(os.environ, QT_COMMAND_LINE_PARSER_NO_GUI_MESSAGE_BOXES="1",
                       QT_QPA_PLATFORM="offscreen")
            version = run_captured([editor_out / "DualCEditor.exe", "--version"], editor_out, env, 120)
            if "DualCEditor" not in version:
                die("DualCEditor --version answered %r" % version)
        print("deployed %s -> %s (%d files)" % (exe.relative_to(ebd), editor_out.relative_to(ROOT),
                                               sum(1 for _ in editor_out.rglob("*") if _.is_file())))

    lib = out / lib_name
    lines = [
        "DualC commit: " + git_commit(src),
        "DualC source: " + str(src),
        "Built: " + datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"),
        "Host: " + platform.platform(),
        "Flags: " + " ".join(flags),
        "sha256 %s: %s" % (lib_name, sha256(lib)),
        "size %s: %d bytes" % (lib_name, lib.stat().st_size),
    ]
    if editor:
        lines += [
            "DualCEditor commit: " + git_commit(editor_src),  # type: ignore[arg-type]
            "DualCEditor source: " + str(editor_src),
            "Editor flags: " + " ".join(editor_flags),
            "Qt prefix: " + str(qt),
            "Qt version: " + (capture([qt / "bin" / ("qmake" + EXE), "-query", "QT_VERSION"])  # type: ignore[operator]
                              if (qt / "bin" / ("qmake" + EXE)).is_file() else "unknown"),  # type: ignore[operator]
            "editor folder: %d bytes" % folder_size(out / "editor"),
        ] + editor_notes
    if not IS_WINDOWS and shutil.which("nm"):
        syms = capture(["nm", "-D", "--defined-only", lib]).splitlines()
        lines.append("exports: %d dualc_ symbols" % sum(1 for l in syms if " dualc_" in l))
    if not IS_WINDOWS and shutil.which("ldd"):
        lines.append("ldd:\n" + capture(["ldd", lib]))
    info = "\n".join(lines) + "\n"
    (out / "BUILD-INFO.txt").write_text(info, encoding="utf-8")
    print("\n" + info)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
