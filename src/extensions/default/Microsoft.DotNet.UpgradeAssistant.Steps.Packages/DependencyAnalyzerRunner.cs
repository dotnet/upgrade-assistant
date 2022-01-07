// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class DependencyAnalyzerRunner : IDependencyAnalyzerRunner
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IDependencyAnalyzer> _packageAnalyzers;
        private readonly ILogger<DependencyAnalyzerRunner> _logger;

        public DependencyAnalyzerRunner(
            IPackageRestorer packageRestorer,
            IEnumerable<IDependencyAnalyzer> packageAnalyzers,
            ILogger<DependencyAnalyzerRunner> logger)
        {
            if (packageAnalyzers is null)
            {
                throw new ArgumentNullException(nameof(packageAnalyzers));
            }

            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers.OrderyByPrecedence();
            _logger = logger;
        }

        public async Task<IDependencyAnalysisState> AnalyzeAsync(IUpgradeContext context, IProject? projectRoot, IReadOnlyCollection<TargetFrameworkMoniker> targetframeworks, CancellationToken token)
        {
            if (projectRoot is null)
            {
                _logger.LogError("No project available");
                throw new ArgumentNullException(nameof(projectRoot));
            }

            await _packageRestorer.RestorePackagesAsync(context, projectRoot, token).ConfigureAwait(false);
            var analysisState = new DependencyAnalysisState(projectRoot, projectRoot.NuGetReferences, targetframeworks);

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
                    analysisState.IsValid = false;
                }
            }

            return analysisState;
        }
    }
}
