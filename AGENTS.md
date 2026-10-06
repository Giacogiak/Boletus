# Boletus — agent entry

Boletus is the .NET / Rhino / Grasshopper front-end for **DualC**, the C++ implicit-field
engine (public, `https://github.com/Giacogiak/DualC`, here the git submodule `external/DualC`):
Grasshopper components compose a field-graph, and the one JSON string it serializes to is the
construction API of DualC's native C ABI (`dualc_capi`, built from the submodule).

## Build, test, gate

Windows or Linux x64, the .NET SDK, CMake and a C++17 toolchain. The native binaries are
**never committed**: `scripts/build_native.py` builds DualC's C ABI (plus `dualc_field` and,
on Windows, the GPU viewer) from the submodule into the gitignored `native/<rid>/`, and CI
does the same. The Grasshopper project restores its reference packages from nuget.org on the
first build. Nothing is pushed to the Yak server or a NuGet feed without the owner's say-so.
On Linux the dotnet commands take `-p:EnableWindowsTargeting=true` (the gate passes it); no
Rhino runs there, so the `.gha` only compiles.

```bash
git submodule update --init              # once per clone
python scripts/build_native.py           # the native side (DUALC_ROOT / --dualc for a sibling checkout)
dotnet build Boletus.sln -c Debug
dotnet test tests/Boletus.Core.Tests/Boletus.Core.Tests.csproj -c Debug
```

The remote is `https://github.com/Giacogiak/Boletus`, its history a fresh root commit (why:
`docs/roadmap/10-public-delivery/README.md`); CI (`.github/workflows/ci.yml`) runs the gate on Ubuntu
and Windows and packages one `.yak`. **The gate is the one command CI and you both run**; run
it before every push (the pre-commit hook runs the fast tier once enabled):

```bash
python scripts/check.py                  # docs contract + native present + dotnet build + dotnet test
python scripts/check.py --fast           # docs + repo hygiene only, under a second
python scripts/check.py --docs --strict  # session end: the report-only lists must be empty
python scripts/check.py --selftest       # the gate's own fixtures
git config core.hooksPath scripts/hooks  # once per clone
```

A `Stop` hook in `.claude/settings.json` runs the docs tier after every turn and hands a
failing report back to the agent. `dualc-links` reads the submodule (else `../DualC`;
`DUALC_ROOT` overrides). Where only `python3` exists (Linux), call it; both hooks pick whichever is installed.

## Five principles

1. **Nothing stays in the chat.** Every fact a session produces — a fix, a measurement, a
   decision, a rejected approach — is written into the repo before the session closes: route
   each one to its owner by the table in `docs/README.md`, then run the gate. The procedure
   is the owner's skill `/repo-docs-lifecycle` (in `~/.claude/skills/`, outside the repo), run
   at both ends of every session — start: gate, reading order, plan; end: gate, harvest,
   route, gate. A session that edits both `src/` and `docs/design/` also runs the owner's
   `/docs-semantic-lint` (`~/.claude/commands/`) — the meaning check of `docs/design/` against
   the code, its runs recorded under the docs-layers roadmap block; it runs at least monthly
   regardless.
2. **Every level has a README index.** Each folder's `README.md` names every file below it;
   an agent finds a fact by walking indexes, not by searching the tree.
3. **Numbers are IDs, never positions.** Roadmap blocks, tracked items (`#N`), reference pages
   and recipes keep the number they were born with; nothing is renumbered, a retired number
   is never reused, and a heading is never rewritten (anchors are links).
4. **The component reference grows with the code.** A component, input or output created,
   changed or removed gets its `docs/command_reference/` page updated in the same session —
   the inputs table with the real defaults, every dropdown's full option set, the exact
   remark and warning strings — and a DualC fact (an op token, an ABI signature, an engine
   invariant) is linked to DualC's own page, never restated.
5. **No page outgrows the cap.** A page that trips the size contract is split into a
   same-numbered folder with its own README, never frozen, never appended past the cap;
   `docs/raw/` is the only exempt tree because its files are immutable inputs.

## Reading order

Read in this order, stopping when you have what the task needs:

1. `docs/README.md` — what lives where and which folder owns which class of fact.
2. `docs/design/README.md` — how the plugin is today: the layering, the `Volume` contract,
   the resolvers, the terminals, conventions, invariants.
3. `docs/decisions/README.md` — what was deferred (with its trigger) or dropped, and why.
4. `docs/roadmap/README.md` § Current focus and § Next up — the one status snapshot.
5. The roadmap block of the topic you touch — the record of why it is the way it is.
6. Its `docs/command_reference/` page — the usage contract you must keep true.
7. For an engine fact, DualC's own layers: `external/DualC/docs/README.md` says where.

`STRUCTURE.md` is the file-by-file map of the codebase; `native/README.md` the native build and the pin.
