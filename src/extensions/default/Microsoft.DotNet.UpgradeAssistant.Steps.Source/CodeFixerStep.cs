// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    /// <summary>
    /// Upgrade step that addresses a particular upgrade diagnostic with a Roslyn code fixer.
    /// Meant to be used as a sub-step of SourceUpdaterStep.
    /// </summary>
    public class CodeFixerStep : UpgradeStep
    {
        // This stores a reference to the parent source updater step, which will have all the diagnostics for the project.
        private readonly SourceUpdaterStep _sourceUpdater;
        private readonly CodeFixProvider _fixProvider;

        private IEnumerable<Diagnostic> Diagnostics => _sourceUpdater.Diagnostics.Where(d => _fixProvider.FixableDiagnosticIds.Contains(d.Id, StringComparer.Ordinal));

        public string DiagnosticId =>
            _fixProvider.FixableDiagnosticIds.Length switch
            {
                0 => "N/A",
                1 => _fixProvider.FixableDiagnosticIds.First(),
                _ => $"[{string.Join(", ", _fixProvider.FixableDiagnosticIds)}]"
            };

        public override string Id => $"{typeof(CodeFixerStep).FullName!}:{DiagnosticId}";

        public override string Description => $"Update source files to automatically fix upgrade analyzer {DiagnosticId}";

        public override string Title { get; }

        public CodeFixerStep(SourceUpdaterStep parentStep, IEnumerable<DiagnosticDescriptor> diagnostics, CodeFixProvider fixProvider, ILogger logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _fixProvider = fixProvider ?? throw new ArgumentNullException(nameof(fixProvider));
            _sourceUpdater = parentStep ?? throw new ArgumentNullException(nameof(parentStep)); // The parent step has the compilation/diagnostics
            ParentStep = parentStep;

            // Get titles for all the diagnostics this step can fix
            var diagnosticTitles = _fixProvider.FixableDiagnosticIds.Select(i => diagnostics.FirstOrDefault(d => d.Id.Equals(i, StringComparison.Ordinal))?.Title).Where(t => t != null);
            Title = $"Apply fix for {DiagnosticId}{(diagnosticTitles is null ? string.Empty : ": " + string.Join(", ", diagnosticTitles))}";
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // Code updates don't apply until a project is selected
            if (context?.CurrentProject is null)
            {
                return Task.FromResult(false);
            }

            // Check the code fix provider for an [ApplicableComponents] attribute
            // If one exists, the step only applies if the project has the indicated components
            return context.CurrentProject.IsApplicableAsync(_fixProvider, token).AsTask();
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            Logger.LogDebug("Identified {DiagnosticCount} fixable {DiagnosticId} diagnostics", Diagnostics.Count(), DiagnosticId);

            // This upgrade step is incomplete if any diagnostics it can fix remain
            return Diagnostics.Any() ?
                Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{Diagnostics.Count()} {DiagnosticId} diagnostics need fixed", BuildBreakRisk.Low)) :
                Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, string.Empty, BuildBreakRisk.None));
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_sourceUpdater.Project is null)
            {
                return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, "No project available.");
            }

            // Regenerating diagnostics is slow for large projects, but is necessary in between fixing multiple diagnostics
            // in a single file. To try and minimize the number of time diagnostics are gathered, fix one diagnostic each
            // from multiple files before regenerating diagnostics.
            var diagnosticCount = Diagnostics.Count();
            while (diagnosticCount > 0)
            {
                // Iterate through the first diagnostic from each document
                foreach (var diagnostic in Diagnostics.GroupBy(d => d.Location.SourceTree?.FilePath).Select(g => g.First()))
                {
                    var doc = _sourceUpdater.Project.GetRoslynProject().GetDocument(diagnostic.Location.SourceTree)!;
                    var updatedSolution = await TryFixDiagnosticAsync(diagnostic, doc, token).ConfigureAwait(false);

                    if (updatedSolution is null)
                    {
                        var description = $"Failed to fix diagnostic {diagnostic.Id} in {doc.FilePath}";
                        AddResultToContext(context, diagnostic.Id, doc.FilePath ?? string.Empty, UpgradeStepStatus.Failed, description);
                        Logger.LogError(description);
                        return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, description);
                    }
                    else if (!context.UpdateSolution(updatedSolution))
                    {
                        var description = $"Failed to apply changes after fixing {diagnostic.Id} to {doc.FilePath}";
                        AddResultToContext(context, diagnostic.Id, doc.FilePath ?? string.Empty, UpgradeStepStatus.Failed, description);
                        Logger.LogError(description);
                        return new UpgradeStepApplyResult(UpgradeStepStatus.Failed, description);
                    }
                    else
                    {
                        var description = $"Diagnostic {diagnostic.Id} fixed in {doc.FilePath}";
                        AddResultToContext(context, diagnostic.Id, doc.FilePath ?? string.Empty, UpgradeStepStatus.Complete, description);
                        Logger.LogInformation(description);
                    }
                }

                // Re-build and get an updated list of diagnostics
                await _sourceUpdater.RefreshDiagnosticsAsync(_sourceUpdater.Project, token).ConfigureAwait(false);

                // There should be less diagnostics for the given diagnostic ID after applying code fixes.
                // Confirm that that's true to guard against a project in a bad state or a bad analyzer/code fix provider
                // pair causing an infinite loop.
                var newDiagnosticCount = Diagnostics.Count();
                if (diagnosticCount == newDiagnosticCount)
                {
                    var description = $"Diagnostic {DiagnosticId} was not fixed as expected. This may be caused by the project being in a bad state (did NuGet packages restore correctly?) or by errors in analyzers or code fix providers related to this diagnostic.";
                    Logger.LogWarning(description);
                    AddResultToContext(context, DiagnosticId, string.Empty, UpgradeStepStatus.Failed, description);
                    break;
                }
                else
                {
                    diagnosticCount = newDiagnosticCount;
                }

                // Normally, the upgrader will apply steps one at a time
                // at the user's instruction. In the case of parent and child steps,
                // the parent has any top-level application done after the children.
                // In the case of this source update steps, the parent (this step's parent)
                // doesn't need to apply anything. Therefore, automatically apply the
                // paraent step if all diagnostics are addressed. This will avoid the annoying
                // user experience of having to "apply" an empty change for the parent
                // source updater step after all children have applied their changes.
                if (!_sourceUpdater.Diagnostics.Any())
                {
                    await _sourceUpdater.ApplyAsync(context, token).ConfigureAwait(false);
                }
            }

            Logger.LogDebug("All instances of {DiagnosticId} fixed", DiagnosticId);
            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, $"No instances of {DiagnosticId} need fixed");
        }

        private void AddResultToContext(IUpgradeContext context, string diagnosticId, string location, UpgradeStepStatus status, string resultMessage)
        {
            context.AddResult(Title, Description, location, diagnosticId, status, resultMessage);
        }

        private async Task<Solution?> TryFixDiagnosticAsync(Diagnostic diagnostic, Document document, CancellationToken token)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            CodeAction? fixAction = null;
            var context = new CodeFixContext(document, diagnostic, (action, _) => fixAction = action, token);
            await _fixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            // fixAction may not be null if the code fixer is applied.
#pragma warning disable CA1508 // Avoid dead conditional code
            if (fixAction is null)
#pragma warning restore CA1508 // Avoid dead conditional code
            {
                Logger.LogWarning("No code fix found for {DiagnosticId}", diagnostic.Id);
                return null;
            }

            var applyOperation = (await fixAction.GetOperationsAsync(token).ConfigureAwait(false))
                .OfType<ApplyChangesOperation>()
                .FirstOrDefault();

            if (applyOperation is null)
            {
                Logger.LogWarning("Code fix could not be applied for {DiagnosticId}", diagnostic.Id);
                return null;
            }

            return applyOperation.ChangedSolution;
        }
    }
}
