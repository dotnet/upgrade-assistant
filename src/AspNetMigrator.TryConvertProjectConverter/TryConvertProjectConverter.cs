using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public class TryConvertProjectConverter : IProjectConverter
    {
        private const string TryConvertArgumentsFormat = "--no-backup --force-web-conversion -p {0}";
        private ILogger Logger { get; }
        private string TryConvertPath { get; } =
            Path.Combine(Path.GetDirectoryName(typeof(TryConvertProjectConverter).Assembly.Location), "tools", "try-convert.exe");

        public TryConvertProjectConverter(ILogger logger)
        {
            Logger = logger;
        }

        public async Task<bool> ConvertAsync(string projectFilePath)
        {
            if (!File.Exists(projectFilePath))
            {
                Logger.Fatal("Project file {ProjectPath} not found", projectFilePath);
                return false;
            }

            Logger.Information("Converting project file format with try-convert");
            using var tryConvertProcess = new Process
            {
                StartInfo = new ProcessStartInfo(TryConvertPath, string.Format(TryConvertArgumentsFormat, projectFilePath))
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            tryConvertProcess.OutputDataReceived += TryConvertOutputReceived;
            tryConvertProcess.ErrorDataReceived += TryConvertErrorReceived;
            tryConvertProcess.Start();
            tryConvertProcess.BeginOutputReadLine();
            tryConvertProcess.BeginErrorReadLine();
            await tryConvertProcess.WaitForExitAsync();
            
            if (tryConvertProcess.ExitCode != 0)
            {
                Logger.Fatal("Conversion with try-convert failed");
                return false;
            }
            else
            {
                Logger.Information("Project file format conversion successful");
                return true;
            }
        }

        private void TryConvertOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Logger.Information($"[try-convert] {e.Data}");
            }
        }

        private void TryConvertErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Logger.Error($"[try-convert] {e.Data}");
            }
        }
    }
}
