using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.UpgradeAssistant.Cli.Commands
{
    public class SeeMoreDetailsCommand : MigrationCommand
    {
        private readonly MigrationStep _step;
        private readonly Func<MigrationStep, Task> _showDetails;

        public SeeMoreDetailsCommand(MigrationStep step, Func<MigrationStep, Task> showDetails)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
            _showDetails = showDetails ?? throw new ArgumentNullException(nameof(showDetails));
        }

        // todo - support localization
        public override string CommandText => "See more step details";

        public override async Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            await _showDetails(_step).ConfigureAwait(false);
            return true;
        }
    }
}
