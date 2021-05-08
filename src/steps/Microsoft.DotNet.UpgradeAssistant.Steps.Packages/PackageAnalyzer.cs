// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageAnalyzer : IPackageAnalyzer
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IPackageReferencesAnalyzer> _packageAnalyzers;

        protected ILogger Logger { get; }

        public PackageAnalyzer(IPackageRestorer packageRestorer,
            IEnumerable<IPackageReferencesAnalyzer> packageAnalyzers,
            ILogger logger)
        {
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            Logger = logger;
        }

        public async Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, PackageAnalysisState? analysisState, CancellationToken token)
        {
            analysisState = await PackageAnalysisState.CreateAsync(context, _packageRestorer, token).ConfigureAwait(false);
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
                analysisState = await analyzer.AnalyzeAsync(projectRoot, analysisState, token).ConfigureAwait(false);
                if (analysisState.Failed)
                {
                    Logger.LogCritical("Package analysis failed (analyzer {AnalyzerName}", analyzer.Name);
                    return false;
                }
            }

            return true;
        }
    }
}
