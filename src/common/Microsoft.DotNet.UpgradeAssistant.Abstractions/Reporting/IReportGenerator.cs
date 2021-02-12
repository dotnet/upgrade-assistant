using System.Collections.Generic;
using System.Threading;

namespace Microsoft.DotNet.UpgradeAssistant.Reporting
{
    public interface IReportGenerator
    {
        public IAsyncEnumerable<Page> Generate(IMigrationContext response, CancellationToken token);
    }
}
