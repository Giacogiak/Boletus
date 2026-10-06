using System;
using System.IO;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Where the CLI-backed tests find DualC's <c>dualc_field</c> and its shipped sample
    /// graphs. One home for the lookup the four CLI-oracle suites share. The tests that
    /// need the CLI skip (return early) when it is absent, so the suite stays runnable on
    /// a checkout that has not built the native side yet.
    /// </summary>
    internal static class TestPaths
    {
        /// <summary>
        /// The <c>dualc_field</c> CLI: beside the test assembly first (scripts/build_native.py
        /// drops it into native/&lt;rid&gt;/ and Boletus.Core.csproj copies it to every
        /// output), then <c>DUALC_FIELD_EXE</c>, then the historical Windows sibling-checkout
        /// path. Null when none exists.
        /// </summary>
        public static string? FindCli()
        {
            string local = Path.Combine(AppContext.BaseDirectory,
                OperatingSystem.IsWindows() ? "dualc_field.exe" : "dualc_field");
            if (File.Exists(local)) return local;
            string? env = Environment.GetEnvironmentVariable("DUALC_FIELD_EXE");
            if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;
            const string def = @"D:\DualC\build\examples\Release\dualc_field.exe";
            return File.Exists(def) ? def : null;
        }

        /// <summary>
        /// DualC's shipped fixtures (<c>examples/samples</c>): <c>DUALC_SAMPLES_DIR</c> first,
        /// then the submodule <c>external/DualC</c> found by walking up from the test assembly
        /// to the folder holding <c>Boletus.sln</c>, then the historical Windows path.
        /// </summary>
        public static string SamplesDir { get; } = ResolveSamplesDir();

        private static string ResolveSamplesDir()
        {
            string? env = Environment.GetEnvironmentVariable("DUALC_SAMPLES_DIR");
            if (!string.IsNullOrEmpty(env)) return env;
            for (DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            {
                if (!File.Exists(Path.Combine(d.FullName, "Boletus.sln"))) continue;
                string sub = Path.Combine(d.FullName, "external", "DualC", "examples", "samples");
                if (Directory.Exists(sub)) return sub;
                break;
            }
            return @"D:\DualC\examples\samples";
        }
    }
}
