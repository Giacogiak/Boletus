using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Boletus.Core;
using Xunit;

namespace Boletus.Core.Tests
{
    /// <summary>
    /// Confirms the wrapper's export is byte-identical to the DualC CLI's, since both
    /// route through the same <c>writeField</c>. Skips (does not fail) when the CLI
    /// binary isn't present, so the suite stays runnable without a DualC checkout.
    /// </summary>
    public class CliParityTests
    {
        private const string AnalyticExpr =
            "intersection(onion(gyroid(wavelength=0.5),thickness=0.12)," +
            "box(min=[-1,-1,-1],max=[1,1,1]))";

        // The CLI beside the test assembly (built from the submodule), else DUALC_FIELD_EXE.
        private static string? FindCli() => TestPaths.FindCli();

        private static string Sha1(string path)
        {
            using var sha = SHA1.Create();
            using var fs = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(fs));
        }

        [Fact]
        public void Wrapper_export_is_byte_identical_to_the_cli()
        {
            string? cli = FindCli();
            if (cli is null) return; // CLI not present: skip the optional parity check.

            string wrapperStl = Path.Combine(Path.GetTempPath(), $"boletus_w_{Guid.NewGuid():N}.stl");
            string cliStl      = Path.Combine(Path.GetTempPath(), $"boletus_c_{Guid.NewGuid():N}.stl");
            try
            {
                var p = DualcContourParams.Default();
                p.MaxDepth = 6;
                using (var field = DualcField.FromExpr(AnalyticExpr))
                    field.Export(wrapperStl, p);

                var psi = new ProcessStartInfo(cli!)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                psi.ArgumentList.Add("--expr");
                psi.ArgumentList.Add(AnalyticExpr);
                psi.ArgumentList.Add("--depth");
                psi.ArgumentList.Add("6");
                psi.ArgumentList.Add("-o");
                psi.ArgumentList.Add(cliStl);

                using (var proc = Process.Start(psi)!)
                {
                    proc.WaitForExit();
                    Assert.True(proc.ExitCode == 0, $"CLI failed: {proc.StandardError.ReadToEnd()}");
                }

                Assert.Equal(new FileInfo(cliStl).Length, new FileInfo(wrapperStl).Length);
                Assert.Equal(Sha1(cliStl), Sha1(wrapperStl));
            }
            finally
            {
                if (File.Exists(wrapperStl)) File.Delete(wrapperStl);
                if (File.Exists(cliStl)) File.Delete(cliStl);
            }
        }
    }
}
