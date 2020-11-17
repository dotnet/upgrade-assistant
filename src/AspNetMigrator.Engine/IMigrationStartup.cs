using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public interface IMigrationStartup
    {
        Task<bool> StartupAsync(CancellationToken token);
    }
}
