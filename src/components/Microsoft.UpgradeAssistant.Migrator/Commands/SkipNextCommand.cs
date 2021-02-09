using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant.Migrator.Commands
{
    public class SkipNextCommand : MigrationCommand
    {
        private readonly MigrationStep _step;

        public SkipNextCommand(MigrationStep step)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
        }

        // todo - support localization
        public override string CommandText => $"Skip next step ({_step.Title})";

        public override async Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            return await _step.SkipAsync(token).ConfigureAwait(false);
        }
    }
}
