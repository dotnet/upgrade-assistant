using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Reporting;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Portability
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
            var sections = await GenerateSections(project.GetRoslynProject(), token).ConfigureAwait(false);

            return new Section("Portability Analysis")
            {
                Content = sections
            };
        }

        private async ValueTask<List<Section>> GenerateSections(Project project, CancellationToken token)
        {
            List<Section> sections = new List<Section>();
            var content = await GenerateContent(project, token).ToListAsync(token).ConfigureAwait(false);
            var groups = content.GroupBy(r => r.Component);
            foreach (var group in groups)
            {
                var groupKey = group.Key;
                Section section = new Section("Component: " + groupKey);
                List<Row> rowList = new List<Row>();
                Table table = new Table
                {
                    Headers = new[] { "Type", "Name", "Description" },
                };

                foreach (var groupedItem in group)
                {
                    rowList.Add(new Row(new object[] { groupedItem.Type, groupedItem.Name, groupedItem.Description }));
                }

                table.Rows = rowList;
                section.Content = new[] { table };
                sections.Add(section);
            }

            return sections;
        }

        private async IAsyncEnumerable<PortabilityResult> GenerateContent(Project project, [EnumeratorCancellation] CancellationToken token)
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
                    yield return result;
                }
            }
        }
    }
}
