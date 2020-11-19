using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Solution
{
    public class SolutionMigrationStep : MigrationStep
    {
        private readonly ICollectUserInput _input;

        public SolutionMigrationStep(MigrateOptions options, ILogger<SolutionMigrationStep> logger, ICollectUserInput input)
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

        private async Task<Project?> GetProject(Workspace ws, CancellationToken token)
        {
            const string EntrypointQuestion = "Please select the project you run. We will then analyze the dependencies and identify the recommended order to migrate projects.";
            const string SelectProjectQuestion = "Here is the recommended order to migrate. Select enter to follow this list, or input the project you want to start with.";

            var entrypoint = await RunQuestionAsync(EntrypointQuestion, ws.CurrentSolution.Projects, token).ConfigureAwait(false);

            if (entrypoint is null)
            {
                return null;
            }

            var ordered = GetOrderedProjects(entrypoint);

            return await RunQuestionAsync(SelectProjectQuestion, ordered, token).ConfigureAwait(false);
        }

        private async Task<Project?> RunQuestionAsync(string question, IEnumerable<Project> projects, CancellationToken token)
        {
            var (text, map) = GenerateQuestion(question, projects);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var input = await _input.AskUserAsync(text).ConfigureAwait(false);

                if (input is null)
                {
                    return null;
                }

                if (map.TryGetValue(input, out var result))
                {
                    return result;
                }

                Logger.LogError("Could not find project with '{ProjectName}'", input);
            }
        }

        private static (string Text, Dictionary<string, Project> Map) GenerateQuestion(string text, IEnumerable<Project> projects)
        {
            var sb = new StringBuilder();
            var dict = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);

            sb.AppendLine("Please select the project you run. We will then analyze the dependencies and identify the recommended order to migrate projects.");

            foreach (var project in projects)
            {
                sb.AppendLine($"- {project.Name}");
                dict.Add(project.Name, project);
            }

            return (sb.ToString(), dict);
        }
    }
}
