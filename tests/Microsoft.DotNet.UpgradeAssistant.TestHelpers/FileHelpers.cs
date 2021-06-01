// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class FileHelpers
    {
        private static readonly string[] DirsToIgnore = new[] { "bin", "obj" };

        public static async Task<TemporaryDirectory> CopyDirectoryAsync(string sourceDir, string destinationDir)
        {
            var destDir = await CopyDirectoryImplAsync(sourceDir, destinationDir).ConfigureAwait(false);
            return new TemporaryDirectory(destDir.FullName);
        }

        private static async Task<DirectoryInfo> CopyDirectoryImplAsync(string sourceDir, string destinationDir)
        {
            var destDir = Directory.CreateDirectory(destinationDir);
            var directoryInfo = new DirectoryInfo(sourceDir);
            foreach (var file in directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var dest = Path.Combine(destinationDir, file.Name);
                await CopyFileAsync(file.FullName, dest).ConfigureAwait(false);
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                await CopyDirectoryImplAsync(dir.FullName, Path.Combine(destinationDir, dir.Name)).ConfigureAwait(false);
            }

            return destDir;
        }

        private static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 65536;
            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions);
            using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, fileOptions);
            await sourceStream.CopyToAsync(destinationStream, bufferSize, CancellationToken.None).ConfigureAwait(false);
        }

        public static void CleanupBuildArtifacts(string workingDir)
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
