// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NuGet.DependencyResolver;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyzeBinaries : IAppCommand
    {
        private readonly IOptions<OutputOptions> _analysisOptions;
        private readonly IBinaryAnalysisExecutor _apiChecker;
        private readonly IOutputResultWriterProvider _writerProvider;
        private readonly ILogger<ConsoleAnalyzeBinaries> _logger;
        private readonly IExtensionProvider _extensionProvider;

        public ConsoleAnalyzeBinaries(IOptions<OutputOptions> analysisOptions,
            IBinaryAnalysisExecutor apiChecker,
            IOutputResultWriterProvider writerProvider,
            IExtensionProvider extensionProvider,
            ILogger<ConsoleAnalyzeBinaries> logger)
        {
            _analysisOptions = analysisOptions ?? throw new ArgumentNullException(nameof(analysisOptions));
            _apiChecker = apiChecker ?? throw new ArgumentNullException(nameof(apiChecker));
            _writerProvider = writerProvider ?? throw new ArgumentNullException(nameof(writerProvider));
            _extensionProvider = extensionProvider ?? throw new ArgumentNullException(nameof(extensionProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(CancellationToken token)
        {
            if (_writerProvider.TryGetWriter(_analysisOptions.Value.Format, out var writer))
            {
                const string resultDefName = "BinaryAnalysis";
                var resultDefInformationUri = new Uri("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");
                var version = GetExtensionVersion();

                var output = Path.Combine(Directory.GetCurrentDirectory(), $"AnalysisReport.{_analysisOptions.Value.Format}");

                _logger.LogInformation(LocalizedStrings.WritingOutputMessage, output);

                using var stream = File.Create(output);

                var allResults = new ConcurrentDictionary<string, ConcurrentBag<OutputResult>>();

                await _apiChecker.RunAsync(result => Task.Run(() =>
                {
                    var bagToPopulate = allResults.GetOrAdd(result.FileLocation, new ConcurrentBag<OutputResult>());
                    bagToPopulate.Add(result);
                }));

                await writer.WriteAsync(allResults
                    .Select(g => new OutputResultDefinition
                    {
                        Results = g.Value.ToAsyncEnumerable(),
                        Name = $"{resultDefName} | {g.Key}",
                        Version = version,
                        InformationUri = resultDefInformationUri,
                    }).ToAsyncEnumerable(), stream, token);

                _logger.LogInformation(LocalizedStrings.AnalysisCompleteMessage, output);
            }
            else
            {
                _logger.LogError(LocalizedStrings.RequestedFormatUnavailableMessage, _analysisOptions.Value.Format);
            }
        }

        private string GetExtensionVersion()
        {
            const string NullVersion = "0.0.0";

            if (_extensionProvider.TryGetExtension(_apiChecker, out var extension))
            {
                return extension.Version?.ToString() ?? NullVersion;
            }

            return NullVersion;
        }
    }
}
