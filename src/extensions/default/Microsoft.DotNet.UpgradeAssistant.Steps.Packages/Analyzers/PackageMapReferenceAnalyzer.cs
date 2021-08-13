// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class PackageMapReferenceAnalyzer : IDependencyAnalyzer
    {
        private readonly ILogger<PackageMapReferenceAnalyzer> _logger;
        private readonly IEnumerable<NuGetPackageMap> _packageMaps;
        private readonly IPackageLoader _packageLoader;
        private readonly IVersionComparer _comparer;

        public string Name => "Package map reference analyzer";

        public PackageMapReferenceAnalyzer(
            IOptions<ICollection<NuGetPackageMap[]>> packageMaps,
            IPackageLoader packageLoader,
            IVersionComparer comparer,
            ILogger<PackageMapReferenceAnalyzer> logger)
        {
            if (packageMaps is null)
            {
                throw new ArgumentNullException(nameof(packageMaps));
            }

            _packageMaps = packageMaps.Value.SelectMany(p => p);
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AnalyzeAsync(IProject project, IDependencyAnalysisState state, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            // Get package maps as an array here so that they're only loaded once (as opposed to each iteration through the loop)
            var packageMaps = state.TargetFrameworks.Any(c => c.IsFramework) ? _packageMaps.Where(x => x.NetCorePackagesWorkOnNetFx).ToArray() : _packageMaps;

            foreach (var packageReference in state.Packages)
            {
                foreach (var map in packageMaps.Where(m => ContainsPackageReference(m.NetFrameworkPackages, packageReference.Name, packageReference.Version)))
                {
                    _logger.LogInformation("Marking package {PackageName} for removal based on package mapping configuration {PackageMapSet}", packageReference.Name, map.PackageSetName);
                    state.Packages.Remove(packageReference, new OperationDetails() { Details = ProcessReferenceWarnings(map.PackageSetWarning), Risk = BuildBreakRisk.Medium });
                    await AddNetCoreReferences(map, state, token).ConfigureAwait(false);
                }
            }

            foreach (var reference in project.References)
            {
                foreach (var map in packageMaps.Where(m => m.ContainsAssemblyReference(reference.Name)))
                {
                    _logger.LogInformation("Marking assembly reference {ReferenceName} for removal based on package mapping configuration {PackageMapSet}", reference.Name, map.PackageSetName);
                    state.References.Remove(reference, new OperationDetails() { Details = ProcessReferenceWarnings(map.PackageSetWarning), Risk = BuildBreakRisk.Medium });
                    await AddNetCoreReferences(map, state, token).ConfigureAwait(false);
                }
            }
        }

        private IEnumerable<string> ProcessReferenceWarnings(string warning)
        {
            var details = new List<string>();
            if (!string.IsNullOrEmpty(warning))
            {
                _logger.LogWarning(warning);
                details.Add(warning);
            }

            return details;
        }

        /// <summary>
        /// Determines whether a packages given contain a given package name and version.
        /// </summary>
        /// <param name="name">The package name to look for.</param>
        /// <param name="version">The package version to look for or null to match any version.</param>
        /// <returns>True if the package exists in NetFrameworkPackages with a version equal to or higher the version specified. Otherwise, false.</returns>
        private bool ContainsPackageReference(IEnumerable<NuGetReference> packages, string name, string? version)
        {
            var reference = packages.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            // If no packages matched, return false
            if (reference is null)
            {
                return false;
            }

            // If the version isn't specified, then matching the name is sufficient
            if (version is null || reference.HasWildcardVersion)
            {
                return true;
            }

            return _comparer.Compare(version, reference.Version) <= 0;
        }

        private async Task AddNetCoreReferences(NuGetPackageMap packageMap, IDependencyAnalysisState state, CancellationToken token)
        {
            foreach (var newPackage in packageMap.NetCorePackages)
            {
                var packageToAdd = newPackage;
                if (packageToAdd.HasWildcardVersion)
                {
                    var reference = await _packageLoader.GetLatestVersionAsync(packageToAdd.Name, state.TargetFrameworks, new(), token).ConfigureAwait(false);

                    if (reference is not null)
                    {
                        packageToAdd = reference;
                    }
                }

                if (state.Packages.Add(packageToAdd, new OperationDetails()))
                {
                    _logger.LogInformation("Adding package {PackageName} based on package mapping configuration {PackageMapSet}", packageToAdd.Name, packageMap.PackageSetName);
                }
            }

            foreach (var frameworkReference in packageMap.NetCoreFrameworkReferences)
            {
                if (state.FrameworkReferences.Add(frameworkReference, new OperationDetails()))
                {
                    _logger.LogInformation("Adding framework reference {FrameworkReference} based on package mapping configuration {PackageMapSet}", frameworkReference.Name, packageMap.PackageSetName);
                }
            }
        }
    }
}
