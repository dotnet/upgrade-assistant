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

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.CurrentProject is not null)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Current project is already selected.", BuildBreakRisk.None);
            }

            var projects = context.Projects.ToList();
            var completeChecks = projects.Select(async p => IsCompleted(context, p) || !await RunChecksAsync(p, token).ConfigureAwait(false));
            if ((await Task.WhenAll(completeChecks).ConfigureAwait(false)).All(b => b))
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No projects need upgrade", BuildBreakRisk.None);
            }

            IProject? newCurrentProject = null;

            if (projects.Count == 1)
            {
                newCurrentProject = projects[0];
                Logger.LogDebug("Setting only project in solution as the current project: {Project}", newCurrentProject.FilePath);
            }
            else if (!context.InputIsSolution)
            {
                // If the user has specified a particular project, only that should be the current project
                newCurrentProject = projects.Where(i => Path.GetFileName(i.FilePath).Equals(Path.GetFileName(context.InputPath), StringComparison.OrdinalIgnoreCase)).First();
                Logger.LogDebug("Setting user-selected project as the current project: {Project}", newCurrentProject.FilePath);
            }

            if (newCurrentProject is null)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "No project is currently selected.", BuildBreakRisk.None);
            }
            else
            {
                if (IsCompleted(context, newCurrentProject))
                {
                    Logger.LogDebug("Project {Project} does not need upgraded", newCurrentProject.FilePath);
                }
                else if (!(await RunChecksAsync(newCurrentProject, token).ConfigureAwait(false)))
                {
                    Logger.LogError("Unable to upgrade project {Project}", newCurrentProject.FilePath);
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

            if (context.EntryPoint is null)
            {
                throw new InvalidOperationException("Entrypoint must be set before using this step");
            }

            var orderedProjects = context.EntryPoint.PostOrderTraversal(p => p.ProjectReferences);

            // No need for an IAsyncEnumerable here since the commands shouldn't be displayed until
            // all are available anyhow.
            var commands = new List<ProjectCommand>();
            foreach (var project in orderedProjects)
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
            var success = true;

            foreach (var check in _checks)
            {
                Logger.LogTrace("Running readiness check {Id}", check.Id);

                success &= await check.IsReadyAsync(project, token).ConfigureAwait(false);
            }

            return success;
        }
    }
}
