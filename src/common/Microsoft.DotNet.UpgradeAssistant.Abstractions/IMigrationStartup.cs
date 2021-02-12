using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IMigrationStartup
    {
        Task<bool> StartupAsync(CancellationToken token);
    }
}
