using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class ConfigureLoggingCommand : MigrationCommand
    {
        // todo - support localization
        public override string CommandText => "Configure logging";

        public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            return Task.FromResult(false);
        }
    }
}
