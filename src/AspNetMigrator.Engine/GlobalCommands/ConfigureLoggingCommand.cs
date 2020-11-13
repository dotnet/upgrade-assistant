using System;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class ConfigureLoggingCommand : MigrationCommand
    {
        // todo - support localization
        public override string CommandText => "Configure logging";

        public override Task<bool> ExecuteAsync()
        {
            return Task.FromResult(false);
        }
    }
}
