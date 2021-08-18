// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class MSBuildRegistrationStartup : IUpgradeStartup
    {
        private static readonly object _sync = new();
        private static string? _version;

        private readonly ITelemetry _telemetry;
        private readonly IOptions<WorkspaceOptions> _options;
        private readonly ILogger _logger;

        public MSBuildRegistrationStartup(
            ITelemetry telemetry,
            IOptions<WorkspaceOptions> options,
            ILogger<MSBuildRegistrationStartup> logger)
        {
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> StartupAsync(CancellationToken token)
            => Task.FromResult(Register());

        public bool Register()
        {
            var msbuildPath = _options.Value.MSBuildPath;

            if (msbuildPath is null)
            {
                throw new UpgradeException("No MSBuild path found");
            }

            var version = RegisterMSBuild(msbuildPath);

            _telemetry.TrackEvent("msbuild", new Dictionary<string, string> { { "MSBuild Version", version } });

            _logger.LogInformation("Registered MSBuild at {Path}", msbuildPath);

            return true;
        }

        private string RegisterMSBuild(string msbuildPath)
        {
            // Can only register MSBuild once, so we verify that before proceeding to register
            if (_version is null)
            {
                lock (_sync)
                {
                    if (_version is null)
                    {
                        var instance = MSBuildLocator.QueryVisualStudioInstances()
                            .FirstOrDefault(i => string.Equals(msbuildPath, i.MSBuildPath, StringComparison.OrdinalIgnoreCase));

                        if (instance is null)
                        {
                            _logger.LogError("No MSBuild instance was found at {Path}", msbuildPath);
                            throw new UpgradeException();
                        }

                        // Must register instance rather than just path so everything gets set correctly for .NET SDK instances
                        MSBuildLocator.RegisterInstance(instance);

                        var resolver = new AssemblyDependencyResolver(msbuildPath);

                        AssemblyLoadContext.Default.Resolving += ResolveAssembly;

                        _version = instance.Version.ToString();
                        return _version;

                        Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
                        {
                            if (context is null || assemblyName is null)
                            {
                                return null;
                            }

                            if (resolver.ResolveAssemblyToPath(assemblyName) is string path)
                            {
                                _logger.LogDebug("Assembly {AssemblyName} loaded into context {ContextName} from {AssemblyPath}", assemblyName.FullName, context.Name, path);
                                return context.LoadFromAssemblyPath(path);
                            }

                            _logger.LogDebug("Unable to resolve assembly {AssemblyName}", assemblyName.FullName);
                            return null;
                        }
                    }
                }
            }

            _logger.LogWarning("MSBuild already registered {Version}", _version);

            return _version;
        }
    }
}
