// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration
{
    public class ConfigUpdaterStep : UpgradeStep
    {
        private readonly IEnumerable<ConfigUpdaterSubStep> _allSteps;

        private readonly string[] _configFilePaths;

        public ImmutableArray<ConfigFile> ConfigFiles { get; private set; }

        public override string Description => $"Update project based on settings in app config files ({string.Join(", ", _configFilePaths)})";

        public override string Title => "Upgrade app config files";

        public override string Id => WellKnownStepIds.ConfigUpdaterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing things based on config files
            WellKnownStepIds.BackupStepId,

            // Template files should be added prior to making config updates (since some IConfigUpdaters may change added templates)
            WellKnownStepIds.TemplateInserterStepId
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId
        };

        public ConfigUpdaterStep(IEnumerable<IUpdater<ConfigFile>> configUpdaters, IOptions<ConfigUpdaterOptions> configUpdaterOptions, ILogger<ConfigUpdaterStep> logger)
            : base(logger)
        {
            if (configUpdaters is null)
            {
                throw new ArgumentNullException(nameof(configUpdaters));
            }

            if (configUpdaterOptions is null)
            {
                throw new ArgumentNullException(nameof(configUpdaterOptions));
            }

            _configFilePaths = configUpdaterOptions.Value?.ConfigFilePaths ?? Array.Empty<string>();
            SubSteps = _allSteps = configUpdaters.Select(u => new ConfigUpdaterSubStep(this, u, logger)).ToList();
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            var result = context?.CurrentProject is not null &&
                SubSteps.Any() &&
                _configFilePaths.Select(p => Path.Combine(context.CurrentProject.FileInfo.DirectoryName, p)).Any(f => File.Exists(f));

            return Task.FromResult(result);
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            var configPaths = _configFilePaths.Select(p => Path.Combine(project.FileInfo.DirectoryName, p)).Where(p => File.Exists(p));
            Logger.LogDebug("Loading config files: {ConfigFiles}", string.Join(", ", configPaths));
            ConfigFiles = ImmutableArray.CreateRange(configPaths.Select(p => new ConfigFile(p)));
            Logger.LogDebug("Loaded {ConfigCount} config files", ConfigFiles.Length);

            await FilterSubStepsByIsApplicableAsync(context, token).ConfigureAwait(false);

            foreach (var step in SubSteps)
            {
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            var incompleteSubSteps = SubSteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No config updaters need applied", BuildBreakRisk.None)
                : new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} config updaters need applied", SubSteps.Where(s => !s.IsDone).Max(s => s.Risk));
        }

        private async Task FilterSubStepsByIsApplicableAsync(IUpgradeContext context, CancellationToken token)
        {
            var applicableSubSteps = new List<ConfigUpdaterSubStep>();
            foreach (var substep in _allSteps)
            {
                if (await substep.IsApplicableAsync(context, token).ConfigureAwait(false))
                {
                    applicableSubSteps.Add(substep);
                }
            }

            SubSteps = applicableSubSteps;
        }

        public override UpgradeStepInitializeResult Reset()
        {
            SubSteps = _allSteps;
            return base.Reset();
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // Nothing needs applied here because the actual upgrade changes are applied by the substeps
            // (which should apply before this step).
            var incompleteSubSteps = SubSteps.Count(s => !s.IsDone);

            return Task.FromResult(incompleteSubSteps == 0
                ? new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty)
                : new UpgradeStepApplyResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} config updaters need applied"));
        }
    }
}
