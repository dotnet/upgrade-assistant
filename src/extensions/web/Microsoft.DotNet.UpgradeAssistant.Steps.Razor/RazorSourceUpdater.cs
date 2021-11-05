// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// An updater for RazorCodeDocuments which applies Roslyn analyzers and code fix providers to the source within the documents.
    /// </summary>
    public class RazorSourceUpdater : IUpdater<RazorCodeDocument>
    {
        private readonly IEnumerable<DiagnosticAnalyzer> _analyzers;
        private readonly IEnumerable<CodeFixProvider> _codeFixProviders;
        private readonly ImmutableArray<AdditionalText> _additionalTexts;
        private readonly ITextMatcher _textMatcher;
        private readonly IMappedTextReplacer _textReplacer;
        private readonly ILogger<RazorSourceUpdater> _logger;

        /// <summary>
        /// Gets a unique identifier for this updater.
        /// </summary>
        public string Id => typeof(RazorSourceUpdater).FullName!;

        /// <summary>
        /// Gets a short user-friendly title for this updater.
        /// </summary>
        public string Title => "Apply code fixes to Razor documents";

        /// <summary>
        /// Gets a user-friendly description of this updater's function.
        /// </summary>
        public string Description => "Update code within Razor documents to fix diagnostics according to registered Roslyn analyzers and code fix providers";

        /// <summary>
        /// Gets a value indicating the risk that this updater will introduce build breaks when applied.
        /// </summary>
        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorSourceUpdater"/> class.
        /// </summary>
        /// <param name="analyzers">Analyzers to use when analyzing source code in the Razor documents.</param>
        /// <param name="codeFixProviders">Code fix providers to use when fixing diagnostics found in the Razor documents.</param>
        /// <param name="additionalTexts">Additional documents that should be included in analysis. This will typically be additional texts used to configure analyzer behavior.</param>
        /// <param name="textMatcher">The text matching service to use for correlating old sections of text in Razor documents with updated texts.</param>
        /// <param name="textReplacer">The text replacing service to use for updating replaced texts in the Razor documents.</param>
        /// <param name="logger">An ILogger to log diagnostics.</param>
        public RazorSourceUpdater(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<CodeFixProvider> codeFixProviders, IEnumerable<AdditionalText> additionalTexts, ITextMatcher textMatcher, IMappedTextReplacer textReplacer, ILogger<RazorSourceUpdater> logger)
        {
            if (additionalTexts is null)
            {
                throw new ArgumentNullException(nameof(additionalTexts));
            }

            _analyzers = analyzers?.OrderBy(a => a.SupportedDiagnostics.First().Id) ?? throw new ArgumentNullException(nameof(analyzers));
            _codeFixProviders = codeFixProviders?.OrderBy(c => c.FixableDiagnosticIds.First()) ?? throw new ArgumentNullException(nameof(codeFixProviders));
            _additionalTexts = ImmutableArray.CreateRange(additionalTexts);
            _textMatcher = textMatcher ?? throw new ArgumentNullException(nameof(textMatcher));
            _textReplacer = textReplacer ?? throw new ArgumentNullException(nameof(textReplacer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Determines whether or not there are diagnostics in Razor documents in the context's current project that this updater can address.
        /// </summary>
        /// <param name="context">The upgrade context containing the project to analyze.</param>
        /// <param name="inputs">The Razor documents within the context's current project that should be analyzed for possible source updates.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>True if the Razor documents contain diagnostics that this updater can address, false otherwise.</returns>
        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = GetProjectWithGeneratedCode(context.CurrentProject.Required(), inputs);
            _logger.LogDebug("Running upgrade analyzers on Razor files in {ProjectName}", project.Name);

            // Use mapped locations so that we only get a number of diagnostics corresponding to the number of cshtml locations that need fixed.
            var mappedDiagnostics = await GetDiagnosticsFromTargetFilesAsync(project, context, inputs.Select(GetGeneratedFilePath), new LocationAndIDComparer(true), token).ConfigureAwait(false);
            _logger.LogInformation("Identified {DiagnosticCount} diagnostics in Razor files in project {ProjectName}", mappedDiagnostics.Count(), project.Name);
            var diagnosticsByFile = mappedDiagnostics.GroupBy(d => d.Location.GetMappedLineSpan().Path);
            foreach (var diagnosticsGroup in diagnosticsByFile)
            {
                _logger.LogInformation("  {DiagnosticsCount} diagnostics need fixed in {FilePath}", diagnosticsGroup.Count(), diagnosticsGroup.Key);
            }

            return new FileUpdaterResult(mappedDiagnostics.Any(), diagnosticsByFile.Select(g => g.Key));
        }

        /// <summary>
        /// Update source code in Razor documents using Roslyn analyzers and code fix providers.
        /// </summary>
        /// <param name="context">The upgrade context with the current project containing Razor documents.</param>
        /// <param name="inputs">The Razor documents to update.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A FileUpdaterResult indicating which documents were updated.</returns>
        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = GetProjectWithGeneratedCode(context.CurrentProject.Required(), inputs);
            var originalProject = project;
            var generatedFilePaths = inputs.Select(GetGeneratedFilePath);

            // Regenerating diagnostics is slow for large projects, but is necessary in between fixing multiple diagnostics
            // in a single file. To minimize the number of time diagnostics are gathered, fix one diagnostic each
            // from multiple files before regenerating diagnostics.
            var diagnostics = await GetDiagnosticsFromTargetFilesAsync(project, context, generatedFilePaths, new LocationAndIDComparer(false), token).ConfigureAwait(false);
            var fixableDiagnostics = diagnostics.Where(d => _codeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)));
            var diagnosticsCount = diagnostics.Count();
            while (fixableDiagnostics.Any())
            {
                // Finding and fixing the diagnostics can be a bit slow; provide an update once per loop iteration
                // so that users can see progress.
                _logger.LogInformation("{DiagnosticCount} diagnostics remaining to be fixed", diagnosticsCount);

                // Iterate through the first fixable diagnostic from each document
                foreach (var diagnostic in fixableDiagnostics.GroupBy(d => d.Location.SourceTree?.FilePath).Select(g => g.First()))
                {
                    var doc = project.GetDocument(diagnostic.Location.SourceTree);
                    if (doc is null)
                    {
                        continue;
                    }

                    // Apply the code fix in the generated source
                    project = await TryFixDiagnosticAsync(diagnostic, doc).ConfigureAwait(false);
                }

                diagnostics = await GetDiagnosticsFromTargetFilesAsync(project, context, generatedFilePaths, new LocationAndIDComparer(false), token).ConfigureAwait(false);
                fixableDiagnostics = diagnostics.Where(d => _codeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)));
                var newDiagnosticCount = diagnostics.Count();
                if (diagnosticsCount == newDiagnosticCount)
                {
                    _logger.LogWarning("Diagnostics could not be fixed as expected. This may be caused by the project being in a bad state (did NuGet packages restore correctly?) or by errors in analyzers or code fix providers related to diagnostics: {DiagnosticIds}.", string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
                    break;
                }
                else
                {
                    diagnosticsCount = newDiagnosticCount;
                }
            }

            // Identify changed code sections
            var textReplacements = await GetReplacements(originalProject, project.Documents.Where(d => generatedFilePaths.Contains(d.FilePath)), token).ConfigureAwait(false);
            var z = project.Documents.Where(d => generatedFilePaths.Contains(d.FilePath)).Select(d => d.GetTextAsync().Result.ToString()).ToArray();

            // Update cshtml based on changes made to generated source code
            // These are applied after finding all of them so that they can be applied in reverse line order
            _logger.LogDebug("Applying {ReplacementCount} updates to Razor documents based on changes made by code fix providers", textReplacements.Count());
            _textReplacer.ApplyTextReplacements(textReplacements);

            // Update the solution/project in case a code fix updated files outside of the views
            project = GetProjectWithoutGeneratedCode(project, inputs);
            context.UpdateSolution(project.Solution);
            await FixUpProjectFileAsync(context, token).ConfigureAwait(false);

            if (diagnosticsCount > 0)
            {
                _logger.LogInformation("Razor source updates complete with {DiagnosticCount} diagnostics remaining which require manual updates", diagnosticsCount);
                foreach (var diagnostic in diagnostics)
                {
                    _logger.LogWarning("Manual updates needed to address: {DiagnosticId}@{DiagnosticLocation}: {DiagnosticMessage}", diagnostic.Id, diagnostic.Location.GetMappedLineSpan(), diagnostic.GetMessage());
                }
            }

            return new FileUpdaterResult(true, textReplacements.Select(r => r.FilePath).Distinct());
        }

        private async Task<IEnumerable<MappedTextReplacement>> GetReplacements(Project originalProject, IEnumerable<Document> updatedDocuments, CancellationToken token)
        {
            var replacements = new List<MappedTextReplacement>();
            foreach (var updatedDocument in updatedDocuments)
            {
                var originalDocument = originalProject.GetDocument(updatedDocument.Id);
                if (originalDocument is null)
                {
                    _logger.LogWarning("Cannot find expected generated file {FilePath}", updatedDocument.FilePath);
                    continue;
                }

                var mappedFilePath = originalDocument.FilePath?.Replace(".cshtml.cs", ".cshtml");
                var originalSubTextGroups = (await MappedSubText.GetMappedSubTextsAsync(originalDocument, mappedFilePath, token).ConfigureAwait(false)).ToLookup(m => m.SourceLocation);
                var updatedSubTextGroups = (await MappedSubText.GetMappedSubTextsAsync(updatedDocument, mappedFilePath, token).ConfigureAwait(false)).ToLookup(m => m.SourceLocation);

                foreach (var originalGroup in originalSubTextGroups)
                {
                    var updatedText = updatedSubTextGroups[originalGroup.Key]?.Select(t => t.Text.ToString()) ?? Enumerable.Empty<string>();
                    replacements.AddRange(_textMatcher.MatchOrderedSubTexts(originalGroup, updatedText));
                }
            }

            return replacements.Distinct();
        }

        private static string GetGeneratedFilePath(RazorCodeDocument doc) => $"{doc.Source.FilePath}.cs";

        private static async Task FixUpProjectFileAsync(IUpgradeContext context, CancellationToken token)
        {
            // Reload the workspace in case code fixes modified the project file
            await context.ReloadWorkspaceAsync(token).ConfigureAwait(false);
            var file = context.CurrentProject.Required().GetFile();
            file.Simplify();
            await file.SaveAsync(token).ConfigureAwait(false);
        }

        private async Task<IEnumerable<Diagnostic>> GetDiagnosticsFromTargetFilesAsync(Project project, IUpgradeContext context, IEnumerable<string> targetFilePaths, IEqualityComparer<Diagnostic> diagnosticsComparer, CancellationToken token)
        {
            var applicableAnalyzers = await GetApplicableAnalyzersAsync(_analyzers, context.CurrentProject!).ToListAsync(token).ConfigureAwait(false);

            if (!applicableAnalyzers.Any())
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var compilation = await project.GetCompilationAsync(token).ConfigureAwait(false);
            if (compilation is null)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var updatedCompilation = compilation
                .WithAnalyzers(ImmutableArray.CreateRange(applicableAnalyzers), new CompilationWithAnalyzersOptions(new AnalyzerOptions(_additionalTexts), ProcessAnalyzerException, true, true));

            var allDiagnostics = await updatedCompilation.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false);

            // Find all diagnostics that registered analyzers produce
            // Note that this intentionally identifies diagnostics that no code fix providers can
            // address so that users can be warned about diagnostics that they will need to address manually.
            var ret = allDiagnostics
                .Where(d => d.Location.IsInSource &&
                       targetFilePaths.Contains(d.Location.SourceTree.FilePath))
                .Distinct(diagnosticsComparer);

            return ret;
        }

        private static Project GetProjectWithGeneratedCode(IProject project, IEnumerable<RazorCodeDocument> documents)
        {
            var projectWithGeneratedCode = project.GetRoslynProject();
            foreach (var document in documents)
            {
                var filePath = GetGeneratedFilePath(document);
                projectWithGeneratedCode = projectWithGeneratedCode.AddDocument(Path.GetFileName(filePath), document.GetCSharpDocument().GeneratedCode, null, filePath).Project;
            }

            return projectWithGeneratedCode;
        }

        private static Project GetProjectWithoutGeneratedCode(Project project, IEnumerable<RazorCodeDocument> documents)
        {
            var filePaths = documents.Select(GetGeneratedFilePath);
            var documentsToRemove = project.Documents.Where(d => filePaths.Contains(d.FilePath, StringComparer.OrdinalIgnoreCase)).Select(d => d.Id);
            return project.RemoveDocuments(ImmutableArray.CreateRange(documentsToRemove));
        }

        private async Task<Project> TryFixDiagnosticAsync(Diagnostic diagnostic, Document document)
        {
            CodeAction? fixAction = null;
            var context = new CodeFixContext(document, diagnostic, (action, _) => fixAction = action, CancellationToken.None);
            var fixProvider = _codeFixProviders.FirstOrDefault(p => p.FixableDiagnosticIds.Contains(diagnostic.Id));

            if (fixProvider is not null)
            {
                await fixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
            }

            // fixAction may not be null if the code fixer is applied.
#pragma warning disable CA1508 // Avoid dead conditional code
            if (fixAction is null)
#pragma warning restore CA1508 // Avoid dead conditional code
            {
                _logger.LogWarning("No code fix found for {DiagnosticId}", diagnostic.Id);
                return document.Project;
            }

            var applyOperation = (await fixAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false)).OfType<ApplyChangesOperation>().FirstOrDefault();

            if (applyOperation is null)
            {
                _logger.LogWarning("Code fix could not be applied for {DiagnosticId}", diagnostic.Id);
                return document.Project;
            }

            return applyOperation.ChangedSolution.GetProject(document.Project.Id) ?? throw new InvalidOperationException("Updated project not found");
        }

        private static IAsyncEnumerable<DiagnosticAnalyzer> GetApplicableAnalyzersAsync(IEnumerable<DiagnosticAnalyzer> analyzers, IProject project) =>
            analyzers.ToAsyncEnumerable().WhereAwaitWithCancellation((a, token) => project.IsApplicableAsync(a, token));

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic) =>
            _logger.LogError(exc, "Analyzer error while running analyzer {AnalyzerId}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)));
    }
}
