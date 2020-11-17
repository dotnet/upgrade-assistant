using System;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
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

        public override Task<bool> ExecuteAsync(Migrator migrator)
        {
            _stopTheProgram();
            return Task.FromResult(true);
        }
    }
}
