// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Packages;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageAnalyzer : IPackageAnalyzer
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IDependencyAnalyzer> _packageAnalyzers;

        protected ILogger Logger { get; }

        public PackageAnalyzer(IPackageRestorer packageRestorer,
            IEnumerable<IDependencyAnalyzer> packageAnalyzers,
            ILogger logger)
        {
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            Logger = logger;
        }

        public async Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, DependencyAnalysisState? analysisState, CancellationToken token)
        {
            if (context?.CurrentProject is null)
            {
                return false;
            }

            await _packageRestorer.RestorePackagesAsync(context, context.CurrentProject, token).ConfigureAwait(false);
            var nugetReferences = await context.CurrentProject.GetNuGetReferencesAsync(token).ConfigureAwait(false);

            analysisState = new DependencyAnalysisState(context.CurrentProject, nugetReferences);

            var projectRoot = context.CurrentProject;

            if (projectRoot is null)
            {
                Logger.LogError("No project available");
                return false;
            }

            // Iterate through all package references in the project file
            foreach (var analyzer in _packageAnalyzers)
            {
                Logger.LogDebug("Analyzing packages with {AnalyzerName}", analyzer.Name);
                try
                {
                    await analyzer.AnalyzeAsync(projectRoot, analysisState, token).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Logger.LogCritical("Package analysis failed (analyzer {AnalyzerName}: {Message}", analyzer.Name, e.Message);
                    return false;
                }
            }

            return true;
        }
    }
}
