﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Solution
{
    public class SolutionMigrationStep : MigrationStep
    {
        private readonly ICollectUserInput _input;
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;
        private readonly ITargetTFMSelector _tfmSelector;

        public override string Id => typeof(SolutionMigrationStep).FullName!;

        public override string Description => string.Empty;

        public override string Title => "Choose project to upgrade";

        public SolutionMigrationStep(
            ICollectUserInput input,
            ITargetFrameworkMonikerComparer tfmComparer,
            ITargetTFMSelector tfmSelector,
            ILogger<SolutionMigrationStep> logger)
            : base(logger)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
            _tfmSelector = tfmSelector ?? throw new ArgumentNullException(nameof(tfmSelector));
        }

        protected override bool IsApplicableImpl(IMigrationContext context) => context is not null && context.Project is null;

        // This migration step is meant to be run fresh every time a new project needs selected
        protected override bool ShouldReset(IMigrationContext context) => context?.Project is null && Status == MigrationStepStatus.Complete;

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.EntryPoint is not null && context.Project is not null)
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Project is already selected.", BuildBreakRisk.None);
            }

            var projects = context.Projects.ToList();

            if (projects.All(p => IsCompleted(context, p)))
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No projects need migrated", BuildBreakRisk.None);
            }

            if (projects.Count == 1)
            {
                await context.SetEntryPointAsync(projects[0], token).ConfigureAwait(false);
                await context.SetProjectAsync(projects[0], token).ConfigureAwait(false);

                Logger.LogInformation("Solution only contains one project ({Project}), setting it as the current project and entrypoint.", context.Project!.FilePath);

                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Selected only project.", BuildBreakRisk.None);
            }

            if (context.EntryPoint is null)
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, "No entryproint is selected.", BuildBreakRisk.None);
            }

            if (context.Project is null)
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, "No project is currently selected.", BuildBreakRisk.None);
            }
            else
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Project is already selected.", BuildBreakRisk.None);
            }
        }

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var selectedProject = await GetProject(context, IsCompleted, token).ConfigureAwait(false);

            if (selectedProject is null)
            {
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, "No project was selected.");
            }
            else
            {
                await context.SetProjectAsync(selectedProject, token).ConfigureAwait(false);
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"Project {selectedProject.GetRoslynProject().Name} was selected.");
            }
        }

        private bool IsCompleted(IMigrationContext context, IProject project)
        {
            // If the entry point hasn't been selected yet, it's safe to consider projects complete
            // if they work against the latest current OS-specific TFM
            var entryPointTargetTFM = context.EntryPointTargetTFM
                ?? _tfmSelector.HighestPossibleTFM;

            return project.GetFile().IsSdk && _tfmComparer.IsCompatible(entryPointTargetTFM, project.TFM);
        }

        private async Task<IProject> GetProject(IMigrationContext context, Func<IMigrationContext, IProject, bool> isProjectCompleted, CancellationToken token)
        {
            const string SelectProjectQuestion = "Here is the recommended order to migrate. Select enter to follow this list, or input the project you want to start with.";

            await context.SetEntryPointAsync(await GetEntrypointAsync(context, token).ConfigureAwait(false), token).ConfigureAwait(false);
            var ordered = context.EntryPoint!.PostOrderTraversal(p => p.ProjectReferences).Select(CreateProjectCommand);

            var result = await _input.ChooseAsync(SelectProjectQuestion, ordered, token).ConfigureAwait(false);

            return result.Project;

            ProjectCommand CreateProjectCommand(IProject project)
            {
                return new ProjectCommand(project, isProjectCompleted(context, project));
            }
        }

        private async ValueTask<IProject> GetEntrypointAsync(IMigrationContext context, CancellationToken token)
        {
            const string EntrypointQuestion = "Please select the project you run. We will then analyze the dependencies and identify the recommended order to migrate projects.";

            if (context.EntryPoint is not null)
            {
                return context.EntryPoint;
            }

            var allProjects = context.Projects.OrderBy(p => p.GetRoslynProject().Name).Select(ProjectCommand.Create).ToList();
            var result = await _input.ChooseAsync(EntrypointQuestion, allProjects, token).ConfigureAwait(false);

            return result.Project;
        }

        private class ProjectCommand : MigrationCommand
        {
            public static ProjectCommand Create(IProject project) => new(project, false);

            public ProjectCommand(IProject project, bool isCompleted)
            {
                IsEnabled = !isCompleted;

                Project = project;
            }

            public override string CommandText => IsEnabled ? Project.GetRoslynProject().Name : $"[Completed] {Project.GetRoslynProject().Name}";

            public IProject Project { get; }

            public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
