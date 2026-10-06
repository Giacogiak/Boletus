# Boletus

**Boletus** is the .NET / Rhino 8 / Grasshopper front-end for the **DualC** implicit-field
engine ([Giacogiak/DualC](https://github.com/Giacogiak/DualC), consumed here as the git
submodule `external/DualC`). It lets engineers compose, preview, and export TPMS-lattice and
implicit-field parts **parametrically on the Grasshopper canvas** — driving DualC's
field-graph engine through its native C ABI.

> **Status.** Public, MIT-licensed, built and tested by CI on every push; the Windows job
> packages the plugin as one `.yak`. Native binaries are never committed — CI and the
> developer build them from the submodule — and nothing is pushed to the Yak server or to a
> NuGet feed without the owner's say-so. Where the project stands:
> [`docs/roadmap/README.md`](docs/roadmap/README.md) § Current focus.

## Why Boletus

DualC already delivers the engineer's value from the command line — compose a field →
contour/preview → export STL/3MF — and proved it (a boolean over a lattice *mesh* costs
tens of minutes; the same composition as a *field* is one short contour). Boletus wraps that
value for Grasshopper so a graph of components produces a printable lattice part, with a
capped proxy mesh in the Rhino viewport, a live GPU preview beside it, and exact STL/3MF
export — no mesh round-trips, the field-graph as the single source of truth.

**The pivotal property:** DualC's C ABI exposes **no per-primitive factories** — *the
field-graph string is the construction API*. A host composes a shape by sending one
JSON / `--expr` string and gets back a proxy mesh and/or a file export. So the
native-interop layer is thin, and the real work is the managed field-graph builder that
Grasshopper components feed.

## Architecture

```
Boletus.Grasshopper (.gha, net7.0-windows)     ← the GH palette + Rhino glue
        │ project reference
Boletus.Core (netstandard2.0, no Rhino dep)    ← P/Invoke wrapper + field-graph model + canonical-JSON
   = DualcField / FieldGraph/ / Volume            serializer + the Rhino-free Volume datatype
        │ P/Invoke (Cdecl, x64)
dualc_capi.dll / libdualc_capi.so              ← DualC's C ABI, built from external/DualC into native/<rid>/
        │ statically links
libdualc + field-graph parser + STL/3MF/OBJ writers
```

`Boletus.Core` is intentionally **Rhino-free** so it is unit-testable on a plain runner and
reusable by any non-Rhino consumer. How the layers work today is [`docs/design/`](docs/design/README.md); how they
got that way is [`docs/roadmap/`](docs/roadmap/README.md); the file-by-file map is
[`STRUCTURE.md`](STRUCTURE.md).

## Build & test

Windows or Linux x64. Needs the **.NET SDK**, **CMake** and a C++17 toolchain (Visual Studio
2022 on Windows; `g++` + Ninja on Linux) for the native side; the first configure fetches
geometry-central from the network. Rhino is needed only to *run* the plugin.

```sh
git clone --recurse-submodules https://github.com/Giacogiak/Boletus.git
cd Boletus
python scripts/build_native.py    # DualC's C ABI (+ dualc_field, and the GPU viewer on Windows) → native/<rid>/
dotnet build Boletus.sln -c Debug
dotnet test  tests/Boletus.Core.Tests/Boletus.Core.Tests.csproj -c Debug
python scripts/check.py           # the gate: docs contract + native present + build + test — what CI runs
```

- **The native side** is built, never committed: `scripts/build_native.py` configures and
  builds the submodule (or a checkout named by `--dualc` / `DUALC_ROOT`) and drops the
  library, DualC's `dualc_field` CLI and, on Windows, `dualc_field_view.exe` into
  `native/<rid>/`, which is gitignored. The pin is the submodule's commit:
  [`native/README.md`](native/README.md).
- **NuGet:** `Boletus.Core` and its tests restore from the cache; the `Boletus.Grasshopper`
  build restores the `Grasshopper` metapackage and the reference packs online. On Linux
  the solution takes `-p:EnableWindowsTargeting=true` (the gate passes it); the `.gha`
  compiles there but only Rhino on Windows loads it.
- **Verification:** the tests assert DualC's **golden contour counts** (a single exact
  equality is a full marshaling gate) and byte-identical export parity with `dualc_field`
  when it sits beside the test assembly (it does after `build_native.py`). The counts and
  the contour-depth rule live in [`docs/design/`](docs/design/07-invariants-and-limits.md).
