// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Integration.Tests.Integration.Tests;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Microsoft.Extensions.Logging;

namespace Integration.Tests
{
    internal class InterceptingKnownPackageLoader : IPackageLoader
    {
        private readonly KnownPackages _packages;
        private readonly IPackageLoader _other;
        private readonly ILogger<InterceptingKnownPackageLoader> _logger;

        public InterceptingKnownPackageLoader(KnownPackages packages, IPackageLoader other, ILogger<InterceptingKnownPackageLoader> logger)
        {
            _packages = packages;
            _other = other;
            _logger = logger;
        }

        public IEnumerable<string> PackageSources => _other.PackageSources;

        public Task<bool> DoesPackageSupportTargetFrameworksAsync(NuGetReference packageReference, IEnumerable<TargetFrameworkMoniker> targetFrameworks, CancellationToken token)
        {
            return _other.DoesPackageSupportTargetFrameworksAsync(packageReference, targetFrameworks, token);
        }

        public async Task<NuGetReference?> GetLatestVersionAsync(string packageName, bool includePreRelease, string[]? packageSources, CancellationToken token)
        {
            if (_packages.TryGetValue(packageName, out var known))
            {
                return known;
            }

            var latest = await _other.GetLatestVersionAsync(packageName, includePreRelease, packageSources, token).ConfigureAwait(false);

            if (latest is not null)
            {
                _logger.LogError("Unexpected version: {Name}, {Version}", latest.Name, latest.Version);
            }

            return latest;
        }

        public async Task<IEnumerable<NuGetReference>> GetNewerVersionsAsync(NuGetReference reference, bool latestMinorAndBuildOnly, CancellationToken token)
        {
            if (_packages.TryGetValue(reference.Name, out var known))
            {
                return new NuGetReference[] { known };
            }

            var latest = await _other.GetNewerVersionsAsync(reference, latestMinorAndBuildOnly, token).ConfigureAwait(false);

            if (latest is not null)
            {
                _logger.LogError("Unexpected check for newer version: {Name}, {Version}", reference.Name, reference.Version);
            }

            return latest;
        }
    }
}
