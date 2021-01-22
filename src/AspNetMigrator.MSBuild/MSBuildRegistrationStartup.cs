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
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError("Unexpected error registering MSBuild {Exception}", e);
                return Task.FromResult(false);
            }
        }

        public static string RegisterMSBuildInstance()
        {
            // TODO : Harden this and allow MSBuild location to be read from env vars.
            var msBuildInstance = MSBuildLocator.QueryVisualStudioInstances().First();
            MSBuildLocator.RegisterInstance(msBuildInstance);
            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
            {
                // TODO : Harden this and extract the event handler to its own class
                if (context is null || assemblyName is null)
                {
                    return null;
                }

                // If the assembly has a culture, check for satellite assemblies
                if (assemblyName.CultureInfo != null)
                {
                    var satellitePath = Path.Combine(msBuildInstance.MSBuildPath, assemblyName.CultureInfo.Name, $"{assemblyName.Name}.dll");
                    if (File.Exists(satellitePath))
                    {
                        return context.LoadFromAssemblyPath(satellitePath);
                    }

                    satellitePath = Path.Combine(msBuildInstance.MSBuildPath, assemblyName.CultureInfo.TwoLetterISOLanguageName, $"{assemblyName.Name}.dll");
                    if (File.Exists(satellitePath))
                    {
                        return context.LoadFromAssemblyPath(satellitePath);
                    }
                }

                var assemblyPath = Path.Combine(msBuildInstance.MSBuildPath, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return context.LoadFromAssemblyPath(assemblyPath);
                }

                // TODO : Log missing assembly
                return null;
            };

            return msBuildInstance.MSBuildPath;
        }
    }
}
