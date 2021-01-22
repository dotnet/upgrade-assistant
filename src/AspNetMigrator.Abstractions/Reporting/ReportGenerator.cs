using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AspNetMigrator.Reporting
{
    internal class ReportGenerator : IReportGenerator
    {
        private readonly IEnumerable<IPageGenerator> _generators;

        public ReportGenerator(IEnumerable<IPageGenerator> generators)
        {
            _generators = generators;
        }

        public async IAsyncEnumerable<Page> Generate(IMigrationContext response, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var generator in _generators)
            {
                await foreach (var page in generator.GeneratePages(response, token))
                {
                    yield return page;
                }
            }
        }
    }
}
