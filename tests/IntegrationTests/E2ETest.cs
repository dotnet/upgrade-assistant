using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntegrationTests
{
    [TestClass]
    public class E2ETest
    {
        // Path relative from .\bin\debug\net5.0
        // TODO : Make this configurable so the test can pass from other working dirs
        private const string IntegrationTestAssetsPath = @"..\..\..\..\TestAssets\IntegrationScenarios";
        private const string CommandsFileName = "Commands.txt";
        private const string OriginalProjectSubDir = "Original";
        private const string MigratedProjectSubDir = "Migrated";

        private const string TryConvertPath = @"%USERPROFILE%\.dotnet\tools\try-convert.exe";

        private static readonly string[] DirsToIgnore = new[] { "bin", "obj" };

        private readonly HashSet<string> _ignoredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".aspnetmigrator"
        };

        [AssemblyInitialize]
#pragma warning disable IDE0060 // Remove unused parameter (required by MSTest)
        public static void InstallTryConvert(TestContext context)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var tryConvertPath = Environment.ExpandEnvironmentVariables(TryConvertPath);
            if (!File.Exists(tryConvertPath))
            {
                // Attempt to install try-convert
                var p = Process.Start("dotnet", "tool install -g try-convert");
                p.WaitForExit();
            }
        }

        [DataRow("AspNetMvcTemplate", "TemplateMvc.csproj")]
        [DataTestMethod]
        public async Task MigrationTest(string scenarioName, string inputFileName)
        {
            // Create a temporary working directory
            var workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                var dir = Directory.CreateDirectory(workingDir);
                Assert.IsTrue(dir.Exists);

                // Copy the scenario files to the temporary directory
                var scenarioDir = Path.Combine(IntegrationTestAssetsPath, scenarioName);
                await CopyDirectoryAsync(Path.Combine(scenarioDir, OriginalProjectSubDir), workingDir).ConfigureAwait(false);

                // Read commands
                var commands = await File.ReadAllLinesAsync(Path.Combine(scenarioDir, CommandsFileName)).ConfigureAwait(false);

                // Run migration
                await MigrationRunner.MigrateAsync(Path.Combine(workingDir, inputFileName), Console.Out, commands, 300).ConfigureAwait(false);
                CleanupBuildArtifacts(workingDir);

                await AssertDirectoriesEqualAsync(Path.Combine(scenarioDir, MigratedProjectSubDir), workingDir).ConfigureAwait(false);
            }
            finally
            {
                if (Directory.Exists(workingDir))
                {
                    Directory.Delete(workingDir, true);
                }
            }
        }

        private async Task AssertDirectoriesEqualAsync(string expectedDir, string actualDir)
        {
            var expectedFiles = Directory.GetFiles(expectedDir, "*", SearchOption.AllDirectories).Select(p => p[(expectedDir.Length + 1)..]).ToArray();
            var actualFiles = Directory.GetFiles(actualDir, "*", SearchOption.AllDirectories).Select(p => p[(actualDir.Length + 1)..])
                .Where(t => !_ignoredFiles.Contains(Path.GetFileName(t)))
                .ToArray();

            CollectionAssert.AreEquivalent(
                expectedFiles,
                actualFiles,
                $"Expected but not actual: {string.Join(", ", expectedFiles.Where(f => !actualFiles.Contains(f)))}\nActual but not expected:{string.Join(", ", actualFiles.Where(f => !expectedFiles.Contains(f)))}");

            foreach (var file in expectedFiles)
            {
                var expectedText = $"{file}: {await File.ReadAllTextAsync(Path.Combine(expectedDir, file)).ConfigureAwait(false)}";
                var actualText = $"{file}: {await File.ReadAllTextAsync(Path.Combine(actualDir, file)).ConfigureAwait(false)}";

                Assert.AreEqual(expectedText, actualText);
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
            foreach (var dir in DirsToIgnore.Select(d => Path.Combine(workingDir, d)))
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }
    }
}
