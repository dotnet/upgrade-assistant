using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface ICollectUserInput
    {
        Task<string?> AskUserAsync(string currentPath);

        Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : MigrationCommand;

        Task<bool> WaitToProceedAsync(CancellationToken token);
    }
}
