// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling
{
    public sealed class CrawlerResults
    {
        public CrawlerResults(IReadOnlyDictionary<ApiKey, int> data)
        {
            Data = data;
        }

        public IReadOnlyDictionary<ApiKey, int> Data { get; }

        public async Task WriteGuidsAsync(string fileName)
        {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
            await using var writer = File.CreateText(fileName);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            await WriteGuidsAsync(writer).ConfigureAwait(false);
        }

        public async Task WriteGuidsAsync(TextWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            foreach (var key in Data.Keys.OrderBy(k => k))
            {
                await writer.WriteLineAsync(key.Id.ToString("N")).ConfigureAwait(false);
            }
        }

        public string GetGuidsText()
        {
            using var writer = new StringWriter();
            foreach (var key in Data.Keys.OrderBy(k => k))
            {
                writer.WriteLine(key.Id.ToString("N"));
            }

            return writer.ToString();
        }
    }
}
