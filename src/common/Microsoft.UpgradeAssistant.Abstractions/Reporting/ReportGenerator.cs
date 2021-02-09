using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator;

namespace Microsoft.UpgradeAssistant.Reporting
{
    internal class ReportGenerator : IReportGenerator
    {
        private readonly IEnumerable<ISectionGenerator> _generators;

        public ReportGenerator(IEnumerable<ISectionGenerator> generators)
        {
            _generators = generators;
        }

        public async IAsyncEnumerable<Page> Generate(IMigrationContext response, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var project in response.Projects)
            {
                var sectionTasks = _generators
                    .Select(generator => generator.GenerateContentAsync(project, token));
                var sections = await Task.WhenAll(sectionTasks).ConfigureAwait(false);

                yield return new Page(Path.GetFileName(project.FilePath))
                {
                    Content = sections
                };
            }
        }
    }
}
