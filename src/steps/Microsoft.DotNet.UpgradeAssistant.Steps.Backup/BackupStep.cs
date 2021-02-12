using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Backup
{
    public class BackupStep : MigrationStep
    {
        private const string FlagFileName = "migration.backup";

        private readonly bool _skipBackup;
        private readonly ICollectUserInput _userInput;

        private string? _projectDir;
        private string? _backupPath;

        public override string Id => typeof(BackupStep).FullName!;

        public override string Description => $"Backup the current project to another directory";

        public override string Title => "Backup project";

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // The user should select a specific project before backing up (since changes are only made at the project-level)
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.CurrentProjectSelectionStep",
        };

        public BackupStep(MigrateOptions options, ILogger<BackupStep> logger, ICollectUserInput userInput)
            : base(logger)
        {
            _skipBackup = options?.SkipBackup ?? throw new ArgumentNullException(nameof(options));
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
        }

        // The backup step backs up at the project level, so it doesn't apply if no project is selected
        protected override bool IsApplicableImpl(IMigrationContext context) => context?.CurrentProject is not null;

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _projectDir = context.CurrentProject.Required().Project.Directory;
            _backupPath ??= GetDefaultBackupPath(_projectDir);

            if (_skipBackup)
            {
                Logger.LogDebug("Backup migration step initalized as complete (backup skipped)");
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Skipped, "Backup skipped", BuildBreakRisk.None));
            }
            else if (_backupPath is null)
            {
                Logger.LogDebug("No backup path specified");
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Failed, "Backup step cannot be applied without a backup location", BuildBreakRisk.None));
            }
            else if (File.Exists(Path.Combine(_backupPath, FlagFileName)))
            {
                Logger.LogDebug("Backup migration step initalized as complete (already done)");
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Existing backup found", BuildBreakRisk.None));
            }
            else
            {
                Logger.LogDebug("Backup migration step initialized as incomplete");
                return Task.FromResult(new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"No existing backup found. Applying this step will copy the contents of {_projectDir} (including subfolders) to another folder.", BuildBreakRisk.None));
            }
        }

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (_skipBackup)
            {
                Logger.LogInformation("Skipping backup");
                return new MigrationStepApplyResult(MigrationStepStatus.Skipped, "Backup skipped");
            }

            var backupPath = await ChooseBackupPath(context, token);

            if (backupPath is null)
            {
                Logger.LogDebug("No backup path specified");
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, "Backup step cannot be applied without a backup location");
            }

            if (_projectDir is null)
            {
                Logger.LogDebug("No project specified");
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, "Backup step cannot be applied without a valid project selected");
            }

            if (Status == MigrationStepStatus.Complete)
            {
                Logger.LogInformation("Backup already exists at {BackupPath}; nothing to do", backupPath);
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, "Existing backup found");
            }

            Logger.LogInformation("Backing up {ProjectDir} to {BackupPath}", _projectDir, backupPath);
            try
            {
                Directory.CreateDirectory(backupPath);
                if (!Directory.Exists(backupPath))
                {
                    Logger.LogError("Failed to create backup directory ({BackupPath})", backupPath);
                    return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Failed to create backup directory {backupPath}");
                }

                await CopyDirectoryAsync(_projectDir, backupPath).ConfigureAwait(false);
                var completedTime = DateTimeOffset.UtcNow;
                await File.WriteAllTextAsync(Path.Combine(backupPath, FlagFileName), $"Backup created at {completedTime.ToUnixTimeSeconds()} ({completedTime})", token).ConfigureAwait(false);
                Logger.LogInformation("Project backed up to {BackupPath}", backupPath);
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, "Backup completed successfully");
            }
            catch (IOException exc)
            {
                Logger.LogError("Unexpected exception while creating backup: {Exception}", exc);
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, $"Unexpected exception while creating backup");
            }
        }

        public override MigrationStepInitializeResult Reset()
        {
            _backupPath = null;
            return base.Reset();
        }

        private async Task<string?> ChooseBackupPath(IMigrationContext context, CancellationToken token)
        {
            var customPath = default(string);
            var commands = new[]
            {
                MigrationCommand.Create($"Use default path [{_backupPath}]"),
                MigrationCommand.Create("Enter custom path", async (ctx, token) =>
                {
                    customPath = await _userInput.AskUserAsync("Please enter a custom path for backups:");
                    return !string.IsNullOrEmpty(customPath);
                })
            };

            while (!token.IsCancellationRequested)
            {
                var result = await _userInput.ChooseAsync("Please choose a backup path", commands, token);

                if (await result.ExecuteAsync(context, token))
                {
                    return customPath ?? _backupPath;
                }
            }

            return null;
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

        private async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            try
            {
                var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
                var bufferSize = 65536;
                using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions);
                using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, fileOptions);
                await sourceStream.CopyToAsync(destinationStream, bufferSize, CancellationToken.None).ConfigureAwait(false);
            }
            catch (IOException e)
            {
                Logger.LogWarning("Could not copy file {Path} due to '{Message}'", sourceFile, e.Message);
            }
        }

        private string GetDefaultBackupPath(string projectDir)
        {
            Logger.LogDebug("Determining backup path");

            var candidateBasePath = $"{Path.TrimEndingDirectorySeparator(projectDir)}.backup";
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
