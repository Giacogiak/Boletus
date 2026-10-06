# Gate portability — the docs workflow on a second machine

Part of the [Docs layers record](README.md) (roadmap 09). A post-close child, not a phase: the
phase table closed on 2026-09-21. On 2026-10-02 the repo was opened on a Linux machine
(siblings under one folder, no `D:` drive), and the gate that
[`/repo-docs-lifecycle`](../../../AGENTS.md) runs at both ends of a session could not go green
there. Item **#29**, decision [D-42](../../decisions/01-settled.md).

**DONE (2026-10-02).**

## What failed

- **No `python`.** The machine has `python3` only. The `Stop` hook in `.claude/settings.json`
  and `scripts/hooks/pre-commit` both called `python`, so neither ran, and nothing said so.
- **`dualc-links` skipped.** `check_data.json` named one root, `D:/DualC`. Without it the
  check answered SKIP, and `--docs --strict` counts a skip as a failure (`check.py`, the
  `failed =` line of `main`). So the session-end gate could not pass on this machine.
- **Six build outputs read as stale.** With `DUALC_ROOT=../DualC` set by hand, the check
  listed six citations as missing: `build/capi/Release/dualc_capi.dll` and
  `build/examples/Release/dualc_field_view.exe` in `native/README.md`, and
  `build/examples/Release/dualc_field.exe` in four test files. They are the provenance of the
  vendored binaries and the CLI the parity tests run. They are not pages, and an unbuilt
  checkout does not have them. The second lint run ([06/02](06-semantic-lint/README.md))
  had read those lines the same way.
- **Outside the repo**, the machine also lacked `~/.claude/commands/docs-semantic-lint.md` and
  this repo's memory. Both were restored from the `_claude-home/` backups that DualC's and
  Boletus's `RESTORE.md` describe; the skill copy was already identical.

## What changed

| File | Change |
| --- | --- |
| `scripts/check_data.json` | `dualc_links.dualc_root` → `dualc_roots`: `D:/DualC`, then `../DualC`, relative to the repo root; the first existing one wins, `DUALC_ROOT` still overrides. New `build_output_prefixes`: `build/`. |
| `scripts/check.py` | `dualc-links` resolves the root from that list; a cited path under a build prefix is stat'ed only when the checkout has that top folder, and counted as an unbuilt build output otherwise. |
| `scripts/check_fixtures/dualc-links/` | `pass` resolves through a missing first root and cites an unbuilt build output; `fail` has a `build/` folder and cites a build output that is not in it. |
| `scripts/hooks/pre-commit`, `.claude/settings.json` | the interpreter is `$(command -v python3 \|\| command -v python)`. |
| `AGENTS.md` | the hook paragraph names both roots and the `python3` rule. |

## Verified

- `python3 scripts/check.py --selftest`: **28 fixture runs, 0 wrong** (26 before; the two new
  runs are the `dualc-links` pair, which now covers both rules).
- `python3 scripts/check.py --docs --strict` with no environment variable: **26 checks, 26
  passed, 0 skipped**; `dualc-links` 0 missing, 6 build outputs unbuilt.
- `sh scripts/hooks/pre-commit` and the `Stop` command, both run from bash: PASS.
- Not run: the dotnet tier. There is no .NET SDK on this machine, so the full
  `python3 scripts/check.py` stays a Windows-machine command. This step touches no `.cs` file.
- *(2026-10-02, later the same day:)* the .NET SDK 9.0.318 was installed user-locally
  (`~/.dotnet`, with `DOTNET_ROOT` and `PATH` set in `~/.bashrc`). `dotnet build Boletus.sln`
  needs `-p:EnableWindowsTargeting=true` on Linux, because the Grasshopper project targets
  `net7.0-windows`; with it, the build has 0 warnings and 0 errors. `dotnet test` then gives
  **99 passed, 18 failed of 117**. The 18 are the tests that P/Invoke into `dualc_capi`: only
  the Windows `dualc_capi.dll` is vendored, and Linux looks for `libdualc_capi.so`. So the full
  gate stays red on Linux until a Linux build of the DLL is vendored, which is a question
  for [`native/README.md`](../../../native/README.md) and the vendoring decisions, not taken
  here. The gate's floor of 117 is measured on Windows.
- *(2026-10-03:)* closed by [10](10-linux-native.md): the C ABI of the pinned commit is
  vendored for Linux and the full gate is green there, 117 of 117 (D-44).

DualC's hooks call `python` the same way; that is reported in
[§ DualC handoff](README.md#dualc-handoff), not fixed there.

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
