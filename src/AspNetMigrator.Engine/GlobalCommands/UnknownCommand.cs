using System;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class UnknownCommand : MigrationCommand
    {
        // todo - support localization
        public override string CommandText => "Unknown command";

        public override Task<bool> ExecuteAsync()
        {
            return Task.FromResult(true);
        }
    }
}
