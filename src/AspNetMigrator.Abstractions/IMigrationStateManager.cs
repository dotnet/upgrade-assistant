using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public interface IMigrationStateManager
    {
        Task LoadStateAsync(IMigrationContext context, CancellationToken token);

        Task SaveStateAsync(IMigrationContext context, CancellationToken token);
    }
}
