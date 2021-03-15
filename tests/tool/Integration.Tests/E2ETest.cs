// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Integration.Tests
{
    public class E2ETest
    {
        private const string IntegrationTestAssetsPath = "IntegrationScenarios";
        private const string OriginalProjectSubDir = "Original";
        private const string UpgradedProjectSubDir = "Upgraded";

        private static readonly string[] DirsToIgnore = new[] { "bin", "obj" };

        private readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ".upgrade-assistant"
        };

        private readonly ITestOutputHelper _output;

        public E2ETest(ITestOutputHelper output)
        {
            _output = output;
        }

        [InlineData("AspNetSample/csharp", "TemplateMvc.csproj", "")]
        [InlineData("WpfSample/csharp", "BeanTrader.sln", "BeanTraderClient.csproj")]
        [InlineData("WpfSample/vb", "WpfApp1.sln", "")]
        [InlineData("PCL", "SamplePCL.csproj", "")]
        [InlineData("WebLibrary/csharp", "WebLibrary.csproj", "")]
        [Theory]
        public async Task UpgradeTest(string scenarioPath, string inputFileName, string entrypoint)
        {
            // Create a temporary working directory
            var workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dir = Directory.CreateDirectory(workingDir);
            Assert.True(dir.Exists);

            // Copy the scenario files to the temporary directory
            var scenarioDir = Path.Combine(IntegrationTestAssetsPath, scenarioPath);
            await CopyDirectoryAsync(Path.Combine(scenarioDir, OriginalProjectSubDir), workingDir).ConfigureAwait(false);

            // Run upgrade
            var result = await UpgradeRunner.UpgradeAsync(Path.Combine(workingDir, inputFileName), entrypoint, _output, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

            Assert.True(result);

            CleanupBuildArtifacts(workingDir);

            await AssertDirectoriesEqualAsync(Path.Combine(scenarioDir, UpgradedProjectSubDir), workingDir).ConfigureAwait(false);

            if (Directory.Exists(workingDir))
            {
                Directory.Delete(workingDir, true);
            }
        }

        private async Task AssertDirectoriesEqualAsync(string expectedDir, string actualDir)
        {
            var expectedFiles = Directory.GetFiles(expectedDir, "*", SearchOption.AllDirectories).Select(p => p[(expectedDir.Length + 1)..]).ToArray();
            var actualFiles = Directory.GetFiles(actualDir, "*", SearchOption.AllDirectories).Select(p => p[(actualDir.Length + 1)..])
                .Where(t => !_ignoredFiles.Contains(Path.GetFileName(t)))
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
                    Assert.True(false, $"Was expecting to see the file '{expectedFiles[i]}' but found '{actualFiles[i]}' instead");
                }
            }

            foreach (var file in expectedFiles)
            {
                var expectedText = $"{file}: {await File.ReadAllTextAsync(Path.Combine(expectedDir, file)).ConfigureAwait(false)}";
                var actualText = $"{file}: {await File.ReadAllTextAsync(Path.Combine(actualDir, file)).ConfigureAwait(false)}";

                Assert.Equal(expectedText, actualText);
            }
        }

        private async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            var directoryInfo = new DirectoryInfo(sourceDir);
            foreach (var file in directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var dest = Path.Combine(destinationDir, file.Name);
                await CopyFileAsync(file.FullName, dest).ConfigureAwait(false);
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                await CopyDirectoryAsync(dir.FullName, Path.Combine(destinationDir, dir.Name)).ConfigureAwait(false);
            }
        }

        private static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 65536;
            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions);
            using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, fileOptions);
            await sourceStream.CopyToAsync(destinationStream, bufferSize, CancellationToken.None).ConfigureAwait(false);
        }

        private static void CleanupBuildArtifacts(string workingDir)
        {
            foreach (var dir in DirsToIgnore.SelectMany(d => Directory.GetDirectories(workingDir, d, SearchOption.AllDirectories)))
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }
    }
}
