// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    public class EntrypointSelectionStep : UpgradeStep
    {
        private readonly IPackageRestorer _restorer;
        private readonly IUserInput _userInput;
        private readonly UpgradeOptions _options;
        private readonly IEntrypointResolver _entrypointResolver;

        public EntrypointSelectionStep(
            UpgradeOptions options,
            IEntrypointResolver entrypointResolver,
            IPackageRestorer restorer,
            IUserInput userInput,
            ILogger<EntrypointSelectionStep> logger)
            : base(logger)
        {
            _options = options;
            _entrypointResolver = entrypointResolver ?? throw new ArgumentNullException(nameof(entrypointResolver));
            _restorer = restorer ?? throw new ArgumentNullException(nameof(restorer));
            _userInput = userInput ?? throw new ArgumentNullException(nameof(userInput));
        }

        public override string Title => "Select an entrypoint";

        public override string Description => "The entrypoint is the application you run or the library that is to be upgraded. Dependencies will then be analyzed and a recommended process will then be determined";

        public override string Id => WellKnownStepIds.EntrypointSelectionStepId;

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.EntryPoints.Any())
            {
                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Entrypoint already set.");
            }

            var selectedProject = await GetEntrypointAsync(context, token).ConfigureAwait(false);

            if (selectedProject is null)
            {
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "No project was selected.");
            }
            else
            {
                context.EntryPoints = new[] { selectedProject };
                await _restorer.RestorePackagesAsync(context, selectedProject, token).ConfigureAwait(false);

                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"Project {selectedProject.GetRoslynProject().Name} was selected.");
            }
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(InitializeImpl(context));

        protected UpgradeStepInitializeResult InitializeImpl(IUpgradeContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.EntryPoints.Any())
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Project is already selected.", BuildBreakRisk.None);
            }

            var projects = context.Projects.ToList();

            if (projects.Count == 1)
            {
                var project = projects[0];
                context.EntryPoints = new[] { project };

                Logger.LogInformation("Setting entrypoint to only project in solution: {Project}", project.FileInfo);

                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Selected only project.", BuildBreakRisk.None);
            }

            // If the user has specified a particular project, that project will be considered the entry point even if
            // other dependencies were loaded along with it.
            if (!context.InputIsSolution)
            {
                var project = projects.First(i => i.FileInfo.Name.Equals(Path.GetFileName(context.InputPath), StringComparison.OrdinalIgnoreCase));
                context.EntryPoints = new[] { project };

                Logger.LogInformation("Setting entrypoint to user selected project: {Project}", project.FileInfo);

                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Selected user's choice of entry point project.", BuildBreakRisk.None);
            }

            context.EntryPoints = _entrypointResolver.GetEntrypoints(context.Projects, _options.EntryPoint);

            if (context.EntryPoints.Any())
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Selected user's choice of entry point project.", BuildBreakRisk.None);
            }
            else if (!_userInput.IsInteractive)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "No entrypoint was selected. Solutions require an entrypoint to proceed.", BuildBreakRisk.None);
            }
            else
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "No entrypoint was selected. Solutions require an entrypoint to proceed.", BuildBreakRisk.None);
            }
        }

        private async ValueTask<IProject> GetEntrypointAsync(IUpgradeContext context, CancellationToken token)
        {
            const string EntrypointQuestion = "Please select the project you run. We will then analyze the dependencies and identify the recommended order to upgrade projects.";

            var allProjects = context.Projects.OrderBy(p => p.GetRoslynProject().Name).Select(ProjectCommand.Create).ToList();
            var result = await _userInput.ChooseAsync(EntrypointQuestion, allProjects, token).ConfigureAwait(false);

            return result.Project;
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(context is not null && !context.EntryPoints.Any() && !context.IsComplete);
    }
}
