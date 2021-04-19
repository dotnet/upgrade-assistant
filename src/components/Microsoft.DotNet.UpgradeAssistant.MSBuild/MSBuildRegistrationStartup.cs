// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class MSBuildRegistrationStartup : IUpgradeStartup
    {
        private static readonly object _sync = new object();

        // MSBuildInstance is stored in a static so that multiple
        // instances of MSBuildRegistrationStartup (which should only
        // happen in test scenarios) will share a single instance
        // per process.
        private static VisualStudioInstance? _instance;

        private readonly ILogger _logger;

        public MSBuildRegistrationStartup(ILogger<MSBuildRegistrationStartup> logger)
        {
            _logger = logger;
        }

        public Task<bool> StartupAsync(CancellationToken token)
        {
            RegisterMSBuildInstance();
            return Task.FromResult(true);
        }

        public string RegisterMSBuildInstance()
        {
            if (_instance is null)
            {
                lock (_sync)
                {
                    // This may be null if called concurrently
#pragma warning disable CA1508 // Avoid dead conditional code
                    if (_instance is null)
#pragma warning restore CA1508 // Avoid dead conditional code
                    {
                        // TODO : Harden this and allow MSBuild location to be read from env vars.
                        var msBuildInstances = FilterForBitness(MSBuildLocator.QueryVisualStudioInstances()).ToList();

                        if (msBuildInstances.Count == 0)
                        {
                            _logger.LogError($"No supported MSBuild found. Ensure `dotnet --list-sdks` show an install that is {ExpectedBitness}");
                            throw new UpgradeException("MSBuild not found");
                        }
                        else
                        {
                            foreach (var instance in msBuildInstances)
                            {
                                _logger.LogDebug("Found candidate MSBuild instances: {Path}", instance.MSBuildPath);
                            }

                            _instance = msBuildInstances
                                .OrderByDescending(m => m.Version)
                                .Where(m => m.Version.Major != 6)
                                .Where(m => m.Version.Build != 300)
                                .First();
                            _logger.LogInformation("MSBuild registered from {MSBuildPath}", _instance.MSBuildPath);

                            MSBuildLocator.RegisterInstance(_instance);
                            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                        }
                    }
                }
            }

            return _instance.MSBuildPath;
        }

        private IEnumerable<VisualStudioInstance> FilterForBitness(IEnumerable<VisualStudioInstance> instances)
        {
            foreach (var instance in instances)
            {
                var is32bit = instance.MSBuildPath.Contains("x86", StringComparison.OrdinalIgnoreCase);

                if (Environment.Is64BitProcess == !is32bit)
                {
                    yield return instance;
                }
                else
                {
                    _logger.LogDebug("Skipping {Path} as it is {Bitness}", instance.MSBuildPath, ExpectedBitness);
                }
            }
        }

        private static string ExpectedBitness => Environment.Is64BitProcess ? "64-bit" : "32-bit";

        private Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (context is null || assemblyName is null || _instance is null)
            {
                return null;
            }

            // If the assembly has a culture, check for satellite assemblies
            if (assemblyName.CultureInfo != null)
            {
                var satellitePath = Path.Combine(_instance.MSBuildPath, assemblyName.CultureInfo.Name, $"{assemblyName.Name}.dll");
                if (File.Exists(satellitePath))
                {
                    _logger.LogDebug("Assembly {AssemblyName} loaded into context {ContextName} from {AssemblyPath}", assemblyName.FullName, context.Name, satellitePath);
                    return context.LoadFromAssemblyPath(satellitePath);
                }

                satellitePath = Path.Combine(_instance.MSBuildPath, assemblyName.CultureInfo.TwoLetterISOLanguageName, $"{assemblyName.Name}.dll");
                if (File.Exists(satellitePath))
                {
                    _logger.LogDebug("Assembly {AssemblyName} loaded into context {ContextName} from {AssemblyPath}", assemblyName.FullName, context.Name, satellitePath);
                    return context.LoadFromAssemblyPath(satellitePath);
                }
            }

            var assemblyPath = Path.Combine(_instance.MSBuildPath, $"{assemblyName.Name}.dll");
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
