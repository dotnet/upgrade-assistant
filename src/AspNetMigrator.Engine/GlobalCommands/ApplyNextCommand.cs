using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine.GlobalCommands
{
    // todo - rearrange the usage of the command to remove the dependency on Migrator
    public class ApplyNextCommand : MigrationCommand
    {
        private readonly Lazy<string> _stepName;

        public ApplyNextCommand(Lazy<string> stepName)
        {
            _stepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        }

        // todo - support localization
        public override string CommandText => $"Apply next step {{{_stepName.Value}}}";

        public override async Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            var migrator = context?.Migrator;

            if (migrator is null)
            {
                throw new ArgumentNullException(nameof(migrator));
            }

            return await migrator.ApplyNextStepAsync(context, token).ConfigureAwait(false);
        }
    }
}
