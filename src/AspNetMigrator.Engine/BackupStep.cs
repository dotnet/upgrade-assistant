using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.Engine
{
    public class BackupStep : MigrationStep
    {
        private const string FlagFileName = "migration.backup";

        private readonly string _backupPath;
        private readonly string _projectDir;
        private readonly bool _skipBackup;

        public BackupStep(MigrateOptions options, ILogger logger)
            : base(options, logger)
        {
            _projectDir = Path.GetDirectoryName(options.ProjectPath);
            _skipBackup = options.SkipBackup;
            _backupPath = DetermineBackupPath(options);

            Title = $"Backup project{(_skipBackup ? " [skipped]" : string.Empty)}";
            Description = $"Backup {_projectDir} to {_backupPath}";
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync()
        {
            if (_skipBackup)
            {
                Logger.Verbose("Backup migration step initalized as complete (backup skipped)");
                return Task.FromResult((MigrationStepStatus.Complete, "Backup skipped"));
            }
            else if (File.Exists(Path.Combine(_backupPath, FlagFileName)))
            {
                Logger.Verbose("Backup migration step initalized as complete (already done)");
                return Task.FromResult((MigrationStepStatus.Complete, "Existing backup found"));
            }
            else
            {
                Logger.Verbose("Backup migration step initialized as incomplete");
                return Task.FromResult((MigrationStepStatus.Incomplete, $"No existing backup found. Applying this step will copy the contents of {_projectDir} to {_backupPath} (including subfolders)."));
            }
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync()
        {
            if (_skipBackup)
            {
                Logger.Information("Skipping backup");
                return (MigrationStepStatus.Complete, "Backup skipped");
            }

            if (Status == MigrationStepStatus.Complete)
            {
                Logger.Information("Backup already exists at {BackupPath}; nothing to do", _backupPath);
                return (MigrationStepStatus.Complete, "Existing backup found");
            }

            Logger.Information("Backing up {ProjectDir} to {BackupPath}", _projectDir, _backupPath);
            try
            {
                Directory.CreateDirectory(_backupPath);
                if (!Directory.Exists(_backupPath))
                {
                    Logger.Error("Failed to create backup directory ({BackupPath})", _backupPath);
                    return (MigrationStepStatus.Failed, $"Failed to create backup directory {_backupPath}");
                }

                await CopyDirectoryAsync(_projectDir, _backupPath).ConfigureAwait(false);
                var completedTime = DateTimeOffset.UtcNow;
                await File.WriteAllTextAsync(Path.Combine(_backupPath, FlagFileName), $"Backup created at {completedTime.ToUnixTimeSeconds()} ({completedTime})").ConfigureAwait(false);
                Logger.Information("Project backed up to {BackupPath}", _backupPath);
                return (MigrationStepStatus.Complete, "Backup completed successfully");
            }
            catch (IOException exc)
            {
                Logger.Error("Unexpected exception while creating backup: {Exception}", exc);
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
                Logger.Verbose("Copied {SourceFile} to {DestinationFile}", file.FullName, dest);
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
            Logger.Verbose("Determining backup path");
            if (!string.IsNullOrWhiteSpace(options.BackupPath))
            {
                Logger.Verbose("Using specified path: {BackupPath}", options.BackupPath);
                return options.BackupPath;
            }

            var candidateBasePath = $"{Path.TrimEndingDirectorySeparator(_projectDir)}.backup";
            var candidatePath = candidateBasePath;
            var iteration = 0;
            while (!IsPathValid(candidatePath))
            {
                Logger.Verbose("Unable to use backup path {CandidatePath}", candidatePath);
                candidatePath = $"{candidateBasePath}.{iteration++}";
            }

            Logger.Verbose("Using backup path {BackupPath}", candidatePath);
            return candidatePath;
        }

        private static bool IsPathValid(string candidatePath) => !Directory.Exists(candidatePath) || File.Exists(Path.Combine(candidatePath, FlagFileName));
    }
}
