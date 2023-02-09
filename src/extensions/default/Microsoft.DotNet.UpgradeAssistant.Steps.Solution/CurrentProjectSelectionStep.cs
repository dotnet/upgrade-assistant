// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public class CurrentProjectSelectionStep : UpgradeStep
    {
        private readonly IEnumerable<IUpgradeReadyCheck> _checks;
        private readonly IUserInput _input;
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;
        private readonly ITargetFrameworkSelector _tfmSelector;
        private readonly IOptions<UpgradeReadinessOptions> _upgradeOptions;
        private IProject[]? _orderedProjects;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.EntrypointSelectionStepId,
        };

        public override string Description => string.Empty;

        public override string Title => "Select project to upgrade";

        public override string Id => WellKnownStepIds.CurrentProjectSelectionStepId;

        public CurrentProjectSelectionStep(
            IEnumerable<IUpgradeReadyCheck> checks,
            IUserInput input,
            ITargetFrameworkMonikerComparer tfmComparer,
            ITargetFrameworkSelector tfmSelector,
            IOptions<UpgradeReadinessOptions> upgradeOptions,
            ILogger<CurrentProjectSelectionStep> logger)
            : base(logger)
        {
            _checks = checks ?? throw new ArgumentNullException(nameof(checks));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
            _tfmSelector = tfmSelector ?? throw new ArgumentNullException(nameof(tfmSelector));
            _upgradeOptions = upgradeOptions ?? throw new ArgumentNullException(nameof(upgradeOptions));

            if (_upgradeOptions.Value is null)
            {
                throw new ArgumentException("Missing UpgradeReadinessOptions");
            }
        }

        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null || context.CurrentProject is not null || context.IsComplete)
            {
                return false;
            }

            return await context.Projects.ToAsyncEnumerable()
                .AnyAwaitAsync(async p => !await IsCompletedAsync(context, p, token).ConfigureAwait(false), token).ConfigureAwait(false);
        }

        // This upgrade step is meant to be run fresh every time a new project needs selected
        protected override bool ShouldReset(IUpgradeContext context) => context?.CurrentProject is null && Status == UpgradeStepStatus.Complete;

        public override UpgradeStepInitializeResult Reset()
        {
            _orderedProjects = null;
            return base.Reset();
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.EntryPoints.Any())
            {
                throw new InvalidOperationException("Entrypoint must be set before using this step");
            }

            // If a current project is selected, then this step is done
            if (context.CurrentProject is not null)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Current project is already selected.", BuildBreakRisk.None);
            }

            // Get the projects we care about based on the entry point project
            _orderedProjects = context.EntryPoints.PostOrderTraversal(p => p.ProjectReferences).ToArray();

            // If all projects related to the entry point project are complete or invalid, then the upgrade is done
            var allProjectsAreUpgraded = await _orderedProjects.ToAsyncEnumerable()
                .SelectAwait(async p => await IsCompletedAsync(context, p, token).ConfigureAwait(false) || !await RunChecksAsync(p, token).ConfigureAwait(false))
                .AllAsync(b => b, token).ConfigureAwait(false);

            if (allProjectsAreUpgraded)
            {
                Logger.LogInformation("No projects need to be upgraded for selected entrypoint");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No projects need to be upgraded", BuildBreakRisk.None);
            }

            // If no project is selected, and at least one still needs upgraded, identify the next project to upgrade
            IProject? newCurrentProject = null;
            if (_orderedProjects.Length == 1)
            {
                // If there is only one project, it is the current project
                newCurrentProject = _orderedProjects[0];
                Logger.LogDebug("Setting only project in solution as the current project: {Project}", newCurrentProject.FileInfo);
            }
            else if (!context.InputIsSolution)
            {
                // If the user has specified a particular project, only that should be the current project
                newCurrentProject = _orderedProjects.Where(i => i.FileInfo.Name.Equals(Path.GetFileName(context.InputPath), StringComparison.OrdinalIgnoreCase)).First();
                Logger.LogDebug("Setting user-selected project as the current project: {Project}", newCurrentProject.FileInfo);
            }

            // If no current project has been found, then this step is incomplete
            if (newCurrentProject is null)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "No project is currently selected.", BuildBreakRisk.None);
            }
            else
            {
                if (await IsCompletedAsync(context, newCurrentProject, token).ConfigureAwait(false))
                {
                    Logger.LogDebug("Project {Project} does not need to be upgraded", newCurrentProject.FileInfo);
                }
                else if (!(await RunChecksAsync(newCurrentProject, token).ConfigureAwait(false)))
                {
                    Logger.LogError("Unable to upgrade project {Project}", newCurrentProject.FileInfo);
                }
                else
                {
                    context.SetCurrentProject(newCurrentProject);
                }

                Logger.LogInformation("Project {Name} was selected", newCurrentProject.GetRoslynProject().Name);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, $"Project {newCurrentProject.GetRoslynProject().Name} was selected", BuildBreakRisk.None);
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var selectedProject = await GetProjectAsync(context, token).ConfigureAwait(false);

            if (selectedProject is null)
            {
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "No project was selected.");
            }
            else
            {
                context.SetCurrentProject(selectedProject);
                Logger.LogInformation("Project {Name} was selected", selectedProject.GetRoslynProject().Name);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Project {selectedProject.GetRoslynProject().Name} was selected.");
            }
        }

        // Consider a project completely upgraded if it is SDK-style and has a TFM equal to (or greater then) the expected one
        private async ValueTask<bool> IsCompletedAsync(IUpgradeContext context, IProject project, CancellationToken token)
        {
            if (!project.GetFile().IsSdk)
            {
                return false;
            }

            var expectedTfm = await _tfmSelector.SelectTargetFrameworkAsync(project, token).ConfigureAwait(false);

            return project.TargetFrameworks.Any(tfm => _tfmComparer.IsCompatible(tfm, expectedTfm));
        }

        private async Task<IProject> GetProjectAsync(IUpgradeContext context, CancellationToken token)
        {
            const string SelectProjectQuestion = "Here is the recommended order to upgrade. Select enter to follow this list, or input the project you want to start with.";

            if (_orderedProjects is null)
            {
                throw new UpgradeException("Project selection step must be initialized before it is applied (null _orderedProjects)");
            }

            // No need for an IAsyncEnumerable here since the commands shouldn't be displayed until
            // all are available anyhow.
            var commands = new List<ProjectCommand>();
            foreach (var project in _orderedProjects)
            {
                commands.Add(await CreateProjectCommandAsync(project).ConfigureAwait(false));
            }

            var result = await _input.ChooseAsync(SelectProjectQuestion, commands, token).ConfigureAwait(false);

            return result.Project;

            async Task<ProjectCommand> CreateProjectCommandAsync(IProject project)
            {
                var projectCompleted = await IsCompletedAsync(context, project, token).ConfigureAwait(false);
                var checks = await RunChecksAsync(project, token).ConfigureAwait(false);

                return new ProjectCommand(project, projectCompleted, checks);
            }
        }

        private async Task<bool> RunChecksAsync(IProject project, CancellationToken token)
        {
            var upgradeGuidanceMessages = new List<string>();
            foreach (var check in _checks)
            {
                Logger.LogTrace("Running readiness check {Id}", check.Id);

                var readiness = await check.IsReadyAsync(project, _upgradeOptions.Value, token).ConfigureAwait(false);
                if (readiness == UpgradeReadiness.NotReady)
                {
                    return false;
                }
                else if (!_upgradeOptions.Value.IgnoreUnsupportedFeatures && readiness == UpgradeReadiness.Unsupported)
                {
                    // an unsupported area has been detected. Capture a message to explain how to proceed.
                    upgradeGuidanceMessages.Add(check.UpgradeMessage);
                }
            }

            if (upgradeGuidanceMessages.Any())
            {
                // NOTE: the hidden assumption is that the UpgradeGuidance messages that were collected will be printed
                // to console by the IUpgradeReadyCheck. We make these messages accessible by property so that they can be integrated into a UI in the future
                // but assume that the IUpgradeReadyCheck displays them via logging because semantic logging provides color highlighting that we want to preserve.
                Logger.LogError("Project {Name} uses feature(s) that are not supported. If you would like upgrade-assistant to continue anyways please use the \"--ignore-unsupported-features\" option.", project.FileInfo);

                // user has been informed about how to proceed.
                // This project is not ready until the option '--ignore-unsupported-features' is provided.
                return false;
            }

            return true;
        }
    }
}
