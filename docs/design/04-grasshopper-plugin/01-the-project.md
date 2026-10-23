# The project

`src/Boletus.Grasshopper/Boletus.Grasshopper.csproj`: **`net7.0-windows`**, x64, output
extension `.gha`, `UseWindowsForms` (the icons and the writer's ticker need it),
`EnableDynamicLoading`. Rhino 8's .NET Core host runs on the .NET 7 runtime, so a net8/net9
assembly does not load — the reason for the TFM. RhinoCommon and Grasshopper are referenced
through the **`Grasshopper` NuGet metapackage, compile-only** (`Private=false`, runtime assets
excluded): the real assemblies come from the running Rhino process and are never copied to
the output, so the shipped payload is exactly `Boletus.Grasshopper.gha`, `Boletus.Core.dll`,
`dualc_capi.dll`, `dualc_field_view.exe` and the `editor/` folder (`DualCEditor.exe`, its Qt
runtime, `LICENSES/`). The metapackage carries net48 reference assemblies, consumed by a
net7 project via `AssetTargetFallback`, which emits **NU1701** — expected for a Rhino 8
plugin build and suppressed in the csproj. The native binaries — built from the two
submodules into `native/x64/` by `scripts/build_native.py`, never committed — are copied
beside the `.gha` by `Exists`-conditioned `<None>` items mirroring the one in
`Boletus.Core.csproj`, the editor as a recursive folder item conditioned on its executable so
a machine without Qt still builds a `.gha` that falls back to the field view. CI's Windows
job packages those files with `yak/manifest.yml` into one `.yak`.

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
