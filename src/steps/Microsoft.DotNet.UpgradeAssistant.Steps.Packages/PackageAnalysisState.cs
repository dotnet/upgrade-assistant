// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageAnalysisState
    {
        public string PackageCachePath { get; private set; } = default!;

        public IList<Reference> FrameworkReferencesToAdd { get; }

        public IList<Reference> FrameworkReferencesToRemove { get; }

        public IList<NuGetReference> PackagesToAdd { get; }

        public IList<NuGetReference> PackagesToRemove { get; }

        public IList<Reference> ReferencesToRemove { get; }

        public bool Failed { get; set; }

        public bool PossibleBreakingChangeRecommended { get; set; }

        public bool ChangesRecommended =>
            FrameworkReferencesToAdd.Any()
            || FrameworkReferencesToRemove.Any()
            || PackagesToAdd.Any()
            || PackagesToRemove.Any()
            || ReferencesToRemove.Any();

        private PackageAnalysisState()
        {
            FrameworkReferencesToAdd = new List<Reference>();
            FrameworkReferencesToRemove = new List<Reference>();
            PackagesToRemove = new List<NuGetReference>();
            PackagesToAdd = new List<NuGetReference>();
            ReferencesToRemove = new List<Reference>();
            Failed = false;
            PossibleBreakingChangeRecommended = false;
        }

        /// <summary>
        /// Creates a new analysis state object for a given upgrade context. This involves restoring packages for the context's current project.
        /// </summary>
        /// <param name="context">The upgrade context to create an analysis state for.</param>
        /// <param name="packageRestorer">The package restorer to use to restore packages for the context's project and generate a lock file.</param>
        /// <returns>A new PackageAnalysisState instance for the specified context.</returns>
        public static async Task<PackageAnalysisState> CreateAsync(IUpgradeContext context, IPackageRestorer packageRestorer, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (packageRestorer is null)
            {
                throw new ArgumentNullException(nameof(packageRestorer));
            }

            if (context.CurrentProject is null)
            {
                throw new InvalidOperationException("Target TFM must be set before analyzing package references");
            }

            var ret = new PackageAnalysisState();
            await ret.PopulatePackageRestoreState(context, packageRestorer, token).ConfigureAwait(false);
            return ret;
        }

        private async Task<bool> PopulatePackageRestoreState(IUpgradeContext context, IPackageRestorer packageRestorer, CancellationToken token)
        {
            if (PackageCachePath is null || Failed)
            {
                var restoreOutput = await packageRestorer.RestorePackagesAsync(context, context.CurrentProject.Required(), token).ConfigureAwait(false);
                if (restoreOutput.PackageCachePath is null)
                {
                    Failed = true;
                    return false;
                }
                else
                {
                    PackageCachePath = restoreOutput.PackageCachePath;
                    Failed = false;
                }
            }

            return true;
        }
    }
}
