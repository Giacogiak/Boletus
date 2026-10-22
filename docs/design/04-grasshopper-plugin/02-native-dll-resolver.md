# The native-DLL resolver

`BoletusPriority.cs` is a `GH_AssemblyPriority`: `PriorityLoad()` runs once at Grasshopper
startup, before any component solves, and installs
`NativeLibrary.SetDllImportResolver(typeof(DualcField).Assembly, …)`. Two facts make it
necessary and shape it. The Windows loader searches `Rhino.exe`'s directory and the system
path when a `[DllImport("dualc_capi")]` fires — **not** the `.gha`'s folder — so the DLL
copied beside the `.gha` is not reliably found without help. And a resolver fires only for
P/Invokes declared in the assembly it is attached to, so it is registered against the **Core**
assembly, where `NativeMethods` lives, not against the `.gha`. The resolver intercepts
`dualc_capi` / `dualc_capi.dll` only, loads it from the `.gha`'s own directory with
`NativeLibrary.TryLoad`, and falls through (`IntPtr.Zero`) for any other library name.
`Boletus.Core` cannot host this itself: netstandard2.0 has no `NativeLibrary`.

---

← Back to [The Grasshopper plugin](README.md) · the [design index](../README.md)
