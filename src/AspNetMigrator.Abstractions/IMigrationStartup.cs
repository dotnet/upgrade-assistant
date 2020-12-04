using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public interface IMigrationStartup
    {
        Task<bool> StartupAsync(CancellationToken token);
    }
}
