using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public class SetBackupPathCommand : MigrationCommand
    {
        private readonly string _currentBackupPath;
        private readonly Func<string, Task<string?>> _collectUserInput;
        private readonly Action<string> _setBackupPath;

        public SetBackupPathCommand(string currentBackupPath, Func<string, Task<string?>> collectUserInput, Action<string> setBackupPath)
        {
            _collectUserInput = collectUserInput ?? throw new ArgumentNullException(nameof(collectUserInput));
            _currentBackupPath = currentBackupPath ?? throw new ArgumentNullException(nameof(currentBackupPath));
            _setBackupPath = setBackupPath ?? throw new ArgumentNullException(nameof(setBackupPath));
        }

        // todo - support localization
        public override string CommandText => "Set path for backup";

        public async override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
        {
            var prompt = $"Current backup path: {_currentBackupPath}";
            var newBackupPath = await _collectUserInput(prompt).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(newBackupPath))
            {
                newBackupPath = _currentBackupPath;
            }

            _setBackupPath(newBackupPath);

            return true;
        }
    }
}
