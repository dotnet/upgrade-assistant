// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public class CurrentProjectSelectionStep : UpgradeStep
    {
        private readonly IEnumerable<IUpgradeReadyCheck> _checks;
        private readonly IUserInput _input;
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;
        private readonly ITargetTFMSelector _tfmSelector;
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
            ITargetTFMSelector tfmSelector,
            ILogger<CurrentProjectSelectionStep> logger)
            : base(logger)
        {
            _checks = checks ?? throw new ArgumentNullException(nameof(checks));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
            _tfmSelector = tfmSelector ?? throw new ArgumentNullException(nameof(tfmSelector));
        }

        protected override bool IsApplicableImpl(IUpgradeContext context) => context is not null && context.CurrentProject is null && context.Projects.Any(p => !IsCompleted(context, p)) && !context.IsComplete;

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

            if (context.EntryPoint is null)
            {
                throw new InvalidOperationException("Entrypoint must be set before using this step");
            }

            // If a current project is selected, then this step is done
            if (context.CurrentProject is not null)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Current project is already selected.", BuildBreakRisk.None);
            }

            // Get the projects we care about based on the entry point project
            _orderedProjects = context.EntryPoint.PostOrderTraversal(p => p.ProjectReferences).ToArray();

            // If all projects related to the entry point project are complete or invalid, then the upgrade is done
            var completeChecks = _orderedProjects.Select(async p => IsCompleted(context, p) || !await RunChecksAsync(p, token).ConfigureAwait(false));
            if ((await Task.WhenAll(completeChecks).ConfigureAwait(false)).All(b => b))
            {
                Logger.LogInformation("No projects need upgraded for entry point {EntryPoint}", context.EntryPoint.FileInfo);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No projects need upgraded", BuildBreakRisk.None);
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
                if (IsCompleted(context, newCurrentProject))
                {
                    Logger.LogDebug("Project {Project} does not need upgraded", newCurrentProject.FileInfo);
                }
                else if (!(await RunChecksAsync(newCurrentProject, token).ConfigureAwait(false)))
                {
                    Logger.LogError("Unable to upgrade project {Project}", newCurrentProject.FileInfo);
                }
                else
                {
                    context.SetCurrentProject(newCurrentProject);
                }

                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, $"Project {newCurrentProject.GetRoslynProject().Name} was selected", BuildBreakRisk.None);
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var selectedProject = await GetProject(context, IsCompleted, token).ConfigureAwait(false);

            if (selectedProject is null)
            {
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "No project was selected.");
            }
            else
            {
                context.SetCurrentProject(selectedProject);
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Project {selectedProject.GetRoslynProject().Name} was selected.");
            }
        }

        // Consider a project completely upgraded if it is SDK-style and has a TFM equal to (or greater then) the expected one
        private bool IsCompleted(IUpgradeContext context, IProject project) =>
            project.GetFile().IsSdk && _tfmComparer.IsCompatible(project.TFM, _tfmSelector.SelectTFM(project));

        private async Task<IProject> GetProject(IUpgradeContext context, Func<IUpgradeContext, IProject, bool> isProjectCompleted, CancellationToken token)
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
                return new ProjectCommand(project, isProjectCompleted(context, project), await RunChecksAsync(project, token).ConfigureAwait(false));
            }
        }

        private async Task<bool> RunChecksAsync(IProject project, CancellationToken token)
        {
            foreach (var check in _checks)
            {
                Logger.LogTrace("Running readiness check {Id}", check.Id);

                if (!await check.IsReadyAsync(project, token).ConfigureAwait(false))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
