// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Backup
{
    public class BackupStep : UpgradeStep
    {
        private const string FlagFileName = "upgrade.backup";
        private const string BackupPropertyName = "BackupLocation";

        private readonly bool _skipBackup;
        private readonly IUserInput _userInput;

        private string? _projectDir;
        private string? _defaultBackupPath;

        public override string Description => $"Back up the current project to another directory";

        public override string Title => "Back up project";

        public override string Id => WellKnownStepIds.BackupStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // The user should select a specific project before backing up (since changes are only made at the project-level)
            WellKnownStepIds.CurrentProjectSelectionStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public BackupStep(UpgradeOptions options, ILogger<BackupStep> logger, IUserInput userInput)
            : base(logger)
        {
            _skipBackup = options?.SkipBackup ?? throw new ArgumentNullException(nameof(options));
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
        }

        // The backup step backs up at the project level, so it doesn't apply if no project is selected
        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _projectDir = context.CurrentProject.Required().FileInfo.DirectoryName;
            _defaultBackupPath = GetDefaultBackupPath(_projectDir);

            var backupLocation = context.TryGetPropertyValue(BackupPropertyName) ?? _defaultBackupPath;

            if (_skipBackup)
            {
                Logger.LogDebug("Backup upgrade step initalized as complete (backup skipped)");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, "Backup skipped", BuildBreakRisk.None));
            }
            else if (_defaultBackupPath is null)
            {
                Logger.LogDebug("No backup path specified");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Backup step cannot be applied without a backup location", BuildBreakRisk.None));
            }
            else if (File.Exists(Path.Combine(backupLocation, FlagFileName)))
            {
                Logger.LogDebug("Backup upgrade step initalized as complete (already done)");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Existing backup found", BuildBreakRisk.None));
            }
            else
            {
                Logger.LogDebug("Backup upgrade step initialized as incomplete");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"No existing backup found. Applying this step will copy the contents of {_projectDir} (including subfolders) to another folder.", BuildBreakRisk.None));
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_skipBackup)
            {
                Logger.LogInformation("Skipping backup");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Skipped, "Backup skipped");
            }

            var backupPath = await ChooseBackupPath(context, token).ConfigureAwait(false);

            if (backupPath is null)
            {
                Logger.LogDebug("No backup path specified");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Backup step cannot be applied without a backup location");
            }

            if (_projectDir is null)
            {
                Logger.LogDebug("No project specified");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Backup step cannot be applied without a valid project selected");
            }

            if (Status == UpgradeStepStatus.Complete)
            {
                Logger.LogInformation("Backup already exists at {BackupPath}; nothing to do", backupPath);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Existing backup found");
            }

            context.SetPropertyValue(BackupPropertyName, backupPath, true);

            Logger.LogInformation("Backing up {ProjectDir} to {BackupPath}", _projectDir, backupPath);
            try
            {
                Directory.CreateDirectory(backupPath);
                if (!Directory.Exists(backupPath))
                {
                    Logger.LogError("Failed to create backup directory ({BackupPath})", backupPath);
                    return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Failed to create backup directory {backupPath}");
                }

                await CopyDirectoryAsync(_projectDir, backupPath).ConfigureAwait(false);
                var completedTime = DateTimeOffset.UtcNow;
                File.WriteAllText(Path.Combine(backupPath, FlagFileName), $"Backup created at {completedTime.ToUnixTimeSeconds()} ({completedTime})");
                Logger.LogInformation("Project backed up to {BackupPath}", backupPath);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Backup completed successfully");
            }
            catch (IOException exc)
            {
                Logger.LogError(exc, "Unexpected exception while creating backup");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Unexpected exception while creating backup");
            }
        }

        public override UpgradeStepInitializeResult Reset()
        {
            _defaultBackupPath = null;
            return base.Reset();
        }

        private async Task<string?> ChooseBackupPath(IUpgradeContext context, CancellationToken token)
        {
            var customPath = default(string);
            var commands = new[]
            {
                UpgradeCommand.Create($"Use default path [{_defaultBackupPath}]"),
                UpgradeCommand.Create("Enter custom path", async (ctx, token) =>
                {
                    customPath = await _userInput.AskUserAsync("Please enter a custom path for backups:").ConfigureAwait(false);
                    return !string.IsNullOrEmpty(customPath);
                })
            };

            while (!token.IsCancellationRequested)
            {
                var result = await _userInput.ChooseAsync("Please choose a backup path", commands, token).ConfigureAwait(false);

                if (await result.ExecuteAsync(context, token).ConfigureAwait(false))
                {
                    // customPath may be set in the lambda above.
#pragma warning disable CA1508 // Avoid dead conditional code
                    return customPath ?? _defaultBackupPath;
#pragma warning restore CA1508 // Avoid dead conditional code
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

            var candidateBasePath = $"{projectDir.TrimEnd('\\', '/')}.backup";
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
