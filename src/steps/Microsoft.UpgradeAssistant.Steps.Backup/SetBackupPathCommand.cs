using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UpgradeAssistant;

namespace Microsoft.UpgradeAssistant.Steps.Backup
{
    public class SetBackupPathCommand : MigrationCommand
    {
        private readonly Func<string?> _getCurrentBackupPath;
        private readonly Func<string, Task<string?>> _collectUserInput;
        private readonly Action<string?> _setBackupPath;

        public SetBackupPathCommand(Func<string?> getCurrentBackupPath, Func<string, Task<string?>> collectUserInput, Action<string?> setBackupPath)
        {
            _collectUserInput = collectUserInput ?? throw new ArgumentNullException(nameof(collectUserInput));
            _getCurrentBackupPath = getCurrentBackupPath ?? throw new ArgumentNullException(nameof(getCurrentBackupPath));
            _setBackupPath = setBackupPath ?? throw new ArgumentNullException(nameof(setBackupPath));
        }

        // todo - support localization
        public override string CommandText => "Set path for backup";

        public async override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            var currentBackupPath = _getCurrentBackupPath();
            var prompt = $"Current backup path: {currentBackupPath ?? "<None>"}";
            var newBackupPath = await _collectUserInput(prompt).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(newBackupPath))
            {
                newBackupPath = currentBackupPath;
            }

            _setBackupPath(newBackupPath);

            return true;
        }
    }
}
