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

            var project = await context.GetProjectAsync(token).ConfigureAwait(false);

            if (project is null)
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
                context.SetProject(selectedProject);
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

            var allProjects = await context.GetProjects(token).OrderBy(p => p.GetRoslynProject().Name).Select(ProjectCommand.Create).ToListAsync(cancellationToken: token).ConfigureAwait(false);
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
            public static ProjectCommand Create(IProject project) => new ProjectCommand(project, false);

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
