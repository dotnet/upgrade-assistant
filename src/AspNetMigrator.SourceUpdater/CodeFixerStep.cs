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

namespace AspNetMigrator.SourceUpdater
{
    /// <summary>
    /// Migration step that addresses a particular migration diagnostic with a Roslyn code fixer.
    /// Meant to be used as a sub-step of SourceUpdaterStep.
    /// </summary>
    public class CodeFixerStep : MigrationStep
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

        public CodeFixerStep(MigrationStep parentStep, IEnumerable<DiagnosticDescriptor> diagnostics, CodeFixProvider fixProvider, MigrateOptions options, ILogger logger)
            : base(options, logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _fixProvider = fixProvider ?? throw new ArgumentNullException(nameof(fixProvider));
            _sourceUpdater = (ParentStep = parentStep) as SourceUpdaterStep ?? throw new ArgumentException(nameof(parentStep)); // The parent step has the compilation/diagnostics

            // Get titles for all the diagnostics this step can fix
            var diagnosticTitles = _fixProvider.FixableDiagnosticIds.Select(i => diagnostics.FirstOrDefault(d => d.Id.Equals(i))?.Title).Where(t => t != null);

            Title = $"Apply fix for {DiagnosticId}{(diagnosticTitles is null ? string.Empty : ": " + string.Join(", ", diagnosticTitles))}";
            Description = $"Update source files in {options.ProjectPath} to automatically fix migration analyzer {DiagnosticId}";
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync(IMigrationContext context, CancellationToken token)
        {
            Logger.LogDebug("Identified {DiagnosticCount} fixable {DiagnosticId} diagnostics", Diagnostics.Count(), DiagnosticId);

            // This migration step is incomplete if any diagnostics it can fix remain
            return Diagnostics.Any() ?
                Task.FromResult((MigrationStepStatus.Incomplete, $"{Diagnostics.Count()} {DiagnosticId} diagnostics need fixed")) :
                Task.FromResult<(MigrationStepStatus, string)>((MigrationStepStatus.Complete, string.Empty));
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync(IMigrationContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_sourceUpdater.Project is null)
            {
                return (MigrationStepStatus.Failed, "No project available.");
            }

            // Regenerating diagnostics is slow for large projects, but is necessary in between fixing multiple diagnostics
            // in a single file. To try and minimize the number of time diagnostics are gathered, fix one diagnostic each
            // from multiple files before regenerating diagnostics.
            while (Diagnostics.Any())
            {
                // Iterate through the first diagnostic from each document
                foreach (var diagnostic in Diagnostics.GroupBy(d => d.Location.SourceTree.FilePath).Select(g => g.First()))
                {
                    var doc = _sourceUpdater.Project.GetDocument(diagnostic.Location.SourceTree)!;
                    var updatedSolution = await TryFixDiagnosticAsync(diagnostic, doc).ConfigureAwait(false);

                    if (updatedSolution is null)
                    {
                        Logger.LogError("Failed to fix diagnostic {DiagnosticId} in {FilePath}", diagnostic.Id, doc.FilePath);
                        return (MigrationStepStatus.Failed, $"Failed to fix diagnostic {diagnostic.Id} in {doc.FilePath}");
                    }
                    else if (!_sourceUpdater.UpdateSolution(updatedSolution))
                    {
                        Logger.LogError("Failed to apply changes after fixing {DiagnosticId} to {FilePath}", diagnostic.Id, doc.FilePath);
                        return (MigrationStepStatus.Failed, $"Failed to apply changes after fixing {diagnostic.Id} to {doc.FilePath}");
                    }
                    else
                    {
                        Logger.LogInformation("Diagnostic {DiagnosticId} fixed in {FilePath}", diagnostic.Id, doc.FilePath);
                    }
                }

                // Re-build and get an updated list of diagnostics
                await _sourceUpdater.GetDiagnosticsAsync(context, token).ConfigureAwait(false);
            }

            // TEMPORARY WORKAROUND
            // https://github.com/dotnet/roslyn/issues/36781
            (await context.GetProjectRootElementAsync(token).ConfigureAwait(false)).WorkAroundRoslynIssue36781();

            Logger.LogDebug("All instances of {DiagnosticId} fixed", DiagnosticId);
            return (MigrationStepStatus.Complete, $"No instances of {DiagnosticId} need fixed");
        }

        private async Task<Solution?> TryFixDiagnosticAsync(Diagnostic diagnostic, Document document)
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
            var context = new CodeFixContext(document, diagnostic, (action, _) => fixAction = action, CancellationToken.None);
            await _fixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            if (fixAction is null)
            {
                Logger.LogWarning("No code fix found for {DiagnosticId}", diagnostic.Id);
                return null;
            }

            var applyOperation = (await fixAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false)).OfType<ApplyChangesOperation>().FirstOrDefault();

            if (applyOperation is null)
            {
                Logger.LogWarning("Code fix could not be applied for {DiagnosticId}", diagnostic.Id);
                return null;
            }

            return applyOperation.ChangedSolution;
        }
    }
}
