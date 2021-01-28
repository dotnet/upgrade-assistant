using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    public class MSBuildRegistrationStartup
    {
        private readonly ILogger _logger;
        private VisualStudioInstance? _msBuildInstance;

        public MSBuildRegistrationStartup(ILogger<MSBuildRegistrationStartup> logger)
        {
            _logger = logger;
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            // Register correct MSBuild for use with SDK-style projects
            try
            {
                var msBuildPath = RegisterMSBuildInstance();
                _logger.LogInformation("MSBuild registered from {MSBuildPath}", msBuildPath);

                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                _logger.LogError("Unexpected error registering MSBuild {Exception}", e);
                return Task.FromResult(false);
            }
        }

        public string RegisterMSBuildInstance()
        {
            if (_msBuildInstance is null)
            {
                // TODO : Harden this and allow MSBuild location to be read from env vars.
                var msBuildInstances = MSBuildLocator.QueryVisualStudioInstances();
                _logger.LogDebug("Found {Count} candidate MSBuild instances:\n\t{MSBuildInstances}", msBuildInstances.Count(), string.Join("\n\t", msBuildInstances.Select(m => m.MSBuildPath)));
                _msBuildInstance = msBuildInstances.First();
                MSBuildLocator.RegisterInstance(_msBuildInstance);
                AssemblyLoadContext.Default.Resolving += ResolveAssembly;
            }

            return _msBuildInstance.MSBuildPath;
        }

        private Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (context is null || assemblyName is null || _msBuildInstance is null)
            {
                return null;
            }

            // If the assembly has a culture, check for satellite assemblies
            if (assemblyName.CultureInfo != null)
            {
                var satellitePath = Path.Combine(_msBuildInstance.MSBuildPath, assemblyName.CultureInfo.Name, $"{assemblyName.Name}.dll");
                if (File.Exists(satellitePath))
                {
                    _logger.LogDebug("Assembly {AssemblyName} loaded into context {ContextName} from {AssemblyPath}", assemblyName.FullName, context.Name, satellitePath);
                    return context.LoadFromAssemblyPath(satellitePath);
                }

                satellitePath = Path.Combine(_msBuildInstance.MSBuildPath, assemblyName.CultureInfo.TwoLetterISOLanguageName, $"{assemblyName.Name}.dll");
                if (File.Exists(satellitePath))
                {
                    _logger.LogDebug("Assembly {AssemblyName} loaded into context {ContextName} from {AssemblyPath}", assemblyName.FullName, context.Name, satellitePath);
                    return context.LoadFromAssemblyPath(satellitePath);
                }
            }

            var assemblyPath = Path.Combine(_msBuildInstance.MSBuildPath, $"{assemblyName.Name}.dll");
            if (File.Exists(assemblyPath))
            {
                _logger.LogDebug("Assembly {AssemblyName} loaded into context {ContextName} from {AssemblyPath}", assemblyName.FullName, context.Name, assemblyPath);
                return context.LoadFromAssemblyPath(assemblyPath);
            }

            _logger.LogDebug("Unable to resolve assembly {AssemblyName}", assemblyName.FullName);
            return null;
        }
    }
}
