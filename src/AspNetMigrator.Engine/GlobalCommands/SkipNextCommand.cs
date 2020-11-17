using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class SkipNextCommand : MigrationCommand
    {
        // todo - support localization
        public override string CommandText => "Skip next step";

        public override async Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var migrator = context.Migrator;

            return migrator != null && !await migrator.SkipNextStepAsync(context, token).ConfigureAwait(false);
        }
    }
}
