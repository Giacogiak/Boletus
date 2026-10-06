using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Boletus.Core;
using Grasshopper.Kernel;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Runs once at Grasshopper startup (before any component solves) to install a
    /// native-DLL resolver for <c>dualc_capi.dll</c>.
    ///
    /// <para>Why this is needed: when a P/Invoke hits <c>[DllImport("dualc_capi.dll")]</c>,
    /// the Windows loader searches Rhino.exe's directory and the system path — NOT the
    /// <c>.gha</c>'s own folder. The native DLL we copy beside the <c>.gha</c> is therefore
    /// not reliably found, so we intercept the load and resolve it explicitly.</para>
    ///
    /// <para>Why it targets the Core assembly: <see cref="NativeLibrary.SetDllImportResolver"/>
    /// only fires for P/Invokes declared in the assembly it is attached to. The
    /// <c>[DllImport]</c> declarations live in <c>Boletus.Core</c> (internal
    /// <c>NativeMethods</c>), not in this <c>.gha</c> — so we register against
    /// <c>typeof(DualcField).Assembly</c>.</para>
    /// </summary>
    public sealed class BoletusPriority : GH_AssemblyPriority
    {
        private static bool _registered;

        public override GH_LoadingInstruction PriorityLoad()
        {
            if (!_registered)
            {
                NativeLibrary.SetDllImportResolver(typeof(DualcField).Assembly, Resolve);
                _registered = true;
            }

            return GH_LoadingInstruction.Proceed;
        }

        // Signature fixed by the DllImportResolver delegate.
        private static IntPtr Resolve(string libraryName, Assembly assembly,
                                      DllImportSearchPath? searchPath)
        {
            // Only intercept our native lib; fall through (Zero) for anything else.
            if (!string.Equals(libraryName, "dualc_capi", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(libraryName, "dualc_capi.dll", StringComparison.OrdinalIgnoreCase))
            {
                return IntPtr.Zero;
            }

            // Directory of THIS .gha — where the csproj copied dualc_capi.dll.
            string? ghaDir = Path.GetDirectoryName(typeof(BoletusPriority).Assembly.Location);
            if (ghaDir is null)
            {
                return IntPtr.Zero;
            }

            string dllPath = Path.Combine(ghaDir, "dualc_capi.dll");
            return NativeLibrary.TryLoad(dllPath, out IntPtr handle) ? handle : IntPtr.Zero;
        }
    }
}
