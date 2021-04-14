// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Integration.Tests
{
    public class InterceptingKnownPackageLoader : IPackageLoader
    {
        private readonly KnownPackages _packages;
        private readonly IPackageLoader _other;
        private readonly ILogger<InterceptingKnownPackageLoader> _logger;
        private static Dictionary<string, string> _unknownPackages;

        public InterceptingKnownPackageLoader(KnownPackages packages, IPackageLoader other, ILogger<InterceptingKnownPackageLoader> logger)
        {
            _packages = packages ?? throw new ArgumentNullException(nameof(packages));
            _other = other ?? throw new ArgumentNullException(nameof(other));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _unknownPackages = new Dictionary<string, string>();
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
                _unknownPackages[packageName] = latest.Version;
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
                _unknownPackages[reference.Name] = reference.Version;
                _logger.LogError("Unexpected check for newer version: {Name}, {Version}", reference.Name, reference.Version);
            }

            return latest ?? Array.Empty<NuGetReference>();
        }

        public static void AssertOnlyKnownPackagesWereReferenced(string actualDirectory)
        {
            if (!_unknownPackages.Keys.Any())
            {
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            var uknownPackageStr = JsonSerializer.Serialize(_unknownPackages, options);
            var outputFile = Path.Combine(actualDirectory, "UnknownPackages.json");
            File.WriteAllText(outputFile, uknownPackageStr);
            Assert.False(true, $"Integration tests tried to access NuGet.{Environment.NewLine}The list of packages not yet \"pinned\" has been written to:{Environment.NewLine}{outputFile}");
        }
    }
}
