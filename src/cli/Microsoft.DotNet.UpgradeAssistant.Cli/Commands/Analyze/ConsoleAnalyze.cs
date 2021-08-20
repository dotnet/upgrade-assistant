// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
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
        private readonly IExtensionManager _extensionManager;

        public ConsoleAnalyze(
            IEnumerable<IAnalyzeResultProvider> analysisProviders,
            IUpgradeContextFactory contextFactory,
            IUpgradeStateManager stateManager,
            IAnalyzeResultWriter writer,
            IExtensionManager extensionManager,
            ILogger<ConsoleAnalyze> logger)
        {
            _providers = analysisProviders ?? throw new ArgumentNullException(nameof(analysisProviders));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _extensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
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

            foreach (var provider in _providers.Where(i => i.IsApplicableAsync(analzyerContext, token).ConfigureAwait(false).GetAwaiter().GetResult()))
            {
                analyzeResultMap.Add(new()
                {
                    Version = GetProviderVersion(provider),
                    Name = provider.Name,
                    InformationURI = provider.InformationURI,
                    AnalysisResults = provider.AnalyzeAsync(analzyerContext, token),
                });
            }

            await _writer.WriteAsync(analyzeResultMap.ToAsyncEnumerable(), token).ConfigureAwait(false);
        }

        private string GetProviderVersion(IAnalyzeResultProvider provider)
        {
            const string NullVersion = "0.0.0";

            if (_extensionManager.TryGetExtension(provider, out var extension))
            {
                return extension.Version?.ToString() ?? NullVersion;
            }

            return NullVersion;
        }
    }
}
