using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public class EntrypointSelectionStep : MigrationStep
    {
        private readonly ICollectUserInput _userInput;

        public EntrypointSelectionStep(ICollectUserInput userInput, ILogger<EntrypointSelectionStep> logger)
            : base(logger)
        {
            _userInput = userInput;
        }

        public override string Id => typeof(EntrypointSelectionStep).FullName!;

        public override string Title => "Select an entrypoint";

        public override string Description => "The entrypoint is the application you run or the library that is to be upgraded. Dependencies will then be analyzed and a recommended process will then be determined";

        protected override async Task<MigrationStepApplyResult> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var selectedProject = await GetEntrypointAsync(context, token);

            if (selectedProject is null)
            {
                return new MigrationStepApplyResult(MigrationStepStatus.Failed, "No project was selected.");
            }
            else
            {
                context.SetEntryPoint(selectedProject);
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"Project {selectedProject.GetRoslynProject().Name} was selected.");
            }
        }

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
            => Task.FromResult(InitializeImpl(context));

        protected MigrationStepInitializeResult InitializeImpl(IMigrationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.EntryPoint is not null)
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Project is already selected.", BuildBreakRisk.None);
            }

            var projects = context.Projects.ToList();

            if (projects.Count == 1)
            {
                var project = projects[0];
                context.SetEntryPoint(project);

                Logger.LogInformation("Setting entrypoint to only project in solution: {Project}", project.FilePath);

                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Selected only project.", BuildBreakRisk.None);
            }

            return new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, "No entryproint is selected.", BuildBreakRisk.None);
        }

        private async ValueTask<IProject> GetEntrypointAsync(IMigrationContext context, CancellationToken token)
        {
            const string EntrypointQuestion = "Please select the project you run. We will then analyze the dependencies and identify the recommended order to migrate projects.";

            if (context.EntryPoint is not null)
            {
                return context.EntryPoint.Project;
            }

            var allProjects = context.Projects.OrderBy(p => p.GetRoslynProject().Name).Select(ProjectCommand.Create).ToList();
            var result = await _userInput.ChooseAsync(EntrypointQuestion, allProjects, token).ConfigureAwait(false);

            return result.Project;
        }

        protected override bool IsApplicableImpl(IMigrationContext context)
            => context.EntryPoint is null && !context.IsComplete;
    }
}
