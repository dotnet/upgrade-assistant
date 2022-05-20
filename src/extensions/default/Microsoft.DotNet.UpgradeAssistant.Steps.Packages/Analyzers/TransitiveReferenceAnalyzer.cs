// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Analyzers
{
    [Order(int.MaxValue)]
    public class TransitiveReferenceAnalyzer : IDependencyAnalyzer
    {
        private readonly ITransitiveDependencyIdentifier _transitiveChecker;
        private readonly IVersionComparer _comparer;

        public TransitiveReferenceAnalyzer(ITransitiveDependencyIdentifier transitiveChecker, IVersionComparer comparer)
        {
            _transitiveChecker = transitiveChecker ?? throw new ArgumentNullException(nameof(transitiveChecker));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public string Name => "Transitive reference analyzer";

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

            var allPackages = new HashSet<NuGetReference>(state.Packages.Concat(project.PackageReferences));
            var closure = await _transitiveChecker.GetTransitiveDependenciesAsync(allPackages, state.TargetFrameworks, token).ConfigureAwait(false);
            var dependencyLookup = allPackages
                .SelectMany(p => closure.GetDependencies(p))
                .ToLookup(p => p.Name);

            var toRemove = state.Packages
                .Where(p =>
                {
                    // Temporary fix for packages to exclude from the removal list
                    // Until we have a fix for https://github.com/dotnet/upgrade-assistant/issues/1069
                    if (p.Name == "Microsoft.WindowsAppSDK")
                    {
                        return false;
                    }

                    // Only remove a package iff it is transitively brought in with a higher or equal version
                    var versions = dependencyLookup[p.Name].Select(static d => d.Version);

                    if (_comparer.TryFindBestVersion(versions, out var best))
                    {
                        return _comparer.Compare(p.Version, best) <= 0;
                    }

                    return false;
                })
                .ToList();

            foreach (var packageReference in toRemove)
            {
                var logMessage = SR.Format("Package {0} needs to be removed as its a transitive dependency that is not required", packageReference.Name);

                state.Packages.Remove(packageReference, new OperationDetails { Details = new[] { logMessage } });
            }
        }
    }
}
