// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyze : IAppCommand
    {
        private readonly IUpgradeContextFactory _contextFactory;
        private readonly IUpgradeStateManager _stateManager;
        private readonly ILogger<ConsoleAnalyze> _logger;
        private readonly IEnumerable<IAnalyzeResultProvider> _providers;
        private readonly IAnalyzeResultWriter _writer;

        public ConsoleAnalyze(
            IEnumerable<IAnalyzeResultProvider> analysisProviders,
            IUpgradeContextFactory contextFactory,
            IUpgradeStateManager stateManager,
            IAnalyzeResultWriter writer,
            ILogger<ConsoleAnalyze> logger)
        {
            _providers = analysisProviders;
            _writer = writer;
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(CancellationToken token)
        {
            using var context = await _contextFactory.CreateContext(token);

            await _stateManager.LoadStateAsync(context, token);
            var analzyerContext = new AnalyzeContext(context);
            var analyzeResultMap = new List<AnalyzeResultDefinition>();
            foreach (var provider in _providers)
            {
                analyzeResultMap.Add(new()
                {
                    Version = UpgradeVersion.Current.FullVersion,
                    Name = provider.Name,
                    InformationURI = provider.InformationURI,
                    Id = provider.Id,
                    AnalysisResults = provider.AnalyzeAsync(analzyerContext, token),
                });
            }

            await _writer.WriteAsync(analyzeResultMap.ToAsyncEnumerable(), token).ConfigureAwait(false);
        }
    }
}
