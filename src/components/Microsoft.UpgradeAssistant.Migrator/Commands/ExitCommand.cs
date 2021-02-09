using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant.Migrator.Commands
{
    public class ExitCommand : MigrationCommand
    {
        private readonly Action _stopTheProgram;

        public ExitCommand(Action stopTheProgram)
        {
            _stopTheProgram = stopTheProgram ?? throw new ArgumentNullException(nameof(stopTheProgram));
        }

        // todo - support localization
        public override string CommandText => "Exit";

        public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            _stopTheProgram();
            return Task.FromResult(true);
        }
    }
}
