// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class NuGetPackageSourceFactory : INuGetPackageSourceFactory
    {
        private const string DefaultPackageSource = "https://api.nuget.org/v3/index.json";

        private readonly ILogger<NuGetPackageSourceFactory> _logger;

        public NuGetPackageSourceFactory(ILogger<NuGetPackageSourceFactory> logger)
        {
            _logger = logger;
        }

        public IEnumerable<PackageSource> GetPackageSources(string? path)
        {
            var packageSources = new List<PackageSource>();

            if (path != null)
            {
                var nugetSettings = Settings.LoadDefaultSettings(path);
                var sourceProvider = new PackageSourceProvider(nugetSettings);
                packageSources.AddRange(sourceProvider.LoadPackageSources().Where(e => e.IsEnabled));
            }

            if (packageSources.Count == 0)
            {
                packageSources.Add(new PackageSource(DefaultPackageSource));
            }

            _logger.LogDebug("Found package sources: {PackageSources}", packageSources);

            return packageSources;
        }
    }
}
