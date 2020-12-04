using System;
using System.Collections.Generic;
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

        public SolutionMigrationStep(ICollectUserInput input, MigrateOptions options, ILogger<SolutionMigrationStep> logger)
            : base(options, logger)
        {
            _input = input;
            Title = "Identify solution conversion order";
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectId = await context.GetProjectIdAsync(token).ConfigureAwait(false);

            if (projectId is null)
            {
                return (MigrationStepStatus.Incomplete, "No project is currently selected.");
            }
            else
            {
                return (MigrationStepStatus.Complete, "Project is already selected.");
            }
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var ws = await context.GetWorkspaceAsync(token).ConfigureAwait(false);
            var selectedProject = await GetProject(ws, token).ConfigureAwait(false);

            if (selectedProject is null)
            {
                return (MigrationStepStatus.Failed, "No project was selected.");
            }
            else
            {
                await context.SetProjectAsync(selectedProject.Id, token).ConfigureAwait(false);
                return (MigrationStepStatus.Complete, $"Project {selectedProject.Name} was selected.");
            }
        }

        private static IEnumerable<Project> GetOrderedProjects(Project entrypoint)
        {
            var sln = entrypoint.Solution;

            return entrypoint.PostOrderTraversal(p =>
                p.ProjectReferences.Select(r => sln.GetProject(r.ProjectId)!));
        }

        private async Task<Project> GetProject(Workspace ws, CancellationToken token)
        {
            const string EntrypointQuestion = "Please select the project you run. We will then analyze the dependencies and identify the recommended order to migrate projects.";
            const string SelectProjectQuestion = "Here is the recommended order to migrate. Select enter to follow this list, or input the project you want to start with.";

            var allProjects = ws.CurrentSolution.Projects.OrderBy(p => p.Name).Select(ProjectCommand.Create);
            var entrypoint = await _input.ChooseAsync(EntrypointQuestion, allProjects, token).ConfigureAwait(false);

            var ordered = GetOrderedProjects(entrypoint.Project).Select(ProjectCommand.Create);

            var result = await _input.ChooseAsync(SelectProjectQuestion, ordered, token).ConfigureAwait(false);

            return result.Project;
        }

        private class ProjectCommand : MigrationCommand
        {
            public static ProjectCommand Create(Project project) => new(project);

            public ProjectCommand(Project project)
            {
                Project = project;
            }

            public override string CommandText => Project.Name;

            public Project Project { get; }

            public override Task<bool> ExecuteAsync(IMigrationContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
