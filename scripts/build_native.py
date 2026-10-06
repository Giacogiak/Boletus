#!/usr/bin/env python3
"""Build DualC's C ABI (and, on Windows, the GPU viewer) from the pinned DualC tree and
drop the binaries into native/<rid>/ where the csproj copy items expect them.

This is the one home of the recipe native/README.md used to spell out by hand; CI runs the
same script. The DualC tree is, in order of precedence:

    --dualc PATH            an explicit checkout (co-development against a sibling)
    $DUALC_ROOT             the same, from the environment
    external/DualC          the git submodule -- the pin (git submodule update --init)

Outputs (all gitignored):
    native/linux-x64/libdualc_capi.so, dualc_field           on Linux
    native/x64/dualc_capi.dll, dualc_field.exe,
               dualc_field_view.exe                          on Windows
    native/<rid>/BUILD-INFO.txt                              provenance of that build

Usage:
    python3 scripts/build_native.py                 # configure + build + copy + smoke test
    python3 scripts/build_native.py --viewer        # also build dualc_field_view off Windows
    python3 scripts/build_native.py --build-dir D   # default: build-native/ at the repo root
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
IS_WINDOWS = platform.system() == "Windows"
RID = "x64" if IS_WINDOWS else "linux-x64"
EXE = ".exe" if IS_WINDOWS else ""


def die(msg: str) -> "NoReturn":  # noqa: F821 - typing only
    print("build_native: " + msg, file=sys.stderr)
    sys.exit(1)


def run(cmd: list[str], cwd: Path | None = None) -> None:
    print("+ " + " ".join(str(c) for c in cmd), flush=True)
    subprocess.run([str(c) for c in cmd], cwd=str(cwd) if cwd else None, check=True)


def capture(cmd: list[str], cwd: Path | None = None) -> str:
    return subprocess.run([str(c) for c in cmd], cwd=str(cwd) if cwd else None,
                          check=True, capture_output=True, text=True).stdout.strip()


def dualc_root(explicit: str | None) -> Path:
    for label, cand in (("--dualc", explicit), ("DUALC_ROOT", os.environ.get("DUALC_ROOT"))):
        if cand:
            p = Path(cand).expanduser().resolve()
            if not (p / "CMakeLists.txt").is_file():
                die("%s=%s is not a DualC tree (no CMakeLists.txt)" % (label, p))
            return p
    if not (SUBMODULE / "CMakeLists.txt").is_file():
        die("the submodule external/DualC is not checked out; run\n"
            "    git submodule update --init\n"
            "or point --dualc / DUALC_ROOT at a DualC checkout")
    return SUBMODULE


def dualc_commit(src: Path) -> str:
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


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--dualc", help="DualC checkout (default: $DUALC_ROOT, then external/DualC)")
    ap.add_argument("--build-dir", default=str(ROOT / "build-native"),
                    help="CMake build tree (default: build-native/ at the repo root)")
    ap.add_argument("--viewer", action="store_true",
                    help="build dualc_field_view too (always on Windows; needs GL headers elsewhere)")
    ap.add_argument("--jobs", type=int, default=0, help="parallel build jobs (default: cmake's)")
    ap.add_argument("--no-smoke", action="store_true", help="skip DualC's dualc_c_demo cancel check")
    args = ap.parse_args(argv)

    src = dualc_root(args.dualc)
    bd = Path(args.build_dir).resolve()
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
    run(["cmake", "-S", src, "-B", bd] + gen + flags)

    targets = ["dualc_capi", "dualc_field", "dualc_c_demo"] + (["dualc_field_view"] if viewer else [])
    build = ["cmake", "--build", bd, "--config", "Release", "--target"] + targets
    if args.jobs:
        build += ["--parallel", str(args.jobs)]
    elif not IS_WINDOWS:
        build += ["--parallel"]
    run(build)

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

    lib = out / lib_name
    lines = [
        "DualC commit: " + dualc_commit(src),
        "DualC source: " + str(src),
        "Built: " + datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"),
        "Host: " + platform.platform(),
        "Flags: " + " ".join(flags),
        "sha256 %s: %s" % (lib_name, sha256(lib)),
        "size %s: %d bytes" % (lib_name, lib.stat().st_size),
    ]
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
