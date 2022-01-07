// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    /// <summary>
    /// Upgrade step that updates NuGet package references
    /// to better work after upgrade. Packages references are
    /// updated if the reference appears to be transitive (with
    /// SDK style projects, only top-level dependencies are necessary
    /// in the project file), if the package version doesn't
    /// target a compatible .NET framework but a newer version does,
    /// or if the package is explicitly mapped to an updated
    /// NuGet package in a mapping configuration file.
    /// </summary>
    public class PackageUpdaterStep : UpgradeStep
    {
        private readonly IPackageRestorer _packageRestorer;
        private readonly IEnumerable<IDependencyAnalyzer> _packageAnalyzers;
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;

        private IEnumerable<UpgradeStep>? _subSteps;

        public override string Description => "Update package references to versions compatible with the target framework";

        public override string Title => "Update NuGet Packages";

        public override string Id => WellKnownStepIds.PackageUpdaterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            WellKnownStepIds.BackupStepId,

            // Project should be SDK-style before changing package references
            WellKnownStepIds.TryConvertProjectConverterStepId,

            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        public PackageUpdaterStep(
            IPackageRestorer packageRestorer,
            IEnumerable<IDependencyAnalyzer> packageAnalyzers,
            IDependencyAnalyzerRunner packageAnalyzer,
            ILogger<PackageUpdaterStep> logger)
            : base(logger)
        {
            _packageRestorer = packageRestorer ?? throw new ArgumentNullException(nameof(packageRestorer));
            _packageAnalyzers = packageAnalyzers ?? throw new ArgumentNullException(nameof(packageAnalyzers));
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

        public override IEnumerable<UpgradeStep> SubSteps => _subSteps ?? Enumerable.Empty<UpgradeStep>();

        private async ValueTask<IDependencyAnalysisState?> GetAnalysisState(IUpgradeContext context, CancellationToken token)
        {
            try
            {
                var currentProject = context.CurrentProject.Required();
                return await _packageAnalyzer.AnalyzeAsync(context, currentProject, currentProject.TargetFrameworks, token).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FileInfo);
                return null;
            }
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var analysis = await GetAnalysisState(context, token).ConfigureAwait(false);

            if (analysis is null || !analysis.IsValid)
            {
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, "Package analysis failed", BuildBreakRisk.Unknown);
            }

            if (!analysis.AreChangesRecommended)
            {
                Logger.LogInformation("No package updates needed");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No package updates needed", BuildBreakRisk.None);
            }

            var steps = new List<UpgradeStep>();

            AddSubsteps(analysis.References.Deletions, "Remove reference '{0}'", static t => t.Name, static (file, op) => file.RemoveReferences(new[] { op.Item }));
            AddSubsteps(analysis.Packages.Deletions, "Remove package '{0}'", static t => t.Name, static (file, op) => file.RemovePackages(new[] { op.Item }));
            AddSubsteps(analysis.Packages.Additions, "Add package '{0}'", static t => t.Name, static (file, op) => file.AddPackages(new[] { op.Item }));
            AddSubsteps(analysis.FrameworkReferences.Deletions, "Remove framework reference '{0}'", static t => t.Name, static (file, op) => file.RemoveFrameworkReferences(new[] { op.Item }));
            AddSubsteps(analysis.FrameworkReferences.Additions, "Add framework reference '{0}'", static t => t.Name, static (file, op) => file.AddFrameworkReferences(new[] { op.Item }));

            void AddSubsteps<T>(IEnumerable<Operation<T>> items, string messageFormat, Func<T, string> textFactory, Action<IProjectFile, Operation<T>> action)
            {
                foreach (var item in items)
                {
                    steps.Add(new PackageManipulationStep<T>(item, SR.Format(messageFormat, textFactory(item.Item)), action, Logger));
                }
            }

            _subSteps = steps;

            return new UpgradeStepInitializeResult(
                UpgradeStepStatus.Incomplete,
                $"{analysis.References.Deletions.Count} references need removed, {analysis.Packages.Deletions.Count} packages need removed, and {analysis.Packages.Additions.Count} packages need added", Risk: analysis.Risk);
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var projectFile = context.CurrentProject.Required().GetFile();

            await projectFile.SaveAsync(token).ConfigureAwait(false);

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Packages updated");
        }

        private class PackageManipulationStep<T> : UpgradeStep
        {
            private readonly Operation<T> _operation;
            private readonly Action<IProjectFile, Operation<T>> _action;

            public PackageManipulationStep(Operation<T> operation, string title, Action<IProjectFile, Operation<T>> action, ILogger logger)
                : base(logger)
            {
                _operation = operation;
                _action = action;

                Title = title;
            }

            public override string Title { get; }

            public override string Description => string.Join(";", _operation.OperationDetails.Details);

            protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
            {
                var file = context.CurrentProject.Required().GetFile();

                _action(file, _operation);

                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, Title));
            }

            protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
                => Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, Title, _operation.OperationDetails.Risk));

            protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(true);
        }
    }
}
