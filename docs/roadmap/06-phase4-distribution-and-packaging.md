# 06 — Phase 4: Distribution & packaging

> Detail for the [roadmap](README.md). **Status: PLANNED** (and **not active now**). How
> Boletus eventually reaches Rhino users — and the hard local-only constraint that governs
> it until then. *(2026-10-06: the `.yak` exists as a CI artifact and the local-only
> constraint is reversed — see the dated note under § Standing constraint; what remains
> PLANNED here is the zero-prerequisite install and any Yak-server release.)*

## Standing constraint — LOCAL ONLY, no publishing

**Everything stays local for now. Nothing is published without explicit authorization.**
Concretely, until told otherwise:

- **No `yak push`** (the publish-to-Rhino-server step).
- **No NuGet publish** to any feed (NuGet is used only to *restore* test packages from the
  local cache).
- **No `git push` / remote / PR.** (Boletus is not even a git repo yet.) *(2026-09-21, roadmap 09
  Phase 4: it is a local git repository on `main` with no remote; the rule stands unchanged —
  [design 06 § Local only](../design/06-conventions.md#local-only).)*

Building a `.yak` *locally* and installing it locally are allowed; **publishing is the one
thing that is off the table** by default.

*(2026-10-06, roadmap 10: the constraint is reversed by [D-46](../decisions/01-settled.md).
Boletus is public at `https://github.com/Giacogiak/Boletus` with a fresh root commit, and CI's
Windows job builds the `.yak` as a run artifact ([10 #34](10-public-delivery.md#34-ci--the-gate-as-a-github-actions-workflow-and-one-yak)).
What stays the owner's say-so is the `yak push` and a NuGet publish; the hard rule under it
is that no native binary is ever committed — the present-tense statement is
[design 06 § Local only](../design/06-conventions.md#local-only). The heading keeps its
name because it is an anchor.)*

## Distribution layer ≠ dependency layer

This is the confusion to keep clear: **Yak is not one of the three build-time options**
(NuGet / vendored DLL / project reference — see [02](02-dependency-strategy.md)). Those
wire the *build*; Yak packages the *finished artifact* for an end user. They coexist: we
build with **project reference + vendored DLL**, then (eventually) package the output as a
**local Yak**.

## Target format — Yak (when Phase 3 produces a `.gha`)

[Yak](https://developer.rhino3d.com/guides/yak/) is Rhino 8's package manager. A `.yak`
installs in one click via `_PackageManager` and lays files out correctly — the most
convenient install for users. It has two **separate** steps:

- `yak build` → produces a `.yak` file **locally**. ✅ allowed.
- `yak push` → uploads it to Rhino's public package server. ⛔ **publishing — not without
  explicit OK.** A local folder can also be added as a Package Manager *source* for fully
  local install/testing, no server involved.

### Package contents

The `.yak` bundles, in the Grasshopper layout:
- `Boletus.Grasshopper.gha`
- `Boletus.Core.dll`
- `dualc_capi.dll` (the vendored native bridge)
- a `manifest.yml` (id, version, authors, Rhino/platform = win-x64)

### For development right now

No packaging is needed at all: build the `.gha` and drop it (+ `Boletus.Core.dll` +
`dualc_capi.dll`) into `%APPDATA%\Grasshopper\Libraries`, then right-click-unblock the
DLLs. That is the local dev loop until a Yak is worth producing.

## Zero-prerequisite install — the VC++ runtime

`dualc_capi.dll` is self-contained except for the **VC++ runtime** (`MSVCP140`,
`VCRUNTIME140*`) + the Windows UCRT (present on Win10+). To avoid making users install the
VC++ Redistributable, the chosen approach is to **statically link the MSVC runtime (`/MT`)
into `dualc_capi.dll`** — a small **DualC-side build change** ([07 § 2](07-upstream-coordination/README.md#2-static-linked-msvc-runtime-mt--zero-prerequisite-install)).
Then the only dependency is the UCRT and the install is truly zero-prerequisite. Until that
lands, the plugin still runs on machines that have the VC++ runtime (e.g. this dev box);
bundling the runtime DLLs app-local is the fallback if the static-link change is delayed.

## Verification (when packaged)

- The local `.yak` installs via Package Manager (local source) and the components appear.
- On a **clean** Win10+ VM with no VC++ redist, the plugin loads (proves the static-runtime
  build) — deferred until the static-link change lands.

---

← Back to the [roadmap](README.md) · prev: [05 — Grasshopper components](05-phase3-grasshopper-components/README.md) · next: [07 — Upstream coordination](07-upstream-coordination/README.md)
