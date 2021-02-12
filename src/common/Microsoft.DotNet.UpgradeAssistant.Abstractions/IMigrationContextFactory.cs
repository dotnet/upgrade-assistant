using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IMigrationContextFactory
    {
        ValueTask<IMigrationContext> CreateContext(CancellationToken token);
    }
}
