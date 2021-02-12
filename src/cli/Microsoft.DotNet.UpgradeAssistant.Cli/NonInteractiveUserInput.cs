using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal class NonInteractiveUserInput : ICollectUserInput
    {
        public Task<string?> AskUserAsync(string currentPath)
        {
            throw new NotImplementedException("User input cannot be selected in non-interactive mode");
        }

        public Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : MigrationCommand
            => Task.FromResult(commands.First(c => c.IsEnabled));
    }
}
