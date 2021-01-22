using System.Collections.Generic;
using System.Threading;

namespace AspNetMigrator.Reporting
{
    public interface IPageGenerator
    {
        IAsyncEnumerable<Page> GeneratePages(IMigrationContext context, CancellationToken token);
    }
}