- **CI** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)): an Ubuntu job and a
  Windows job each build the native side from the submodule and run the gate; the Windows
  job also stages `Boletus.Grasshopper.gha`, `Boletus.Core.dll`, `dualc_capi.dll` and
  `dualc_field_view.exe` with [`yak/manifest.yml`](yak/manifest.yml) and uploads the
  resulting `.yak` (and the bare folder) as run artifacts — install one with Rhino's
  Package Manager or drop the folder into `%APPDATA%\Grasshopper\Libraries`.

## Using `Boletus.Core`

```csharp
using Boletus.Core;

Console.WriteLine(DualcField.Version());

// The field-graph string IS the construction API (JSON or --expr shorthand).
using var field = DualcField.FromExpr(
    "intersection(onion(gyroid(wavelength=0.5),thickness=0.12)," +
    "box(min=[-1,-1,-1],max=[1,1,1]))");

var p = DualcContourParams.Default();
p.MaxDepth = 6;                                 // coarse = cheap drawable PROXY
DualcMeshData proxy = field.Contour(p);         // -> positions / normals / indices

p.MaxDepth = 8;                                 // full = export-grade
field.Export(@"C:\tmp\part.stl", p);            // .obj / .stl / .3mf by extension
```

Non-OK native statuses surface as `DualcException` (with the `DualcStatus` code and, for
graph errors, a locator). Mesh sources reach DualC **in RAM (no disk)** — to clip a Rhino
mesh, marshal it to a `MeshBuffer`, reference it as `mesh(id="…")`, and build via
`DualcField.FromJson(json, meshes)`; `mesh(path="…")` from disk also still works.

## Using the Grasshopper components

Once the `.gha` is loaded in Rhino 8, the components appear on the **Boletus** tab. The
reference — every component's inputs and outputs with their real defaults, every dropdown's
option set, the exact messages, the recipes, the worked examples and the troubleshooting
table — is [`docs/command_reference/`](docs/command_reference/README.md). A demo canvas is in
[`examples/`](examples/README.md).

## The DualC dependency

Boletus consumes DualC two ways, by the most maintainable mechanism for each:

- **Managed `Boletus.Core` ↔ `.gha`:** project reference (one solution, build-from-source).
- **Native `dualc_capi.dll` + `dualc_field_view.exe`:** built from the **git submodule**
  `external/DualC`, pinned by its commit, by `scripts/build_native.py` — on the developer's
  machine and in CI alike. Nothing prebuilt is committed.

Rationale and the NuGet / vendored-DLL / submodule / project-reference trade-off:
[`docs/roadmap/02-dependency-strategy.md`](docs/roadmap/02-dependency-strategy.md) and
[`docs/roadmap/10-public-delivery.md`](docs/roadmap/10-public-delivery.md); the build
procedure: [`native/README.md`](native/README.md). The DualC repo's authoritative files (the
ABI header, the field-graph vocabulary, its design layer, fixtures) are catalogued in
[`docs/roadmap/00-references-and-environment.md`](docs/roadmap/00-references-and-environment.md).

## Documentation

Start at [`docs/README.md`](docs/README.md): it says which folder owns which class of fact —
design, usage contract, development record, decisions, immutable inputs — and how to search
them. `STRUCTURE.md` is the file-by-file map of the codebase; `AGENTS.md` is the entry file
for coding agents.

## License

MIT — see [`LICENSE`](LICENSE). To cite Boletus, see [`CITATION.cff`](CITATION.cff). DualC
is MIT as well; its third-party notices are in its own repository.
