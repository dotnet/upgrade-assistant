// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class HtmlAnalyzeResultWriter : IOutputResultWriter
    {
        private readonly Lazy<IOutputResultWriter?> _sarifWriter;
        private readonly ILogger<HtmlAnalyzeResultWriter> _logger;

        public string Format => WellKnownFormats.Html;

        public HtmlAnalyzeResultWriter(
            Lazy<IOutputResultWriterProvider> provider,
            ILogger<HtmlAnalyzeResultWriter> logger)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sarifWriter = new(() =>
            {
                if (provider.Value.TryGetWriter(WellKnownFormats.Sarif, out var result))
                {
                    return result;
                }

                return null;
            });
        }

        public async Task WriteAsync(IAsyncEnumerable<OutputResultDefinition> results, Stream stream, CancellationToken token)
        {
            if (results is null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var sarifContent = await GetSarifContentAsync(results, token).ConfigureAwait(false);

            if (sarifContent is null)
            {
                _logger.LogError("Failed to write HTML report. Required SARIF writer not found.");
                return;
            }

            using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
            await WriteHtmlAsync(CleanSarifContent(sarifContent), writer).ConfigureAwait(false);
        }

        private static async Task WriteHtmlAsync(string sarifContent, TextWriter writer)
        {
            using var assembly = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(HtmlAnalyzeResultWriter), "HtmlTemplate.html");
            using var templateString = new StreamReader(assembly);
            var template = await templateString.ReadToEndAsync().ConfigureAwait(false);
            var finishedTemplate = template.Replace("%SARIF_LOG%", sarifContent);

            await writer.WriteAsync(finishedTemplate).ConfigureAwait(false);
        }

        private async Task<string?> GetSarifContentAsync(IAsyncEnumerable<OutputResultDefinition> results, CancellationToken token)
        {
            var sarifWriter = _sarifWriter.Value;

            if (sarifWriter is null)
            {
                return null;
            }

            using var ms = new MemoryStream();
            await sarifWriter.WriteAsync(results, ms, token).ConfigureAwait(false);

            ms.Position = 0;

            using var reader = new StreamReader(ms);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        private static string CleanSarifContent(string sarifContent)
        {
            var sb = new StringBuilder(sarifContent);

            sb.Replace("\r\n", string.Empty);
            sb.Replace(@"\", string.Empty);
            sb.Replace("file:///C:/", string.Empty);

            return sb.ToString();
        }
    }
}
