// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private readonly List<DependencyAnalyzerStep> _subSteps;

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
            IEnumerable<IDependencyAnalyzer> dependencyAnalyzers,
            ILogger<PackageUpdaterStep> logger)
            : base(logger)
        {
            _subSteps = dependencyAnalyzers.OrderyByPrecedence().Select(d => new DependencyAnalyzerStep(this, d, logger)).ToList();
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is not null);

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (_subSteps.All(d => d.IsDone))
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "All analyzers have run", BuildBreakRisk.Low));
            }

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Some analyzers have  not run", BuildBreakRisk.Low));
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
            => Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "All analyzers have run"));

        public override IEnumerable<UpgradeStep> SubSteps => _subSteps;

        private class DependencyAnalyzerStep : AutoApplySubStep
        {
            private readonly IDependencyAnalyzer _analyzer;

            private IEnumerable<UpgradeStep>? _subSteps;
            private BuildBreakRisk _risk;

            public DependencyAnalyzerStep(
                PackageUpdaterStep parentStep,
                IDependencyAnalyzer analyzer,
                ILogger logger)
                : base(parentStep, logger)
            {
                if (logger is null)
                {
                    throw new ArgumentNullException(nameof(logger));
                }

                _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            }

            public override string Title => _analyzer.Name;

            public override string Description => _analyzer.Name;

            public override IEnumerable<UpgradeStep> SubSteps => _subSteps ?? Enumerable.Empty<UpgradeStep>();

            protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
            {
                if (context is null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (_subSteps is null)
                {
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

                    var additions = UpdatePackageAddition(analysis.Packages, context.CurrentProject.Required());
                    var steps = new List<UpgradeStep>();

                    AddSubsteps(analysis.References.Deletions, "Remove reference '{0}'", static t => t.Name, static (file, op) => file.RemoveReferences(new[] { op.Item }));
                    AddSubsteps(analysis.Packages.Deletions, "Remove package '{0}'", static t => t.Name, static (file, op) => file.RemovePackages(new[] { op.Item }));
                    AddSubsteps<NuGetReference>(additions, "Add package '{0}'", static t => t.Name, static (file, op) => file.AddPackages(new[] { op.Item }));
                    AddSubsteps(analysis.FrameworkReferences.Deletions, "Remove framework reference '{0}'", static t => t.Name, static (file, op) => file.RemoveFrameworkReferences(new[] { op.Item }));
                    AddSubsteps(analysis.FrameworkReferences.Additions, "Add framework reference '{0}'", static t => t.Name, static (file, op) => file.AddFrameworkReferences(new[] { op.Item }));

                    void AddSubsteps<T>(IEnumerable<Operation<T>> items, string messageFormat, Func<T, string> textFactory, Action<IProjectFile, Operation<T>> action)
                    {
                        foreach (var item in items)
                        {
                            steps.Add(new PackageManipulationStep<T>(this, item, SR.Format(messageFormat, textFactory(item.Item)), action, Logger));
                        }
                    }

                    _subSteps = steps;
                    _risk = analysis.Risk;
                }

                if (_subSteps.All(s => s.IsDone))
                {
                    return new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, $"{_analyzer.Name} has no recommended changes", BuildBreakRisk.None);
                }

                return new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{_analyzer.Name} has identified some recommended changes", Risk: _risk);
            }

            // For server-only projects, do not need to add System.ServiceModel packages.
            private static IEnumerable<Operation<NuGetReference>> UpdatePackageAddition(IDependencyCollection<NuGetReference> packages, IProject project)
            {
                var files = project.FindFiles(".cs", ProjectItemType.Compile);
                var containsService = false;
                foreach (var f in files)
                {
                    var root = CSharpSyntaxTree.ParseText(File.ReadAllText(f)).GetRoot();
                    if (ContainsIdentifier(root, "ChannelFactory") || ContainsIdentifier(root, "ClientBase"))
                    {
                        return packages.Additions;
                    }

                    if (!containsService && ContainsIdentifier(root, "ServiceHost"))
                    {
                        containsService = true;
                    }
                }

                if (containsService)
                {
                    return from p in packages.Additions
                            where !p.Item.Name.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase)
                            select p;
                }

                return packages.Additions;
            }

            // Checks if the root has descendant nodes that contains id
            private static bool ContainsIdentifier(SyntaxNode root, string id)
            {
                return root.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => n.Identifier.ValueText.IndexOf(id, StringComparison.Ordinal) >= 0);
            }

            public override UpgradeStepInitializeResult Reset()
            {
                _subSteps = default;
                _risk = default;

                return base.Reset();
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

            private async Task<IDependencyAnalysisState> GetAnalysisState(IUpgradeContext context, CancellationToken token)
            {
                var projectRoot = context.CurrentProject.Required();

                var analysisState = new DependencyAnalysisState(projectRoot, projectRoot.NuGetReferences, projectRoot.TargetFrameworks);

                Logger.LogDebug("Analyzing packages with {AnalyzerName}", _analyzer.Name);

                try
                {
                    await _analyzer.AnalyzeAsync(projectRoot, analysisState, token).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Logger.LogCritical("Package analysis failed (analyzer {AnalyzerName}: {Message}", _analyzer.Name, e.Message);
                    analysisState.IsValid = false;
                }

                return analysisState;
            }

            protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(true);
        }

        private class PackageManipulationStep<T> : AutoApplySubStep
        {
            private readonly Operation<T> _operation;
            private readonly Action<IProjectFile, Operation<T>> _action;

            public PackageManipulationStep(DependencyAnalyzerStep parentStep, Operation<T> operation, string title, Action<IProjectFile, Operation<T>> action, ILogger logger)
                : base(parentStep, logger)
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

                AddResultToContext(context);
                return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, Title));
            }

            private void AddResultToContext(IUpgradeContext context)
            {
                context.AddResultForStep(this, context.CurrentProject?.GetFile()?.FilePath ?? string.Empty, UpgradeStepStatus.Complete, Title);
            }

            protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
                => Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, Title, _operation.OperationDetails.Risk));

            protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(true);
        }
    }
}
