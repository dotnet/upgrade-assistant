﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Solution
{
    public class SolutionMigrationStep : MigrationStep
    {
        private readonly ICollectUserInput _input;
        private readonly ITargetFrameworkIdentifier _tfm;

        public override string Id => typeof(SolutionMigrationStep).FullName!;

        public override string Description => string.Empty;

        public override string Title => "Identify solution conversion order";

        public SolutionMigrationStep(
            ICollectUserInput input,
            ITargetFrameworkIdentifier tfm,
            ILogger<SolutionMigrationStep> logger)
            : base(logger)
        {
            _input = input;
            _tfm = tfm;
        }

        protected override bool IsApplicableImpl(IMigrationContext context) => context is not null && context.Project is null;

        // This migration step is meant to be run fresh every time a new project needs selected
        protected override bool ShouldReset(IMigrationContext context) => context?.Project is null;

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
            => Task.FromResult(InitializeImpl(context));

        private MigrationStepInitializeResult InitializeImpl(IMigrationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Project is null)
            {
                var projects = context.Projects.ToList();

                if (projects.All(IsCompleted))
                {
                    return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No projects need migrated", BuildBreakRisk.None);
                }

                if (projects.Count == 1)
                {
                    var onlyProject = projects[0];

                    Logger.LogInformation("Solution only contains one project ({Project}), setting it as the current project", onlyProject.FilePath);
                    context.Project = onlyProject;

                    return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Selected only project.", BuildBreakRisk.None);
                }

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
                context.Project = selectedProject;
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"Project {selectedProject.GetRoslynProject().Name} was selected.");
            }
        }

        private bool IsCompleted(IProject project)
        {
            if (project.GetRoslynProject().FilePath is string path)
            {
                using var projectFile = File.OpenRead(path);
                return _tfm.IsCoreCompatible(projectFile);
            }

            return false;
        }

        private async Task<IProject> GetProject(IMigrationContext context, Func<IProject, bool> isProjectCompleted, CancellationToken token)
        {
            const string EntrypointQuestion = "Please select the project you run. We will then analyze the dependencies and identify the recommended order to migrate projects.";
            const string SelectProjectQuestion = "Here is the recommended order to migrate. Select enter to follow this list, or input the project you want to start with.";

            var allProjects = context.Projects.OrderBy(p => p.GetRoslynProject().Name).Select(ProjectCommand.Create).ToList();
            var entrypoint = await _input.ChooseAsync(EntrypointQuestion, allProjects, token).ConfigureAwait(false);

            var ordered = entrypoint.Project.PostOrderTraversal(p => p.ProjectReferences).Select(CreateProjectCommand);

            var result = await _input.ChooseAsync(SelectProjectQuestion, ordered, token).ConfigureAwait(false);

            return result.Project;

            ProjectCommand CreateProjectCommand(IProject project)
            {
                return new ProjectCommand(project, isProjectCompleted(project));
            }
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
