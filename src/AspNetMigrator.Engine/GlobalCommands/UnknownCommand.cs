using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class UnknownCommand : MigrationCommand
    {
        // todo - support localization
        public override string CommandText => "Unknown command";

        public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            return Task.FromResult(true);
        }
    }
}
