# 02 — Dependency strategy: NuGet vs. vendored DLL vs. project reference

> Detail for the [roadmap](README.md). How Boletus structures its **build-time
> dependencies**, with a clear recommendation for the dependency on the **DualC** library
> project. This is a *separate layer* from end-user distribution (Yak) — see
> [06](06-phase4-distribution-and-packaging.md) for that, and the explicit contrast at the
> end of this doc.

## Two dependencies, two natures

Boletus has two distinct dependencies, and the right mechanism differs for each:

1. **Managed, ours:** `Boletus.Grasshopper` (.gha) → `Boletus.Core`. Co-developed C#
   in one solution.
2. **Native, from DualC:** `dualc_capi.dll` (+ the `dualc_c.h` contract). A prebuilt C++
   artifact produced by a separate repo with its own toolchain (CMake + geometry-central).

The "NuGet vs. vendored DLL vs. project reference" question applies to **both**, but the
answer is not the same for both. Below: the three options with pros/cons, then the
recommendation per dependency.

---

## The three options

### A. Project reference (`<ProjectReference>`)

The consumer references another **source project** in the same solution; MSBuild builds it
from source and wires the output automatically.

**Pros**
- Build-from-source: step-into debugging, refactor-across-projects, no version skew.
- Zero packaging overhead; one `dotnet build` builds everything.
- Always consistent — the consumer can never reference a stale binary.

**Cons**
- Requires the dependency's **source + its toolchain** to be buildable in the same build.
  Fine for C# projects; **a poor fit for a C++/CMake artifact** (would force a C++
  toolchain + geometry-central checkout into Boletus's build).
- All consumers must live in (or reference) the same solution tree.

### B. Vendored DLL (committed prebuilt binary)

A prebuilt binary is **committed into the repo** (here `native/x64/dualc_capi.dll`) and
referenced directly (P/Invoke by file name for native; an assembly reference for managed).

**Pros**
- **No toolchain dependency** — Boletus builds from a clean checkout with only the .NET
  SDK; no C++ build, no geometry-central, no network.
- **Fully local & reproducible** — exactly what the local-only constraint wants.
- Dead simple; the binary is right there, version frozen with the commit.

**Cons**
- **Manual updates** — refreshing the binary is a deliberate step; drift from upstream is
  possible if not tracked.
- A **binary in git** (acceptable for a ~1 MB DLL; not ideal at scale).
- **Provenance must be recorded** (which DualC commit/version produced it) or the binary
  becomes a mystery artifact.

### C. NuGet package (`<PackageReference>`)

The dependency is packaged as a `.nupkg` (managed assembly, or a native "runtimes/win-x64"
package) and consumed via a feed.

**Pros**
- **Versioned & restore-based** — explicit `1.2.3` pins, clean upgrades, transitive
  dependency resolution.
- Scales to **multiple consumer repos** sharing the same artifact.
- A **local folder feed** works fully offline — NuGet does **not** imply publishing to
  nuget.org.

