using System;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    // todo - rearrange the usage of the command to remove the dependency on Migrator
    public class ApplyNextCommand : MigrationCommand
    {
        private readonly Migrator _migrator;

        public ApplyNextCommand(Migrator migrator)
        {
            _migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));
        }

        // todo - support localization
        public override string CommandText => $"Apply next step {(_migrator?.NextStep is null ? string.Empty : $" ({_migrator.NextStep.Title})")}";

        public override async Task<bool> ExecuteAsync()
        {
            return await _migrator.ApplyNextStepAsync().ConfigureAwait(false);
        }
    }
}
