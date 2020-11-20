using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public interface ICollectUserInput
    {
        Task<string?> AskUserAsync(string currentPath);

        Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, T defaultResult, CancellationToken token)
            where T : MigrationCommand;
    }
}
