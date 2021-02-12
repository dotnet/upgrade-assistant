using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant
{
    public interface IMigrationContextFactory
    {
        ValueTask<IMigrationContext> CreateContext(CancellationToken token);
    }
}
