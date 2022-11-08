// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper.Configuration.Annotations;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Cli;
using Xunit;
using Xunit.Abstractions;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace Integration.Tests
{
    public sealed class E2ETest
    {
        private const string TempDirectoryName = "dotnet-upgrade-assistant-tests";
        private const string IntegrationTestAssetsPath = "IntegrationScenarios";
        private const string OriginalProjectSubDir = "Original";
        private const string UpgradedProjectSubDir = "Upgraded";

        private readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ".upgrade-assistant"
        };

        private readonly ITestOutputHelper _output;

        public E2ETest(ITestOutputHelper output)
        {
            _output = output;
        }

        [InlineData("PCL", "SamplePCL.csproj", "")]
        [InlineData("WpfSample/csharp", "BeanTrader.sln", "BeanTraderClient.csproj")]
/*
        [InlineData("WebLibrary/csharp", "WebLibrary.csproj", "")]
        [InlineData("AspNetSample/csharp", "TemplateMvc.csproj", "")]
*/
        [InlineData("WpfSample/vb", "WpfApp1.sln", "")]
        [InlineData("WCFSample", "ConsoleApp.csproj", "")]

        // TODO: [mgoertz] Re-enable after .NET 7 GA
        // [InlineData("MauiSample/droid", "EwDavidForms.sln", "EwDavidForms.Android.csproj")]
        // [InlineData("MauiSample/ios", "EwDavidForms.sln", "EwDavidForms.iOS.csproj")]
        [Theory]
        public async Task UpgradeTest(string scenarioPath, string inputFileName, string entrypoint)
        {
            // Create a temporary working directory
            var workingDir = Path.Combine(Path.GetTempPath(), TempDirectoryName, Guid.NewGuid().ToString());
            var dir = Directory.CreateDirectory(workingDir);
            Assert.True(dir.Exists);

            // Copy the scenario files to the temporary directory
            var scenarioDir = Path.Combine(IntegrationTestAssetsPath, scenarioPath);
            var temp = await FileHelpers.CopyDirectoryAsync(Path.Combine(scenarioDir, OriginalProjectSubDir), workingDir).ConfigureAwait(false);
            var upgradeRunner = new UpgradeRunner();

            // Run upgrade
            var result = await upgradeRunner.UpgradeAsync(Path.Combine(workingDir, inputFileName), entrypoint, _output, TimeSpan.FromMinutes(15)).ConfigureAwait(false);

            Assert.Equal(ErrorCodes.Success, result);

            AssertOnlyKnownPackagesWereReferenced(upgradeRunner.UnknownPackages, workingDir);
            AssertDirectoriesEqual(Path.Combine(scenarioDir, UpgradedProjectSubDir), workingDir);

            FileHelpers.CleanupBuildArtifacts(workingDir);
            temp.Dispose();
        }

        private static void AssertOnlyKnownPackagesWereReferenced(UnknownPackages unknownPackages, string actualDirectory)
        {
            if (!unknownPackages.Keys.Any())
            {
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            var uknownPackageStr = JsonSerializer.Serialize(unknownPackages, options);
            var outputFile = Path.Combine(actualDirectory, "UnknownPackages.json");

            File.WriteAllText(outputFile, uknownPackageStr);

            Assert.False(true, $"Integration tests tried to access NuGet.{Environment.NewLine}The list of packages not yet \"pinned\" has been written to:{Environment.NewLine}{outputFile}");
        }

        private static bool IsBuildArtifact(PathString path)
            => path.Contains("obj") || path.Contains("bin");

        private void AssertDirectoriesEqual(string expectedDir, string actualDir)
        {
            var expectedFiles = Directory.GetFiles(expectedDir, "*", SearchOption.AllDirectories).Select(p => p[(expectedDir.Length + 1)..])
                .OrderBy(fileName => fileName)
                .ToArray();
            var actualFiles = Directory.GetFiles(actualDir, "*", SearchOption.AllDirectories).Select(p => p[(actualDir.Length + 1)..])
                .Where(t => !_ignoredFiles.Contains(Path.GetFileName(t)))
                .Where(t => !IsBuildArtifact(t))
                .OrderBy(fileName => fileName)
                .ToArray();

            var maxLength = expectedFiles.Length > actualFiles.Length ? expectedFiles.Length : actualFiles.Length;
            for (var i = 0; i < maxLength; i++)
            {
                if (i == expectedFiles.Length)
                {
                    Assert.True(false, $"Was not expecting to find file '{actualFiles[i]}'");
                }
                else if (i == actualFiles.Length)
                {
                    Assert.True(false, $"Could not find expected file '{expectedFiles[i]}'");
                }
                else if (expectedFiles[i] != actualFiles[i])
                {
                    Assert.True(false, $"Was expecting to see the file '{expectedFiles[i]}' but found '{actualFiles[i]}' instead.");
                }
            }

            foreach (var file in expectedFiles)
            {
                var expectedText = ReadFile(expectedDir, file);
                var actualText = ReadFile(actualDir, file);

                if (file.StartsWith("UpgradeReport."))
                {
                    actualText = actualText.Replace(actualDir.Replace("\\", "\\\\"), "[ACTUAL_PROJECT_ROOT]")
                        .Replace(actualDir.Replace("\\", "/"), "[ACTUAL_PROJECT_ROOT]")
                        .Replace(Directory.GetCurrentDirectory().Replace("\\", "/"), "[UA_PROJECT_BIN]");

                    actualText = Regex.Replace(actualText, "Version=(\\d+\\.){3}([\\da-zA-Z\\-])+", "[VERSION]");
                }

                if (!string.Equals(expectedText, actualText, StringComparison.Ordinal))
                {
                    var message = $"The contents of \"{file}\" do not match.";
                    if (file.StartsWith("UpgradeReport."))
                    {
                        var fileToCompare = Path.Combine(actualDir, "UpgradeReport.relative.txt");
                        File.WriteAllText(fileToCompare, actualText);
                        var diff = FindFileDiff(Path.Combine(Directory.GetCurrentDirectory(), expectedDir, file!), fileToCompare);
                        message += $"\nFile Diff:\n{diff}";
                    }

                    _output.WriteLine(message);
                    _output.WriteLine(string.Empty);
                    _output.WriteLine("Expected contents:");
                    _output.WriteLine(expectedText);
                    _output.WriteLine(string.Empty);
                    _output.WriteLine("Actual contents:");
                    _output.WriteLine(actualText);

                    Assert.True(false, message);
                }
            }

            static string ReadFile(string directory, string file)
                => File.ReadAllText(Path.Combine(directory, file));
        }

        private string FindFileDiff(string file1, string file2)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = $"/C fc {file1} {file2} /c",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
    }
}
