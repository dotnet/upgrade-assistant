using System.Collections.Generic;
using System.Threading;
using AspNetMigrator;

namespace Microsoft.UpgradeAssistant.Reporting
{
    public interface IReportGenerator
    {
        public IAsyncEnumerable<Page> Generate(IMigrationContext response, CancellationToken token);
    }
}