**Cons**
- **Most infrastructure**: someone must produce the package (a `dualc_capi` native nupkg
  from DualC's build), host a feed (even a local folder), and manage versions.
- Indirection: the bits live in the NuGet cache, not visibly in the repo.
- Overkill while there is a **single consumer** (Boletus) and a single dev machine.

---

## Recommendation

### Managed `Boletus.Core` ↔ `Boletus.Grasshopper`: **project reference** ✅ (in use)

They are co-developed in one solution. Project references give build-from-source,
cross-project debugging, and no version skew — the obvious best practice for our own code.
Vendoring our own `Core.dll` would lose source/debugging; a NuGet package would add
versioning ceremony with no second consumer to justify it.

### Native `dualc_capi.dll` (the DualC dependency): **vendored DLL now → local NuGet later** ✅ (vendored, in use)

A project reference to the C++ artifact is **rejected**: it would drag a C++ toolchain +
geometry-central into Boletus's build for no benefit. Between vendored and NuGet:

- **Now — vendored.** It is the most maintainable choice *for the current situation*:
  single consumer, single machine, **local-only**, no network. Boletus builds from a clean
  checkout with just the .NET SDK. (Validated: the vendored DLL flows automatically to a
  referencing project's output — the test runner loaded it via the project reference, so
  the future `.gha` will too.)
- **Later — local NuGet feed.** Migrate when a concrete trigger fires (below). A versioned
  `dualc_capi` native package from DualC's build, consumed from a **local folder feed**,
  gives reproducible, explicitly-pinned native bits without a DualC checkout — still 100%
  local, no public publishing.

This is "vendored now, NuGet-ready later": both share the same P/Invoke surface, so the
migration is a sourcing change, **not** a code change.

### Best-practice guardrails for the vendored native dependency

**DEFERRED** (guardrails 3, 4) and **DROPPED** (guardrail 2); guardrail 1 exists; guardrail 5 **DONE** (2026-10-06, as `scripts/build_native.py` — D-10 settled, [10 #33](10-public-delivery.md#33-dualc-as-a-git-submodule--the-binaries-built-never-committed)) — see the follow-up at the end of the section. *(2026-09-21, roadmap 09 Phase 3: status line added as the first body line so the `decisions-index` gate finds this entry; the rows are D-07 to D-10 in the [decisions index](../decisions/README.md), one per guardrail with its disposition and trigger.)*

To keep the vendored DLL maintainable rather than a mystery binary:

1. **Record provenance.** Keep `native/README.md` (or `VERSION.txt`) noting the DualC
   **commit/tag + version** that produced the current `dualc_capi.dll`, the build command,
   and the date. (Build cmd: `cmake -S . -B build -DDUALC_BUILD_C_ABI=ON
   -DDUALC_BUILD_EXAMPLES=ON && cmake --build build --config Release --target dualc_capi`.)
2. **Pin the contract.** Commit a copy of `dualc_c.h` next to the DLL for reference; treat
   it as the versioned interface the wrapper targets.
3. **Verify at runtime.** Check `DualcField.Version()` at plugin startup and assert it
   matches the expected DualC version; surface a clear error on mismatch (the ABI is
   stable but the *graph vocabulary* can grow).
4. **Centralize the copy.** A `Directory.Build.props` / a single `<None>` item defines the
   native-DLL copy-to-output once, so every consumer (tests, .gha) inherits it.
5. **Scripted refresh.** A `scripts/update-native.ps1` that rebuilds `dualc_capi.dll` in
   the DualC tree and copies it into `native/x64/` + updates the provenance file — so
   "update the native dep" is one reproducible command, not ad-hoc copying.

**Follow-up (2026-09-21, roadmap 09 Phase 2).** Of the five, only guardrail 1 exists
(`native/README.md`). Guardrails 2 (a committed `dualc_c.h` copy), 3 (a startup
`DualcField.Version()` assert — moot while the DLL's version string does not change between
the commits Boletus pins, the version trap of [07 § 4](07-upstream-coordination/README.md#4-version-pinning--provenance--maintainability-hygiene)), 4 (a
single copy item) and 5 (`scripts/update-native.ps1`) were promised here on 2026-06-17 and
never built; the refresh procedure in `native/README.md` is manual. Each gets a row in
`docs/decisions/README.md` (Phase 3) with its disposition and trigger.

### Triggers to migrate the native dep to a local NuGet feed

**DROPPED** (2026-10-06): the CI trigger fired and was answered by a git submodule built in CI, not a feed — [10 #33](10-public-delivery.md#33-dualc-as-a-git-submodule--the-binaries-built-never-committed), D-47; the row is D-04 in the [decisions index](../decisions/README.md). *(2026-09-21, roadmap 09 Phase 3: status line added as the first body line so the `decisions-index` gate finds this entry; it read DEFERRED — the four triggers below — until 2026-10-06.)*

- A **second consumer** of `dualc_capi.dll` appears (e.g. a separate viewer/CLI shell).
- DualC adopts a **release cadence / versioned artifacts** worth pinning explicitly.
- Boletus gains **CI** that must build without a DualC checkout and wants reproducible,
  hash-verified native bits.
- The committed binary's churn in git history becomes a nuisance.

Until one fires, vendored is the lower-overhead, fully-local choice.

*(2026-10-06, roadmap 10.)* **A fourth option, chosen: D — a git submodule.** With DualC
public since 2026-10-03 and Boletus going public, the native dependency became *source*:
`external/DualC` pinned by its gitlink, built by `scripts/build_native.py` into the
gitignored `native/<rid>/` on every machine and in CI. It keeps A's "build from source"
without A's rejected cost (the C++ toolchain stays outside MSBuild, in one script), B's
pin-by-commit without a committed binary, and makes C moot. The argument, what landed and
what was rejected are [10 #33](10-public-delivery.md#33-dualc-as-a-git-submodule--the-binaries-built-never-committed);
D-01 (vendored) is reversed by D-47 in the [settled decisions](../decisions/01-settled.md).

---

## This is *not* the same as distribution (Yak)

A frequent confusion: **Yak is not one of these three options.** The three above are the
**build-time** layer (how the solution wires itself together while compiling). **Yak** is
the **end-user distribution** layer (the package format a Rhino user installs). They are
orthogonal and coexist: we build with *project reference + vendored DLL*, and we will
eventually *package the built output as a local Yak*. See
[06 — Distribution & packaging](06-phase4-distribution-and-packaging.md). Note the
standing constraint: **everything local, no publishing** without explicit authorization.
*(2026-10-06: reversed by D-46 — the repository is public and CI builds the `.yak`;
[10](10-public-delivery.md).)*

---

← Back to the [roadmap](README.md) · prev: [01 — Architecture & contract](01-architecture-and-contract.md) · next: [03 — Phase 2 wrapper](03-phase2-core-wrapper.md)
