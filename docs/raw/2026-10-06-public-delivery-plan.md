# Publish Boletus on GitHub: fresh root (the DualC procedure), DualC as a submodule, CI + a .yak artifact

## Context

Boletus goes public at `https://github.com/Giacogiak/Boletus`, for legal reasons with **no
pre-publication history**: one fresh root commit; the old commits (69 today) survive only in a
zip outside the repo, restorably. This is exactly what DualC did on 2026-10-03 (guide:
`~/Documents/git-history-restore-guide.md`; record: DualC `docs/roadmap/20-public-delivery.md`
§ Update 2026-10-03; archive `~/Documents/DualC-history-2026-10-03.zip` = one `git bundle` of
`main`, no folder inside the zip, the loose bundle deleted).

Decided by the owner in this session:
- **License as DualC**: MIT (Copyright (c) 2026 Giacomo Forcina) + a `CITATION.cff`.
- **No binaries in the public tree.** The vendored `native/x64/dualc_capi.dll`,
  `native/x64/dualc_field_view.exe`, `native/linux-x64/libdualc_capi.so` are replaced by a
  **git submodule of the public DualC at `external/DualC`**, pinned at the current pin
  `2fcd19f` (reachable on GitHub only via the `ci/gate-workflow` branch for now — accepted;
  re-pin to a `main` commit once DualC's `main` is pushed/merged).
- **CI**: a GitHub Actions workflow builds DualC from the submodule, runs the gate, and the
  Windows job publishes **one `.yak` package** as the artifact (gha + Core.dll +
  dualc_capi.dll + dualc_field_view.exe + manifest.yml). Never pushed to the Yak server.
  (A native library cannot be statically linked into a managed `.gha`; the `.yak` is Rhino's
  single-file unit. Embedding-and-extracting was offered and not chosen.)
- Every "local only / never published / native never leaves the machine / no remote / no CI"
  statement is rectified (as DualC's commit `3dd0346` did); headings and anchors never change,
  roadmap topic files get dated appends only, rewritable pages are rewritten.
- The restore guide becomes **one file for both repos**.
- The owner creates the empty GitHub repo in the browser; the push happens only after they
  confirm it exists. Nothing else outward happens.

## Order of work

All preparation lands as ordinary gate-green commits on the current `main` (they end up in the
archive, not on GitHub). Then: archive → fresh root → push → one docs commit recording the
remote and the root hash (DualC's `3dd0346` pattern) → CI green.

1. Submodule + `scripts/build_native.py` + csproj/tests/gate changes; binaries un-tracked
2. CI workflow + Yak manifest
3. LICENSE, CITATION.cff, docs rectified, `docs/raw/2026-10-06-public-delivery-plan.md`
4. Gate green: `python3 scripts/check.py` and `python3 scripts/check.py --docs --strict`
5. Archive the old history (bundle → zip), verify; update the guide
6. Fresh root commit from the final tree, purge old objects, verify one root
7. Owner confirms the empty repo exists → add remote, push `main`
8. Docs commit "Boletus is published": remote URL, root hash, CI status; push; watch CI

---

## Step 1 — DualC as a submodule, binaries built not vendored

**Submodule**
```bash
git submodule add https://github.com/Giacogiak/DualC.git external/DualC
git -C external/DualC fetch origin ci/gate-workflow && git -C external/DualC checkout 2fcd19f
git add .gitmodules external/DualC
```
`.gitmodules` gets `branch = main` left out (a plain pinned gitlink). The gitlink **is the pin**:
`native/README.md` stops stating a commit hash and says "the pin is the submodule's commit"
(`git -C external/DualC rev-parse --short HEAD`). This retires the "Windows DLL behind the pin"
gap (both platforms now build from the one commit).

**Un-track the binaries** (`git rm --cached` the three files; they stay on disk, gitignored):
- `.gitignore`: delete the un-ignore block (L19–25); add `native/x64/`, `native/linux-x64/`,
  `build-native/`.
- `native/README.md` (rewrite, rewritable page): what lands in `native/<rid>/`, how it is
  built (the script), provenance = the gitlink + the script's printed sha256/nm/ldd; keep the
  "version trap" paragraph (pin by commit, not by version string) and the ABI contract section.

**`scripts/build_native.py`** (new; Python like `check.py`, cross-platform; the one home of the
recipe that `native/README.md` § How to refresh holds today and CI reuses):
- DualC root: `--dualc PATH` > `$DUALC_ROOT` > `external/DualC` (error with
  `git submodule update --init` if the gitlink is uninitialised).
- Configure into `build-native/` (`--build-dir` override): Linux `-G Ninja -DCMAKE_BUILD_TYPE=Release`,
  Windows `-G "Visual Studio 17 2022" -A x64`; flags `-DDUALC_BUILD_C_ABI=ON
  -DDUALC_BUILD_EXAMPLES=ON -DDUALC_BUILD_TESTS=OFF -DCMAKE_POSITION_INDEPENDENT_CODE=ON`, plus
  `-DDUALC_BUILD_FIELD_VIEW=ON` on Windows (or `--viewer` anywhere).
- Targets: `dualc_capi dualc_field dualc_c_demo` (+ `dualc_field_view` on Windows).
- Copy outputs: Linux `build-native/capi/libdualc_capi.so` (stripped) + `build-native/examples/dualc_field`
  → `native/linux-x64/`; Windows `build-native/capi/Release/dualc_capi.dll`,
  `build-native/examples/Release/dualc_field.exe`, `dualc_field_view.exe` → `native/x64/`.
- Smoke: run `dualc_c_demo <tmp>.stl cancel` (the upstream check native/README already records).
- Print a provenance block (DualC commit, sha256, export count, ldd on Linux) and write it to
  `native/<rid>/BUILD-INFO.txt` (gitignored).

**csproj**
- `src/Boletus.Core/Boletus.Core.csproj` L21–32 and `src/Boletus.Grasshopper/Boletus.Grasshopper.csproj`
  L49–66: add `Condition="Exists('…')"` to every native copy item so a checkout without a
  native build still compiles managed code; the Windows DLL/exe items additionally get
  `$([MSBuild]::IsOSPlatform('Windows'))`. Rewrite the "NEVER publish" comments. Add a copy
  item for `dualc_field[.exe]` next to the library (Exists-conditioned) so the tests find it.
- Keep D-09 (duplicate DLL item) as is — out of scope.

**Tests** (`tests/Boletus.Core.Tests/*Tests.cs`, the four `FindCli()` copies): look for
`dualc_field[.exe]` in `AppContext.BaseDirectory` first, then `DUALC_FIELD_EXE`, then the
historical `D:\DualC\…`. `FieldGraphTests.cs` samples dir: `DUALC_SAMPLES_DIR`, else walk up
from the test assembly to the folder holding `Boletus.sln` and use
`external/DualC/examples/samples`, else the `D:\` fallback. Merge the four copies into one
`TestPaths` helper while touching them.

**Gate** (`scripts/check.py`, `scripts/check_data.json`)
- New build-tier check `native-built`, before `dotnet-build`: the platform's library exists
  under `native/<rid>/`, else FAIL naming `python3 scripts/build_native.py`.
- `dualc_links.dualc_roots`: `["external/DualC", "D:/DualC", "../DualC"]` (submodule first; the
  anchor check then validates against the pinned docs, not the sibling's newer ones).
- `structure`: add rows for `external/DualC` (gitlink), `scripts/build_native.py`,
  `.gitmodules`, `.github/workflows/ci.yml`, `yak/manifest.yml`, `LICENSE`, `CITATION.cff`;
  drop the three binary rows. Add `external/` to the summarized/ignored prefixes so the
  submodule's own files are never scanned (`size`, `links`, `doc-lag`, `structure`).
- Docstring L2–18, `description=` L1523, the message at L1373: rewrite ("the gate is what CI
  runs"; the native rule is "never committed", not "never published").

## Step 2 — CI and the Yak package

**`.github/workflows/ci.yml`** — triggers: push (all branches), pull_request, workflow_dispatch;
concurrency per ref. `actions/checkout` with `submodules: true`.
- Job `linux` (ubuntu-24.04): apt `cmake ninja-build g++`; setup-dotnet 9;
  `actions/cache` on `build-native/` keyed `native-${{ runner.os }}-${{ submodule sha }}`
  (the DualC tree changes only on a re-pin, so caching objects is the right trade here);
  `python3 scripts/build_native.py`; `DUALC_ROOT=external/DualC DUALC_SAMPLES_DIR=external/DualC/examples/samples
  python3 scripts/check.py` (full gate = docs + build + test, `--docs --strict` too).
- Job `windows` (windows-2022): same shape with VS 2022, builds the viewer too; after the gate,
  `dotnet build Boletus.sln -c Release`; download McNeel's standalone `yak.exe`
  (`https://files.mcneel.com/yak/tools/latest/yak.exe` — verify the URL at implementation);
  stage `yak/manifest.yml` + `Boletus.Grasshopper.gha`, `Boletus.Core.dll`, `dualc_capi.dll`,
  `dualc_field_view.exe` into one folder; `yak build`; `actions/upload-artifact` the `.yak`.
  Also upload the staged folder as a second artifact (the plain drop-in for
  `%APPDATA%\Grasshopper\Libraries`).
- No `yak push`, no release job, no secrets.

**`yak/manifest.yml`** (new): name `boletus`, version from `BoletusInfo.Version` (`0.1.0`),
authors Giacomo Forcina, description, url = the GitHub repo, keywords. Version sync rule: the
manifest version equals `BoletusInfo.Version` — add a fast-tier check `yak-version` for it.

## Step 3 — license, citation, docs rectified

**New root files**: `LICENSE` (DualC's MIT text verbatim), `CITATION.cff` (DualC's shape:
title Boletus, abstract one sentence, version 0.1.0, license MIT,
repository-code `https://github.com/Giacogiak/Boletus`).

**Decisions** (`docs/decisions/README.md` + `01-settled.md`; next free ID **D-46**):
- D-46 (DONE, 2026-10-06): published on GitHub with a fresh root; D-02's local-only rule is
  reversed — the standing rule becomes: no push to `main` without the gate green, no Yak-server
  or NuGet publish without the owner's say-so, **native binaries are never committed**
  (built from the submodule); D-02 stays, as settled rows do.
- D-47 (DONE, 2026-10-06): DualC consumed as a git submodule at `external/DualC`, the gitlink
  is the pin; reverses D-01 (vendored DLL).
- D-04 (local NuGet feed) → DROPPED: the submodule + CI make a feed moot (its CI trigger
  fired and was answered differently). D-10 (`update-native.ps1`) → DONE as `build_native.py`.

**New roadmap block `docs/roadmap/10-public-delivery.md`** (next free block **10**; the DualC
block-20 shape; items next free **#32**):
- #32 Publish with a fresh root (the archive, the guide, the root hash — filled after step 6).
- #33 DualC as a submodule; binaries built, not vendored (what landed, rejected: keep vendoring,
  `add_subdirectory` from MSBuild, a DualC release artifact — DualC's own CI plan names it out
  of scope; the `.yak` vs embedded-gha trade-off, embedding deferred → D-48 DEFERRED, trigger:
  a user who needs a bare single `.gha`).
- #34 CI: the gate as a GitHub Actions workflow + the `.yak` artifact (verification: the first
  green run's numbers, filled in step 8).
- Footer `← Back to the [Roadmap index](README.md).`; 09's README footer gains `next: 10`.

**Roadmap index `docs/roadmap/README.md`**: block 10 row (DONE after step 8); § Current focus
one paragraph; § Recent milestones +2 rows (drop the oldest beyond five); § Next up: item 1
("Windows rebuild at the pin") retired — CI builds it; new item 1 = the Rhino smoke tests on
the CI `.yak`; item 3 Distribution updated (the `.yak` exists, zero-prerequisite still waits on
D-05); item 5 drop D-04. § How the blocks relate: 02 "submodule, pinned by the gitlink".
"Standing constraints — local only" line → "published; native never committed".

**Dated appends (never rewrite)**: roadmap 02 § Recommendation (option D: submodule, chosen
2026-10-06, link to 10 #33); roadmap 06 § Standing constraint (italic dated note: reversed by
D-46, the rule as it stands now, link to 10); roadmap 00 (git row + DualC path note);
roadmap 07/02 L15–25 (the viewer is built, not vendored); roadmap 03 L75–78 and 05/02 L19–21
one-line dated notes pointing at D-46.

**Rewritten pages** (present tense, no dates under `design/`):
- `README.md`: L4 (DualC = public repo + submodule), L8–9 banner → "Public; CI builds DualC
  from the submodule; the `.yak` is the artifact", L46, L53, L58–59; a § Build that names
  `git clone --recurse-submodules` + `build_native.py` + the dotnet commands; § License.
- `AGENTS.md` (≤ 80 lines, no ISO dates): L3–5, L9–13, L23 ("CI runs the gate; the remote is
  https://github.com/Giacogiak/Boletus, a fresh root — why: docs/roadmap/10-public-delivery.md"),
  L35, L75; the hard rule becomes "native binaries are never committed; no Yak-server/NuGet
  publish without the owner's say-so".
- `STRUCTURE.md` L4, L15, L104–110, L117, L141–144 + the new rows.
- `docs/README.md` L22; `docs/design/README.md` L9, L29, L43–44, L100–103;
  `docs/design/01-native-interop.md` § Where the DLL is found (L160–170);
  `docs/design/04-grasshopper-plugin.md` L25, L172–175;
  `docs/design/06-conventions.md` § Local only (body only), § Provenance by commit, L122;
  `docs/design/08-glossary.md` rows Vendored → "Built native", The pin → the gitlink, the
  `#21`–`#31` vocabulary line → `#34`; `examples/README.md` L12.
- `docs/raw/2026-10-06-public-delivery-plan.md`: this plan as drafted + an index row.

**Memory** (outside the repo, after the session): update
`~/.claude/projects/-home-giak-Documents-Boletus/memory/local-only-no-publish.md` to the new rule.

## Step 5 — archive the old history (mirrors DualC exactly)

```bash
cd ~/Documents/Boletus && git status --short                              # empty
git bundle create ~/Documents/Boletus-history-2026-10-06.bundle main      # one ref, as DualC's
git bundle verify ~/Documents/Boletus-history-2026-10-06.bundle           # "is okay", complete
git bundle list-heads ~/Documents/Boletus-history-2026-10-06.bundle       # <old tip> refs/heads/main
cd ~/Documents && zip Boletus-history-2026-10-06.zip Boletus-history-2026-10-06.bundle
unzip -l Boletus-history-2026-10-06.zip && rm Boletus-history-2026-10-06.bundle
```
Record old tip, `git rev-list --count main`, zip path. Test once: clone the bundle `-b main`
into the scratchpad, count commits, delete the clone.

## Step 6 — fresh root commit

```bash
cd ~/Documents/Boletus
git checkout --orphan public-main            # same tree (gitlink + .gitmodules included), no parent
git commit -m "Initial public release of Boletus" -m "Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
git branch -M public-main main
git reflog expire --expire=now --all && git gc --prune=now --aggressive
git rev-list --all --max-parents=0           # exactly one hash
git fsck --no-reflogs && git count-objects -v
```
Optional (DualC did not): the untracked `scripts/hooks/pre-push` guard from the guide's B4,
listed in `.git/info/exclude`. Install it — it costs nothing and the guide already documents it.

## Step 7 — remote and push (only after the owner confirms the empty repo exists)

```bash
git remote add origin git@github.com:Giacogiak/Boletus.git
git push -u origin main && git ls-remote origin          # only refs/heads/main
```

## Step 8 — "Boletus is published" commit

Fill 10 #32 (root hash, archive name, count), AGENTS.md's remote line, the CI run numbers once
green (#34); gate; commit; push. If CI is red, fix forward in further commits.

## The guide — `~/Documents/git-history-restore-guide.md`, one file for both repos

Keep every method and warning verbatim; add a per-repo value table at the top:

| Repo | Published | Public root | Archive zip | Old tip | Old commits |
| DualC | 2026-10-03 | 5989fb5… | DualC-history-2026-10-03.zip | c119ac3… | 191 |
| Boletus | 2026-10-06 | (step 6) | Boletus-history-2026-10-06.zip | (step 5) | (step 5) |

Parametrise commands with `<REPO>`/`<DATE>` keeping the DualC lines as the worked example;
add one Boletus note: the archive holds the gitlink only, a clone of it needs
`git submodule update --init` (fetches DualC from GitHub); the old history still has the
binaries committed — never push a restored tree.

## Verification

- `python3 scripts/check.py` (full) and `--docs --strict` green on Linux, `--selftest` green.
- `python3 scripts/build_native.py` from the submodule produces `native/linux-x64/libdualc_capi.so`
  + `dualc_field`; `dotnet test` passes including CLI parity (now exercised, not skipped).
- A fresh `git clone --recurse-submodules` of the archive-free repo into the scratchpad:
  `build_native.py` + gate pass with no sibling DualC and no `D:\`.
- `git rev-list --all --max-parents=0` prints one hash; the bundle verifies; `git ls-remote`
  shows only `main`.
- GitHub Actions: `linux` green; `windows` green with a `boletus-0.1.0-*.yak` artifact
  attached. Windows-only runtime (Rhino) stays a manual smoke test — recorded as pending.
- Session end: `/repo-docs-lifecycle` harvest + gate; `/docs-semantic-lint` run (both `src/`
  and `docs/design/` change this session), recorded under 09/06.
