// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageAnalyzer : IPackageAnalyzer
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IDependencyAnalyzer> _packageAnalyzers;

        private readonly ILogger _logger;

        public PackageAnalyzer(IPackageRestorer packageRestorer,
            IEnumerable<IDependencyAnalyzer> packageAnalyzers,
            ILogger logger)
        {
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            _logger = logger;
        }

        public async Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, IDependencyAnalysisState? analysisState, CancellationToken token)
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
                _logger.LogError("No project available");
                return false;
            }

            // Iterate through all package references in the project file
            foreach (var analyzer in _packageAnalyzers)
            {
                _logger.LogDebug("Analyzing packages with {AnalyzerName}", analyzer.Name);
                try
                {
                    await analyzer.AnalyzeAsync(projectRoot, analysisState, token).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.LogCritical("Package analysis failed (analyzer {AnalyzerName}: {Message}", analyzer.Name, e.Message);
                    return false;
                }
            }

            return true;
        }
    }
}
