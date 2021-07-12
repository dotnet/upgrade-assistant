// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyze : IAppCommand
    {
        private readonly IUpgradeContextFactory _contextFactory;
        private readonly IUpgradeStateManager _stateManager;
        private readonly ILogger<ConsoleAnalyze> _logger;
        private readonly IEnumerable<IAnalyzeResultProvider> _providers;

        public ConsoleAnalyze(
            IEnumerable<IAnalyzeResultProvider> analysisProviders,
            IUpgradeContextFactory contextFactory,
            IUpgradeStateManager stateManager,
            ILogger<ConsoleAnalyze> logger)
        {
            _providers = analysisProviders;
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [ObsoleteAttribute("This property is WIP, expect changes in this area.", false)]
        public async Task RunAsync(CancellationToken token)
        {
            _logger.LogWarning("The Analyze command feature is still under development, expect things to be not fully functional at the moment");
            using var context = await _contextFactory.CreateContext(token);

            await _stateManager.LoadStateAsync(context, token);
            var analzyerContext = new AnalyzeContext(context);
            foreach (var provider in _providers)
            {
                await provider.AnalyzeAsync(analzyerContext, token);
            }
        }
    }
}
