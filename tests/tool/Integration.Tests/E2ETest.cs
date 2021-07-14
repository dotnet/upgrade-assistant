// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Cli;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Integration.Tests
{
    public sealed class E2ETest : IDisposable
    {
        private const string IntegrationTestAssetsPath = "IntegrationScenarios";
        private const string OriginalProjectSubDir = "Original";
        private const string UpgradedProjectSubDir = "Upgraded";

        private readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ".upgrade-assistant"
        };

        private readonly ITestOutputHelper _output;
        private readonly List<TemporaryDirectory> _temporaryDirectories;

        public E2ETest(ITestOutputHelper output)
        {
            _output = output;
            _temporaryDirectories = new List<TemporaryDirectory>();
        }

        [InlineData("AspNetSample/csharp", "TemplateMvc.csproj", "")]
        [InlineData("PCL", "SamplePCL.csproj", "")]
        [InlineData("WebLibrary/csharp", "WebLibrary.csproj", "")]
        [InlineData("WpfSample/csharp", "BeanTrader.sln", "BeanTraderClient.csproj")]
        [InlineData("WpfSample/vb", "WpfApp1.sln", "")]
        [InlineData("MauiSample", "EwDavidForms.sln", "EwDavidForms.Android.csproj")]
        [InlineData("MauiSample", "EwDavidForms.sln", "EwDavidForms.iOS.csproj")]
        [Theory]
        public async Task UpgradeTest(string scenarioPath, string inputFileName, string entrypoint)
        {
            // Create a temporary working directory
            var workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dir = Directory.CreateDirectory(workingDir);
            Assert.True(dir.Exists);

            // Copy the scenario files to the temporary directory
            var scenarioDir = Path.Combine(IntegrationTestAssetsPath, scenarioPath);
            _temporaryDirectories.Add(await FileHelpers.CopyDirectoryAsync(Path.Combine(scenarioDir, OriginalProjectSubDir), workingDir).ConfigureAwait(false));
            var upgradeRunner = new UpgradeRunner();

            // Run upgrade
            var result = await upgradeRunner.UpgradeAsync(Path.Combine(workingDir, inputFileName), entrypoint, _output, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

            Assert.Equal(ErrorCodes.Success, result);

            FileHelpers.CleanupBuildArtifacts(workingDir);

            AssertOnlyKnownPackagesWereReferenced(upgradeRunner.UnknownPackages, workingDir);
            await AssertDirectoriesEqualAsync(Path.Combine(scenarioDir, UpgradedProjectSubDir), workingDir).ConfigureAwait(false);
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

        private async Task AssertDirectoriesEqualAsync(string expectedDir, string actualDir)
        {
            var expectedFiles = Directory.GetFiles(expectedDir, "*", SearchOption.AllDirectories).Select(p => p[(expectedDir.Length + 1)..])
                .OrderBy(fileName => fileName)
                .ToArray();
            var actualFiles = Directory.GetFiles(actualDir, "*", SearchOption.AllDirectories).Select(p => p[(actualDir.Length + 1)..])
                .Where(t => !_ignoredFiles.Contains(Path.GetFileName(t)))
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
                var expectedText = await File.ReadAllTextAsync(Path.Combine(expectedDir, file)).ConfigureAwait(false);
                var actualText = await File.ReadAllTextAsync(Path.Combine(actualDir, file)).ConfigureAwait(false);
                try
                {
                    Assert.Equal(expectedText, actualText);
                }
                catch (EqualException ex)
                {
                    Assert.True(false, $"The contents of \"{file}\" do not match.{Environment.NewLine}{ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            foreach (var dir in _temporaryDirectories)
            {
                dir?.Dispose();
            }
        }
    }
}
