using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant
{
    public interface IMigrationStartup
    {
        Task<bool> StartupAsync(CancellationToken token);
    }
}
