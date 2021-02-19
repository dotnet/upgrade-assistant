// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Reporting;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Portability
{
    internal class PortabilityAnalysis : ISectionGenerator
    {
        private readonly IEnumerable<IPortabilityAnalyzer> _analyzers;
        private readonly ILogger<PortabilityAnalysis> _logger;

        public PortabilityAnalysis(IEnumerable<IPortabilityAnalyzer> analyzers, ILogger<PortabilityAnalysis> logger)
        {
            _analyzers = analyzers;
            _logger = logger;
        }

        public async Task<Section> GenerateContentAsync(IProject project, CancellationToken token)
        {
            var table = await GenerateTable(project.GetRoslynProject(), token).ConfigureAwait(false);

            return new Section("Portability Analysis")
            {
                Content = new[] { table }
            };
        }

        private async ValueTask<Table> GenerateTable(Project project, CancellationToken token)
        {
            var rows = await GenerateContent(project, token).ToListAsync(token).ConfigureAwait(false);

            return new Table
            {
                Headers = new[] { "Type", "Name", "Description" },
                Rows = rows,
            };
        }

        private async IAsyncEnumerable<Row> GenerateContent(Project project, [EnumeratorCancellation] CancellationToken token)
        {
            var compilation = await project.GetCompilationAsync(token).ConfigureAwait(false);

            if (compilation is null)
            {
                _logger.LogWarning("Could not compile {Project}", project.Name);
                yield break;
            }

            foreach (var analyzer in _analyzers)
            {
                await foreach (var result in analyzer.Analyze(compilation, token))
                {
                    yield return new Row(new object[] { result.Type, result.Name, result.Description });
                }
            }
        }
    }
}
