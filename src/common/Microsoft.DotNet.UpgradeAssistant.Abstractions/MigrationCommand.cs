using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public abstract class MigrationCommand
    {
        /// <summary>
        /// A command that can be executed.
        /// </summary>
        public abstract Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token);

        /// <summary>
        /// Gets the text displayed to the user from the REPL (e.g. Set Backup Path).
        /// </summary>
        public abstract string CommandText { get; }

        public bool IsEnabled { get; init; } = true;

        public static MigrationCommand Create(string text, bool isEnabled = true)
            => Create(text, (_, __) => Task.FromResult(true), isEnabled);

        public static MigrationCommand Create(string text, Func<IMigrationContext, CancellationToken, Task<bool>> execute, bool isEnabled = true)
            => new DelegateMigrationCommand(text, execute) { IsEnabled = isEnabled };

        private class DelegateMigrationCommand : MigrationCommand
        {
            private readonly Func<IMigrationContext, CancellationToken, Task<bool>> _execute;

            public DelegateMigrationCommand(string text, Func<IMigrationContext, CancellationToken, Task<bool>> execute)
            {
                _execute = execute;
                CommandText = text;
            }

            public override string CommandText { get; }

            public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
                => _execute(context, token);
        }
    }
}
