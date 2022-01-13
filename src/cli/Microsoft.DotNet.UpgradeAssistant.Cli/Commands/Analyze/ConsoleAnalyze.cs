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
using Microsoft.Extensions.Options;
using Serilog;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyze : IAppCommand
    {
        private readonly IUpgradeContextFactory _contextFactory;
        private readonly IUpgradeStateManager _stateManager;
        private readonly IEnumerable<IAnalyzeResultProvider> _providers;
        private readonly IExtensionProvider _extensionProvider;
        private readonly IOptions<AnalysisOptions> _options;
        private readonly IAnalyzeResultWriterProvider _writerProvider;
        private readonly ILogger<ConsoleAnalyze> _logger;

        public ConsoleAnalyze(
            IEnumerable<IAnalyzeResultProvider> analysisProviders,
            IUpgradeContextFactory contextFactory,
            IUpgradeStateManager stateManager,
            IExtensionProvider extensionProvider,
            IOptions<AnalysisOptions> options,
            IAnalyzeResultWriterProvider writerProvider,
            ILogger<ConsoleAnalyze> logger)
        {
            _providers = analysisProviders ?? throw new ArgumentNullException(nameof(analysisProviders));
            _extensionProvider = extensionProvider ?? throw new ArgumentNullException(nameof(extensionProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _writerProvider = writerProvider ?? throw new ArgumentNullException(nameof(writerProvider));
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

            await foreach (var provider in _providers.ToAsyncEnumerable().WhereAwait(async i => await i.IsApplicableAsync(analzyerContext, token)))
            {
                analyzeResultMap.Add(new()
                {
                    Version = GetProviderVersion(provider),
                    Name = provider.Name,
                    InformationUri = provider.InformationUri,
                    AnalysisResults = provider.AnalyzeAsync(analzyerContext, token),
                });
            }

            if (_writerProvider.TryGetWriter(_options.Value.Format, out var writer))
            {
                _logger.LogInformation("Writing to output format {Format}", _options.Value.Format);
                await writer.WriteAsync(analyzeResultMap.ToAsyncEnumerable(), _options.Value.Format, token).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError("Requested format '{Format}' is unavailable", _options.Value.Format);
            }
        }

        private string GetProviderVersion(IAnalyzeResultProvider provider)
        {
            const string NullVersion = "0.0.0";

            if (_extensionProvider.TryGetExtension(provider, out var extension))
            {
                return extension.Version?.ToString() ?? NullVersion;
            }

            return NullVersion;
        }
    }
}
