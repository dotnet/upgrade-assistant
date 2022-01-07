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
        private readonly IDependencyAnalyzerRunner _packageAnalyzer;

        private IDependencyAnalysisState? _analysisState;

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
            IDependencyAnalyzerRunner packageAnalyzer,
            ILogger<PackageUpdaterStep> logger)
            : base(logger)
        {
            _packageAnalyzer = packageAnalyzer ?? throw new ArgumentNullException(nameof(packageAnalyzer));
            _analysisState = null;
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var currentProject = context.CurrentProject.Required();
                _analysisState = await _packageAnalyzer.AnalyzeAsync(context, currentProject, currentProject.TargetFrameworks, token).ConfigureAwait(false);
                if (!_analysisState.IsValid)
                {
                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Package analysis failed", BuildBreakRisk.Unknown);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception exc)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogCritical(exc, "Unexpected exception analyzing package references for: {ProjectPath}", context.CurrentProject.Required().FileInfo);
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Failed, $"Unexpected exception analyzing package references for: {context.CurrentProject.Required().FileInfo}", BuildBreakRisk.Unknown);
            }

            if (_analysisState is null || !_analysisState.AreChangesRecommended)
            {
                Logger.LogInformation("No package updates needed");
                return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No package updates needed", BuildBreakRisk.None);
            }
            else
            {
                LogDetails("References to be removed: {References}", _analysisState.References.Deletions);
                LogDetails("References to be added: {References}", _analysisState.References.Additions);
                LogDetails("Packages to be removed: {Packages}", _analysisState.Packages.Deletions);
                LogDetails("Packages to be added: {Packages}", _analysisState.Packages.Additions);
                LogDetails("Framework references to be added: {FrameworkReference}", _analysisState.FrameworkReferences.Additions);
                LogDetails("Framework references to be removed: {FrameworkReference}", _analysisState.FrameworkReferences.Deletions);

                void LogDetails<T>(string name, IReadOnlyCollection<Operation<T>> collection)
                {
                    if (collection.Count > 0)
                    {
                        Logger.LogInformation(name, string.Join(Environment.NewLine, collection.Select(o => o.Item)));
                    }
                }

                return new UpgradeStepInitializeResult(
                    UpgradeStepStatus.Incomplete,
                    $"{_analysisState.References.Deletions.Count} references need removed, {_analysisState.Packages.Deletions.Count} packages need removed, and {_analysisState.Packages.Additions.Count} packages need added", Risk: _analysisState.Risk);
            }
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = context.CurrentProject.Required();

            var projectFile = project.GetFile();

            if (_analysisState is not null)
            {
                projectFile.RemoveReferences(_analysisState.References.Deletions.Select(i => i.Item));

                projectFile.RemovePackages(_analysisState.Packages.Deletions.Select(i => i.Item));
                projectFile.AddPackages(_analysisState.Packages.Additions.Select(i => i.Item));

                projectFile.RemoveFrameworkReferences(_analysisState.FrameworkReferences.Deletions.Select(i => i.Item));
                projectFile.AddFrameworkReferences(_analysisState.FrameworkReferences.Additions.Select(i => i.Item));

                await projectFile.SaveAsync(token).ConfigureAwait(false);
            }

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Packages updated");
        }
    }
}
