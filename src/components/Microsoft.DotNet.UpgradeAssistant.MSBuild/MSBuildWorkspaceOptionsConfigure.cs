// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class MSBuildWorkspaceOptionsConfigure : IConfigureOptions<WorkspaceOptions>
    {
        private readonly ILogger<MSBuildWorkspaceOptionsConfigure> _logger;

        public MSBuildWorkspaceOptionsConfigure(ILogger<MSBuildWorkspaceOptionsConfigure> logger)
        {
            _logger = logger;
        }

        public void Configure(WorkspaceOptions options)
        {
            if (options.MSBuildPath is string expectedPath)
            {
                _logger.LogInformation("Using supplied path for MSBuild [{Path}]", expectedPath);
            }
            else
            {
                options.MSBuildPath = FindMSBuildPath();
            }
        }

        public string FindMSBuildPath()
        {
            // TODO : Harden this and allow MSBuild location to be read from env vars.
            var msBuildInstances = FilterForBitness(MSBuildLocator.QueryVisualStudioInstances())
                .OrderByDescending(m => m.Version)
                .ToList();

            if (msBuildInstances.Count == 0)
            {
                _logger.LogError($"No supported MSBuild found. Ensure `dotnet --list-sdks` show an install that is {ExpectedBitness}");
                throw new UpgradeException("MSBuild not found");
            }
            else
            {
                foreach (var instance in msBuildInstances)
                {
                    _logger.LogInformation("Found candidate MSBuild instances: {Path}", instance.MSBuildPath);
                }

                var selected = msBuildInstances.First();

                _logger.LogInformation("MSBuild registered from {MSBuildPath}", selected.MSBuildPath);

                return selected.MSBuildPath;
            }
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
    }
}
