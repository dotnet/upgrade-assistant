using System;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    public class SkipNextCommand : MigrationCommand
    {
        private readonly Migrator _migrator;

        public SkipNextCommand(Migrator migrator)
        {
            _migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));
        }

        // todo - support localization
        public override string CommandText => "Skip next step";

        public override async Task<bool> ExecuteAsync()
        {
            return _migrator != null && !await _migrator.SkipNextStepAsync().ConfigureAwait(false);
        }
    }
}
