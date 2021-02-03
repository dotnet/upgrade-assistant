using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    public class MSBuildPackageRestorer : IPackageRestorer
    {
        private const string LockFileName = "project.assets.json";
        private readonly ILogger<MSBuildPackageRestorer> _logger;

        public MSBuildPackageRestorer(ILogger<MSBuildPackageRestorer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RestoreOutput> RestorePackagesAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectInstance = new ProjectInstance(context.Project.Required().FilePath);
            RestorePackages(projectInstance);

            // Reload the project because, by design, NuGet properties (like NuGetPackageRoot)
            // aren't available in a project until after restore is run the first time.
            // https://github.com/NuGet/Home/issues/9150
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);

            // Check for the lock file's existence rather than success since a bad NuGet reference won't
            // prevent other (valid) packages from being restored and we may still have a (partial) lock file.
            var lockFilePath = Path.Combine(projectInstance.GetPropertyValue("MSBuildProjectExtensionsPath"), LockFileName);
            if (!Path.IsPathFullyQualified(lockFilePath))
            {
                lockFilePath = Path.Combine(projectInstance.Directory, lockFilePath);
            }

            // Get the path used for caching NuGet packages
            projectInstance = new ProjectInstance(context.Project.Required().FilePath);
            var nugetCachePath = projectInstance.GetPropertyValue("NuGetPackageRoot");

            return new RestoreOutput(File.Exists(lockFilePath) ? lockFilePath : null, Directory.Exists(nugetCachePath) ? nugetCachePath : null);
        }

        public BuildResult? RestorePackages(ProjectInstance projectInstance)
        {
            if (projectInstance is null)
            {
                throw new ArgumentNullException(nameof(projectInstance));
            }

            var buildParameters = new BuildParameters
            {
                Loggers = new List<Microsoft.Build.Framework.ILogger>
                {
                    new MSBuildExtensionsLogger(_logger, Microsoft.Build.Framework.LoggerVerbosity.Normal)
                }
            };

            var restoreRequest = new BuildRequestData(projectInstance, new[] { "Restore" });
            _logger.LogDebug("Restoring NuGet packages for project {ProjectPath}", projectInstance.FullPath);
            var restoreResult = BuildManager.DefaultBuildManager.Build(buildParameters, restoreRequest);
            _logger.LogDebug("MSBuild exited with status {RestoreStatus}", restoreResult.OverallResult);
            if (restoreResult.Exception != null)
            {
                _logger.LogError(restoreResult.Exception, "MSBuild threw an unexpected exception");
                throw restoreResult.Exception;
            }

            return restoreResult;
        }
    }
}
