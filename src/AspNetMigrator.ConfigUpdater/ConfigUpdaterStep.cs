﻿using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.ConfigUpdater
{
    public class ConfigUpdaterStep : MigrationStep
    {
        private readonly string[] _configFilePaths;

        public ImmutableArray<ConfigFile> ConfigFiles { get; private set; }

        public ConfigUpdaterStep(MigrateOptions options, ConfigUpdaterProvider configUpdaterProvider, ILogger<ConfigUpdaterStep> logger)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (configUpdaterProvider is null)
            {
                throw new ArgumentNullException(nameof(configUpdaterProvider));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _configFilePaths = configUpdaterProvider.ConfigFilePaths.ToArray();

            Title = "Migrate app config files";
            Description = $"Update project based on settings in app config files ({string.Join(", ", _configFilePaths)})";

            SubSteps = configUpdaterProvider.GetUpdaters().Select(u => new ConfigUpdaterSubStep(this, u, options, logger)).ToList();
        }

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.Project.Required();

            var configPaths = _configFilePaths.Select(p => Path.Combine(project.Directory ?? string.Empty, p)).Where(p => File.Exists(p));
            Logger.LogDebug("Loading config files: {ConfigFiles}", string.Join(", ", configPaths));
            ConfigFiles = ImmutableArray.CreateRange(configPaths.Select(p => new ConfigFile(p)));
            Logger.LogDebug("Loaded {ConfigCount} config files", ConfigFiles.Length);

            foreach (var step in SubSteps)
            {
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            var incompleteSubSteps = SubSteps.Count(s => !s.IsComplete);

            return incompleteSubSteps == 0
                ? new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No config updaters need applied", BuildBreakRisk.None)
                : new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, $"{incompleteSubSteps} config updaters need applied", SubSteps.Where(s => !s.IsComplete).Max(s => s.Risk));
        }

        protected override Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            // Nothing needs applied here because the actual migration changes are applied by the substeps
            // (which should apply before this step).
            var incompleteSubSteps = SubSteps.Count(s => !s.IsComplete);

            return Task.FromResult(incompleteSubSteps == 0
                ? new MigrationStepApplyResult(MigrationStepStatus.Complete, string.Empty)
                : new MigrationStepApplyResult(MigrationStepStatus.Incomplete, $"{incompleteSubSteps} config updaters need applied"));
        }
    }
}
