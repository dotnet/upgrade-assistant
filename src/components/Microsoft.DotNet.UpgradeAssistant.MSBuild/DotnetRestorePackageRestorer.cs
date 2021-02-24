// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using static System.FormattableString;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class DotnetRestorePackageRestorer : IPackageRestorer
    {
        private static readonly string[] EnvVarsToWithold = new string[] { "MSBuildSDKsPath", "MSBuildExtensionsPath", "MSBUILD_EXE_PATH" };

        private readonly ILogger<DotnetRestorePackageRestorer> _logger;
        private readonly IProcessRunner _runner;

        public DotnetRestorePackageRestorer(ILogger<DotnetRestorePackageRestorer> logger, IProcessRunner runner)
        {
            _logger = logger;
            _runner = runner;
        }

        public async Task<RestoreOutput> RestorePackagesAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await RunRestoreAsync(context, project.FilePath, token);

            // Reload the project because, by design, NuGet properties (like NuGetPackageRoot)
            // aren't available in a project until after restore is run the first time.
            // https://github.com/NuGet/Home/issues/9150
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            return GetRestoreOutput(project);
        }

        private RestoreOutput GetRestoreOutput(IProject project)
        {
            // Check for the lock file's existence rather than success since a bad NuGet reference won't
            // prevent other (valid) packages from being restored and we may still have a (partial) lock file.
            var lockFilePath = project.LockFilePath;

            // Get the path used for caching NuGet packages
            var nugetCachePath = project.GetFile().GetPropertyValue("NuGetPackageRoot");

            return new RestoreOutput(lockFilePath, Directory.Exists(nugetCachePath) ? nugetCachePath : null);
        }

        private Task<bool> RunRestoreAsync(IUpgradeContext context, string path, CancellationToken token)
        {
            // Run `dotnet restore` using quiet mode since some warnings and errors are
            // expected. As long as a lock file is produced (which is checked elsewhere),
            // the tool was successful enough.
            return _runner.RunProcessAsync(new ProcessInfo
            {
                Command = "dotnet",
                Arguments = Invariant($"restore \"{path}\""),
                EnvironmentVariables = context.GlobalProperties,
                Name = "dotnet-restore",
                Quiet = true
            }, token);
        }
    }
}
