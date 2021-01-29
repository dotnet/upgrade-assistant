using System;
using System.Collections.Generic;
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

        public string RegisterMSBuildInstance()
        {
            if (_msBuildInstance is null)
            {
                // TODO : Harden this and allow MSBuild location to be read from env vars.
                var msBuildInstances = FilterForBitness(MSBuildLocator.QueryVisualStudioInstances()).ToList();

                if (msBuildInstances.Count == 0)
                {
                    var expected = Environment.Is64BitProcess ? "64-bit" : "32-bit";
                    _logger.LogError($"No supported MSBuild found. Ensure `dotnet --list-sdks` show an install that is {expected}");
                    throw new MigrationException("MSBuild not found");
                }
                else
                {
                    _logger.LogDebug("Found {Count} candidate MSBuild instances:\n\t{MSBuildInstances}", msBuildInstances.Count, string.Join("\n\t", msBuildInstances.Select(m => m.MSBuildPath)));
                    _msBuildInstance = msBuildInstances.First();
                    MSBuildLocator.RegisterInstance(_msBuildInstance);
                    AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                }
            }

            return _msBuildInstance.MSBuildPath;
        }

        private IEnumerable<VisualStudioInstance> FilterForBitness(IEnumerable<VisualStudioInstance> instances)
        {
            foreach (var instance in instances)
            {
                var is32bit = instance.MSBuildPath.Contains("x86", StringComparison.OrdinalIgnoreCase);

                if (Environment.Is64BitProcess && !is32bit)
                {
                    yield return instance;
                }
                else
                {
                    _logger.LogDebug("Skipping {Path} as it is 32-bit", instance.MSBuildPath);
                }
            }
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
