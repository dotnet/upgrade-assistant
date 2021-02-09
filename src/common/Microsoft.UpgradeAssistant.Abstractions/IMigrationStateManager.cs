using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant
{
    public interface IMigrationStateManager
    {
        Task LoadStateAsync(IMigrationContext context, CancellationToken token);

        Task SaveStateAsync(IMigrationContext context, CancellationToken token);
    }
}
