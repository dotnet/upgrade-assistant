using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal class NonInteractiveUserInput : IUserInput
    {
        private readonly TimeSpan _waitPeriod;

        public NonInteractiveUserInput(MigrateOptions options)
        {
            _waitPeriod = TimeSpan.FromSeconds(options.NonInteractiveWait);
        }

        public Task<string?> AskUserAsync(string currentPath)
        {
            throw new NotImplementedException("User input cannot be selected in non-interactive mode");
        }

        public Task<T> ChooseAsync<T>(string message, IEnumerable<T> commands, CancellationToken token)
            where T : MigrationCommand
            => Task.FromResult(commands.First(c => c.IsEnabled));

        public async Task<bool> WaitToProceedAsync(CancellationToken token)
        {
            await Task.Delay(_waitPeriod, token);

            return true;
        }
    }
}
