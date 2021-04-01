// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    public class PackageMapReferenceAnalyzer : IPackageReferencesAnalyzer
    {
        private readonly ILogger<PackageMapReferenceAnalyzer> _logger;
        private readonly PackageMapProvider _packageMapProvider;
        private readonly IPackageLoader _packageLoader;
        private readonly IVersionComparer _comparer;

        public string Name => "Package map reference analyzer";

        public PackageMapReferenceAnalyzer(
            PackageMapProvider packageMapProvider,
            IPackageLoader packageLoader,
            IVersionComparer comparer,
            ILogger<PackageMapReferenceAnalyzer> logger)
        {
            _packageMapProvider = packageMapProvider ?? throw new ArgumentNullException(nameof(packageMapProvider));
            _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PackageAnalysisState> AnalyzeAsync(IProject project, PackageAnalysisState state, CancellationToken token)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var currentTFM = project.TargetFrameworks;

            // Get package maps as an array here so that they're only loaded once (as opposed to each iteration through the loop)
            var allPackageMaps = await _packageMapProvider.GetPackageMapsAsync(token).ToArrayAsync(token).ConfigureAwait(false);
            var packageMaps = currentTFM.Any(c => c.IsFramework) ? allPackageMaps.Where(x => x.NetCorePackagesWorkOnNetFx).ToArray<NuGetPackageMap>() : allPackageMaps;

            foreach (var packageReference in project.NuGetReferences.PackageReferences.Where(r => !state.PackagesToRemove.Contains(r)))
            {
                foreach (var map in packageMaps.Where(m => ContainsPackageReference(m.NetFrameworkPackages, packageReference.Name, packageReference.Version)))
                {
                    state.PossibleBreakingChangeRecommended = true;
                    _logger.LogInformation("Marking package {PackageName} for removal based on package mapping configuration {PackageMapSet}", packageReference.Name, map.PackageSetName);
                    state.PackagesToRemove.Add(packageReference);
                    await AddNetCoreReferences(map, state, project, token).ConfigureAwait(false);
                }
            }

            foreach (var reference in project.References.Where(r => !state.ReferencesToRemove.Contains(r)))
            {
                foreach (var map in packageMaps.Where(m => m.ContainsAssemblyReference(reference.Name)))
                {
                    state.PossibleBreakingChangeRecommended = true;
                    _logger.LogInformation("Marking assembly reference {ReferenceName} for removal based on package mapping configuration {PackageMapSet}", reference.Name, map.PackageSetName);
                    state.ReferencesToRemove.Add(reference);
                    await AddNetCoreReferences(map, state, project, token).ConfigureAwait(false);
                }
            }

            return state;
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

        private async Task AddNetCoreReferences(NuGetPackageMap packageMap, PackageAnalysisState state, IProject project, CancellationToken token)
        {
            foreach (var newPackage in packageMap.NetCorePackages)
            {
                var packageToAdd = newPackage;
                if (packageToAdd.HasWildcardVersion)
                {
                    var reference = await _packageLoader.GetLatestVersionAsync(packageToAdd.Name, false, null, token).ConfigureAwait(false);

                    if (reference is not null)
                    {
                        packageToAdd = reference;
                    }
                }

                if (!state.PackagesToAdd.Contains(packageToAdd) && !project.NuGetReferences.PackageReferences.Contains(packageToAdd))
                {
                    _logger.LogInformation("Adding package {PackageName} based on package mapping configuration {PackageMapSet}", packageToAdd.Name, packageMap.PackageSetName);
                    state.PackagesToAdd.Add(packageToAdd);
                }
            }

            foreach (var frameworkReference in packageMap.NetCoreFrameworkReferences)
            {
                if (!state.FrameworkReferencesToAdd.Contains(frameworkReference) && !project.FrameworkReferences.Contains(frameworkReference))
                {
                    _logger.LogInformation("Adding framework reference {FrameworkReference} based on package mapping configuration {PackageMapSet}", frameworkReference.Name, packageMap.PackageSetName);
                    state.FrameworkReferencesToAdd.Add(frameworkReference);
                }
            }
        }
    }
}
