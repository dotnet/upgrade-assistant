// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    internal class UpgradeResultWriter : IUpgradeResultWriter
    {
        private Dictionary<string, IAnalyzeResultWriter> _formatToResultWriterMap;

        private IList<(Stream Stream, string Format)> _writeDestinations;

        private IAnalyzeResultWriterProvider _resultWriterProvider;

        public UpgradeResultWriter(IAnalyzeResultWriterProvider analyzeResultWriterProvider)
        {
            _resultWriterProvider = analyzeResultWriterProvider ?? throw new ArgumentNullException(nameof(analyzeResultWriterProvider));
            _formatToResultWriterMap = new Dictionary<string, IAnalyzeResultWriter>();
            _writeDestinations = new List<(Stream Stream, string Format)>();
        }

        public void AddWriteDestination(Stream stream, string format)
        {
            _writeDestinations.Add((stream, format));
            if (!_formatToResultWriterMap.ContainsKey(format))
            {
                if (!_resultWriterProvider.TryGetWriter(format, out var writer))
                {
                    throw new ArgumentException($"{format} is not a valid format for {_resultWriterProvider.GetType()}");
                }

                _formatToResultWriterMap.Add(format, writer);
            }
        }

        public async Task WriteAsync(IAsyncEnumerable<AnalyzeResultDefinition> results, CancellationToken token)
        {
            foreach (var destination in _writeDestinations)
            {
                await _formatToResultWriterMap[destination.Format].WriteAsync(results, destination.Stream, token);
            }
        }
    }
}
