using System;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class SkipNextCommand : MigrationCommand
    {
        // todo - support localization
        public override string CommandText => "Skip next step";

        public override async Task<bool> ExecuteAsync(Migrator migrator)
        {
            if (migrator is null)
            {
                throw new ArgumentNullException(nameof(migrator));
            }

            return migrator != null && !await migrator.SkipNextStepAsync().ConfigureAwait(false);
        }
    }
}
