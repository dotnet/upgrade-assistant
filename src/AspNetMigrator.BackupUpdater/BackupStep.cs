using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.BackupUpdater
{
    public class BackupStep : MigrationStep
    {
        private const string FlagFileName = "migration.backup";

        private readonly string _projectDir;
        private readonly bool _skipBackup;

        private string _backupPath;

        public BackupStep(MigrateOptions options, ILogger<BackupStep> logger, ICollectUserInput collectBackupPathFromUser)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _projectDir = Path.GetDirectoryName(options.ProjectPath)!;
            _skipBackup = options.SkipBackup;
            _backupPath = DetermineBackupPath(options);

            Title = $"Backup project";
            Description = $"Backup {_projectDir} to {_backupPath}";
            if (collectBackupPathFromUser is null)
            {
                throw new ArgumentNullException(nameof(collectBackupPathFromUser));
            }

            Commands.Insert(0, new SetBackupPathCommand(_backupPath, collectBackupPathFromUser.AskUserAsync, (string newPath) =>
            {
                _backupPath = newPath;
            }));
        }

        public string GetBackupPath()
        {
            return _backupPath;
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (_skipBackup)
            {
                Logger.LogDebug("Backup migration step initalized as complete (backup skipped)");
                return Task.FromResult((MigrationStepStatus.Skipped, "Backup skipped"));
            }
            else if (File.Exists(Path.Combine(_backupPath, FlagFileName)))
            {
                Logger.LogDebug("Backup migration step initalized as complete (already done)");
                return Task.FromResult((MigrationStepStatus.Complete, "Existing backup found"));
            }
            else
            {
                Logger.LogDebug("Backup migration step initialized as incomplete");
                return Task.FromResult((MigrationStepStatus.Incomplete, $"No existing backup found. Applying this step will copy the contents of {_projectDir} to {_backupPath} (including subfolders)."));
            }
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (_skipBackup)
            {
                Logger.LogInformation("Skipping backup");
                return (MigrationStepStatus.Complete, "Backup skipped");
            }

            if (Status == MigrationStepStatus.Complete)
            {
                Logger.LogInformation("Backup already exists at {BackupPath}; nothing to do", _backupPath);
                return (MigrationStepStatus.Complete, "Existing backup found");
            }

            Logger.LogInformation("Backing up {ProjectDir} to {BackupPath}", _projectDir, _backupPath);
            try
            {
                Directory.CreateDirectory(_backupPath);
                if (!Directory.Exists(_backupPath))
                {
                    Logger.LogError("Failed to create backup directory ({BackupPath})", _backupPath);
                    return (MigrationStepStatus.Failed, $"Failed to create backup directory {_backupPath}");
                }

                await CopyDirectoryAsync(_projectDir, _backupPath).ConfigureAwait(false);
                var completedTime = DateTimeOffset.UtcNow;
                await File.WriteAllTextAsync(Path.Combine(_backupPath, FlagFileName), $"Backup created at {completedTime.ToUnixTimeSeconds()} ({completedTime})", token).ConfigureAwait(false);
                Logger.LogInformation("Project backed up to {BackupPath}", _backupPath);
                return (MigrationStepStatus.Complete, "Backup completed successfully");
            }
            catch (IOException exc)
            {
                Logger.LogError("Unexpected exception while creating backup: {Exception}", exc);
                return (MigrationStepStatus.Failed, $"Unexpected exception while creating backup");
            }
        }

        private async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            var directoryInfo = new DirectoryInfo(sourceDir);
            foreach (var file in directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var dest = Path.Combine(destinationDir, file.Name);
                await CopyFileAsync(file.FullName, dest).ConfigureAwait(false);
                Logger.LogDebug("Copied {SourceFile} to {DestinationFile}", file.FullName, dest);
            }

            foreach (var dir in directoryInfo.GetDirectories())
            {
                await CopyDirectoryAsync(dir.FullName, Path.Combine(destinationDir, dir.Name)).ConfigureAwait(false);
            }
        }

        private static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 65536;
            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions);
            using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, fileOptions);
            await sourceStream.CopyToAsync(destinationStream, bufferSize, CancellationToken.None).ConfigureAwait(false);
        }

        private string DetermineBackupPath(MigrateOptions options)
        {
            Logger.LogDebug("Determining backup path");
            if (!string.IsNullOrWhiteSpace(options.BackupPath))
            {
                Logger.LogDebug("Using specified path: {BackupPath}", options.BackupPath);
                return options.BackupPath;
            }

            var candidateBasePath = $"{Path.TrimEndingDirectorySeparator(_projectDir)}.backup";
            var candidatePath = candidateBasePath;
            var iteration = 0;
            while (!IsPathValid(candidatePath))
            {
                Logger.LogDebug("Unable to use backup path {CandidatePath}", candidatePath);
                candidatePath = $"{candidateBasePath}.{iteration++}";
            }

            Logger.LogDebug("Using backup path {BackupPath}", candidatePath);
            return candidatePath;
        }

        private static bool IsPathValid(string candidatePath) => !Directory.Exists(candidatePath) || File.Exists(Path.Combine(candidatePath, FlagFileName));
    }
}
