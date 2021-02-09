using System.Collections.Generic;
using System.Threading;

namespace Microsoft.UpgradeAssistant.Reporting
{
    public interface IReportGenerator
    {
        public IAsyncEnumerable<Page> Generate(IMigrationContext response, CancellationToken token);
    }
}
