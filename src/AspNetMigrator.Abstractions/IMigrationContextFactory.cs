using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator
{
    public interface IMigrationContextFactory
    {
        ValueTask<IMigrationContext> CreateContext(CancellationToken token);
    }
}
