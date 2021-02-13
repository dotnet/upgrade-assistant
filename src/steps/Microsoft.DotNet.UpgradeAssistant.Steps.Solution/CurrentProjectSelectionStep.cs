using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public class CurrentProjectSelectionStep : MigrationStep
    {
        private readonly IUserInput _input;
        private readonly ITargetFrameworkMonikerComparer _tfmComparer;
        private readonly ITargetTFMSelector _tfmSelector;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.EntrypointSelectionStep"
        };

        public override string Id => typeof(CurrentProjectSelectionStep).FullName!;

        public override string Description => string.Empty;

        public override string Title => "Select project to upgrade";

        public CurrentProjectSelectionStep(
            IUserInput input,
            ITargetFrameworkMonikerComparer tfmComparer,
            ITargetTFMSelector tfmSelector,
            ILogger<CurrentProjectSelectionStep> logger)
            : base(logger)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _tfmComparer = tfmComparer ?? throw new ArgumentNullException(nameof(tfmComparer));
            _tfmSelector = tfmSelector ?? throw new ArgumentNullException(nameof(tfmSelector));
        }

        protected override bool IsApplicableImpl(IMigrationContext context) => context.CurrentProject is null && context.Projects.Any(p => !IsCompleted(context, p));

        // This migration step is meant to be run fresh every time a new project needs selected
        protected override bool ShouldReset(IMigrationContext context) => context?.CurrentProject is null && Status == MigrationStepStatus.Complete;

        protected override Task<MigrationStepInitializeResult> InitializeImplAsync(IMigrationContext context, CancellationToken token)
            => Task.FromResult(InitializeImpl(context));

        private MigrationStepInitializeResult InitializeImpl(IMigrationContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.CurrentProject is not null)
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Current project is already selected.", BuildBreakRisk.None);
            }

            var projects = context.Projects.ToList();

            if (projects.All(p => IsCompleted(context, p)))
            {
                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "No projects need migrated", BuildBreakRisk.None);
            }

            if (projects.Count == 1)
            {
                var project = projects[0];
                context.SetCurrentProject(project);

                Logger.LogInformation("Setting only project in solution as the current project: {Project}", project.FilePath);

                return new MigrationStepInitializeResult(MigrationStepStatus.Complete, "Selected only project.", BuildBreakRisk.None);
            }

            return new MigrationStepInitializeResult(MigrationStepStatus.Incomplete, "No project is currently selected.", BuildBreakRisk.None);
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
                context.SetCurrentProject(selectedProject);
                return new MigrationStepApplyResult(MigrationStepStatus.Complete, $"Project {selectedProject.GetRoslynProject().Name} was selected.");
            }
        }

        // Consider a project completely upgraded if it is SDK-style and has a TFM equal to (or greater then) the expected one
        private bool IsCompleted(IMigrationContext context, IProject project) =>
            project.GetFile().IsSdk && _tfmComparer.IsCompatible(project.TFM, _tfmSelector.SelectTFM(project));

        private async Task<IProject> GetProject(IMigrationContext context, Func<IMigrationContext, IProject, bool> isProjectCompleted, CancellationToken token)
        {
            const string SelectProjectQuestion = "Here is the recommended order to migrate. Select enter to follow this list, or input the project you want to start with.";

            if (context.EntryPoint is null)
            {
                throw new InvalidOperationException("Entrypoint must be set before using this step");
            }

            var ordered = context.EntryPoint.Project.PostOrderTraversal(p => p.ProjectReferences).Select(CreateProjectCommand);

            var result = await _input.ChooseAsync(SelectProjectQuestion, ordered, token).ConfigureAwait(false);

            return result.Project;

            ProjectCommand CreateProjectCommand(IProject project)
            {
                return new ProjectCommand(project, isProjectCompleted(context, project));
            }
        }
    }
}
