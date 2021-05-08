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
        private readonly IUserInput _input;
        private readonly InputOutputStreams _io;
        private readonly IUpgradeContextFactory _contextFactory;
        private readonly CommandProvider _commandProvider;
        private readonly UpgraderManager _upgrader;
        private readonly IUpgradeStateManager _stateManager;
        private readonly ILogger<ConsoleAnalyze> _logger;
        private readonly UpgradeStep _step;
        private readonly IEnumerable<IAnalyzeResultProvider> _providers;

        public ConsoleAnalyze(
            IEnumerable<IAnalyzeResultProvider> analysisProviders,
            IUserInput input,
            InputOutputStreams io,
            IUpgradeContextFactory contextFactory,
            CommandProvider commandProvider,
            UpgraderManager upgrader,
            IUpgradeStateManager stateManager,
            UpgradeStep step,
            ILogger<ConsoleAnalyze> logger)
        {
            _providers = analysisProviders;
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _commandProvider = commandProvider ?? throw new ArgumentNullException(nameof(commandProvider));
            _upgrader = upgrader ?? throw new ArgumentNullException(nameof(upgrader));
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _step = step ?? throw new ArgumentNullException(nameof(step));
        }

        public async Task RunAsync(CancellationToken token)
        {
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

