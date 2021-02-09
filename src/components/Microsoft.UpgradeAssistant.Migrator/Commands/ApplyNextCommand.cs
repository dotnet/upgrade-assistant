using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UpgradeAssistant;

namespace Microsoft.UpgradeAssistant.Migrator.Commands
{
    public class ApplyNextCommand : MigrationCommand
    {
        private readonly MigrationStep _step;

        public ApplyNextCommand(MigrationStep step)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
        }

        // todo - support localization
        public override string CommandText => $"Apply next step ({_step.Title})";

        public override async Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            return await _step.ApplyAsync(context, token).ConfigureAwait(false);
        }
    }
}
