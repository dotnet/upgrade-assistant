using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Reporting;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Portability
{
    internal class PortabilityAnalysis : IPageGenerator
    {
        private readonly IEnumerable<IPortabilityAnalyzer> _analyzers;
        private readonly ILogger<PortabilityAnalysis> _logger;

        public PortabilityAnalysis(IEnumerable<IPortabilityAnalyzer> analyzers, ILogger<PortabilityAnalysis> logger)
        {
            _analyzers = analyzers;
            _logger = logger;
        }

        public async IAsyncEnumerable<Page> GeneratePages(IMigrationContext context, [EnumeratorCancellation] CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            yield return new Page("Portability analysis")
            {
                Content = await GenerateContent(context, token).ToListAsync(token).ConfigureAwait(false)
            };
        }

        private async IAsyncEnumerable<Content> GenerateContent(IMigrationContext context, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var p in context.Projects)
            {
                var table = await GenerateTable(p.GetRoslynProject(), token).ConfigureAwait(false);

                yield return new Section(Path.GetFileName(p.FilePath))
                {
                    Content = new[] { table }
                };
            }
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
