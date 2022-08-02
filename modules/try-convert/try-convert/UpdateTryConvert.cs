using System;
using System.Diagnostics;
namespace MSBuild.Conversion
{
    public static class UpdateTryConvert
    {
        public static void Update()
        {
            var global = IsGlobalTool() ? "--global" : string.Empty;
            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $"tool update {global} try-convert";
                process.Start();
            }
        }

        private static bool IsGlobalTool()
        {
            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = "tool list --global";
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (line?.Contains("try-convert") ?? false)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
