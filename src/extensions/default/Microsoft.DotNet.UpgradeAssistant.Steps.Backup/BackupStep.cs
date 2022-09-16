// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Backup
{
    public class BackupStep : UpgradeStep
    {
        private const string FlagFileName = "upgrade.backup";
        private const string BaseBackupPropertyName = "BaseBackupLocation";

        private readonly bool _skipBackup;
        private readonly IUserInput _userInput;

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

        public BackupStep(
            IUserInput userInput,
            IOptions<BackupOptions> options,
            ILogger<BackupStep> logger)
            : base(logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _skipBackup = options.Value.Skip;
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

            var projectDir = GetProjectDir(context);
            var baseBackupLocation = context.Properties.GetPropertyValue(BaseBackupPropertyName) ?? GetDefaultBaseBackupPath(context.InputPath);
            var backupLocation = EnsureValidPath(context.InputIsSolution
                ? Path.Combine(baseBackupLocation, Path.GetFileName(projectDir))
                : baseBackupLocation);

            if (_skipBackup)
            {
                Logger.LogDebug("Backup upgrade step initalized as complete (backup skipped)");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, "Backup skipped", BuildBreakRisk.None));
            }
            else if (backupLocation is null)
            {
                Logger.LogDebug("No backup path specified");
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Backup step cannot be applied without a backup location", BuildBreakRisk.None));
            }
            else if (File.Exists(Path.Combine(backupLocation, FlagFileName)))
            {
                Logger.LogDebug("Backup upgrade step initalized as complete (already done). Backup is stored at {BackupLocation}", backupLocation);
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, $"Existing backup found at {backupLocation}", BuildBreakRisk.None));
            }
            else
            {
                Logger.LogDebug("Backup upgrade step initialized as incomplete; will backup to {BackupLocation}", backupLocation);
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"No existing backup found. Applying this step will copy the contents of {projectDir} (including subfolders) to {backupLocation}", BuildBreakRisk.None));
            }
        }

        private void AddResultToContext(IUpgradeContext context, string backupLocation, UpgradeStepStatus status, string description)
        {
            context.AddResultForStep(this, backupLocation, status, description);
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_skipBackup)
            {
                var description = "Skipping backup";
                Logger.LogInformation(description);
                AddResultToContext(context, string.Empty, UpgradeStepStatus.Skipped, description);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Skipped, "Backup skipped");
            }

            var baseBackupPath = await ChooseBackupPath(context, token).ConfigureAwait(false);

            if (baseBackupPath is null)
            {
                var description = "No backup path specified";
                Logger.LogDebug(description);
                AddResultToContext(context, baseBackupPath ?? string.Empty, UpgradeStepStatus.Failed, description);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "Backup step cannot be applied without a backup location");
            }

            var projectDir = GetProjectDir(context);

            var backupPath = EnsureValidPath(context.InputIsSolution
                ? Path.Combine(baseBackupPath, Path.GetFileName(projectDir))
                : baseBackupPath);

            context.Properties.SetPropertyValue(BaseBackupPropertyName, context.InputIsSolution ? baseBackupPath : backupPath, true);

            if (File.Exists(Path.Combine(backupPath, FlagFileName)))
            {
                var description = $"Backup already exists at {backupPath}; nothing to do";
                Logger.LogInformation(description);
                AddResultToContext(context, backupPath, UpgradeStepStatus.Complete, description);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Backup already exists at {backupPath}; nothing to do");
            }

            Logger.LogInformation("Backing up {ProjectDir} to {BackupPath}", projectDir, backupPath);
            try
            {
                Directory.CreateDirectory(backupPath);
                if (!Directory.Exists(backupPath))
                {
                    var failDescription = $"Failed to create backup directory ({backupPath})";
                    Logger.LogError(failDescription);
                    AddResultToContext(context, baseBackupPath, UpgradeStepStatus.Failed, failDescription);
                    return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Failed to create backup directory {backupPath}");
                }

                await CopyDirectoryAsync(projectDir, backupPath).ConfigureAwait(false);
                var completedTime = DateTimeOffset.UtcNow;
                File.WriteAllText(Path.Combine(backupPath, FlagFileName), $"Backup created at {completedTime.ToUnixTimeSeconds()} ({completedTime})");

                var description = $"Project backed up to {backupPath}";
                Logger.LogInformation(description);
                AddResultToContext(context, backupPath, UpgradeStepStatus.Complete, description);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Project backed up to {backupPath}");
            }
            catch (IOException exc)
            {
                Logger.LogError(exc, "Unexpected exception while creating backup");
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, $"Unexpected exception while creating backup");
            }
        }

        private async Task<string?> ChooseBackupPath(IUpgradeContext context, CancellationToken token)
        {
            var defaultPath = GetDefaultBaseBackupPath(context.InputPath);
            var customPath = default(string);

            var commands = new[]
            {
                UpgradeCommand.Create($"Use default path [{defaultPath}]"),
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
                    return customPath ?? defaultPath;
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

        private static string GetDefaultBaseBackupPath(string inputPath) => $"{Path.GetDirectoryName(inputPath).TrimEnd('\\', '/')}.backup";

        private string EnsureValidPath(string path)
        {
            var candidatePath = path;
            Logger.LogDebug("Determining backup path");
            var iteration = 0;
            while (!IsPathValid(candidatePath))
            {
                Logger.LogDebug("Unable to use backup path {CandidatePath}", candidatePath);
                candidatePath = $"{path}.{iteration++}";
            }

            Logger.LogDebug("Using backup path {BackupPath}", candidatePath);
            return candidatePath;
        }

        private static string GetProjectDir(IUpgradeContext context)
        {
            return context.CurrentProject.Required().FileInfo.DirectoryName;
        }

        private static bool IsPathValid(string candidatePath) => !Directory.Exists(candidatePath) || File.Exists(Path.Combine(candidatePath, FlagFileName));
    }
}
