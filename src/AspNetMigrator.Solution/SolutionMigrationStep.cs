using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Solution
{
    public class SolutionMigrationStep : MigrationStep
    {
        private readonly ICollectUserInput _input;
        private readonly ITargetFrameworkIdentifier _tfm;

        public SolutionMigrationStep(
            ICollectUserInput input,
            MigrateOptions options,
            ITargetFrameworkIdentifier tfm,
            ILogger<SolutionMigrationStep> logger)
            : base(options, logger)
        {
            _input = input;
            _tfm = tfm;
            Title = "Identify solution conversion order";
        }

        protected override async Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectId = await context.GetProjectIdAsync(token).ConfigureAwait(false);

            if (projectId is null)
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

            var ws = await context.GetWorkspaceAsync(token).ConfigureAwait(false);
            var selectedProject = await GetProject(ws, IsCompleted, token).ConfigureAwait(false);

            if (selectedProject is null)
            {
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, "No project was selected.");
            }
            else
            {
                await context.SetProjectAsync(selectedProject.Id, token).ConfigureAwait(false);
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"Project {selectedProject.Name} was selected.");
            }
        }

        private static IEnumerable<Project> GetOrderedProjects(Project entrypoint)
        {
            var sln = entrypoint.Solution;

            return entrypoint.PostOrderTraversal(p =>
                p.ProjectReferences.Select(r => sln.GetProject(r.ProjectId)!));
        }

        private bool IsCompleted(Project project)
        {
            if (project.FilePath is not null)
            {
                using var projectFile = File.OpenRead(project.FilePath);
                return _tfm.IsCoreCompatible(projectFile);
            }

            return false;
        }

        private async Task<Project> GetProject(Workspace ws, Func<Project, bool> isProjectCompleted, CancellationToken token)
        {
            const string EntrypointQuestion = "Please select the project you run. We will then analyze the dependencies and identify the recommended order to migrate projects.";
            const string SelectProjectQuestion = "Here is the recommended order to migrate. Select enter to follow this list, or input the project you want to start with.";

            var allProjects = ws.CurrentSolution.Projects.OrderBy(p => p.Name).Select(ProjectCommand.Create);
            var entrypoint = await _input.ChooseAsync(EntrypointQuestion, allProjects, token).ConfigureAwait(false);

            var ordered = GetOrderedProjects(entrypoint.Project).Select(CreateProjectCommand);

            var result = await _input.ChooseAsync(SelectProjectQuestion, ordered, token).ConfigureAwait(false);

            return result.Project;

            ProjectCommand CreateProjectCommand(Project project)
            {
                return new ProjectCommand(project, isProjectCompleted(project));
            }
        }

        private class ProjectCommand : MigrationCommand
        {
            public static ProjectCommand Create(Project project) => new ProjectCommand(project, false);

            public ProjectCommand(Project project, bool isCompleted)
            {
                IsEnabled = !isCompleted;

                Project = project;
            }

            public override string CommandText => IsEnabled ? Project.Name : $"[Completed] {Project.Name}";

            public Project Project { get; }

            public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
