// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public class RazorSourceUpdater : IUpdater<RazorCodeDocument>
    {
        private readonly IEnumerable<DiagnosticAnalyzer> _analyzers;
        private readonly IEnumerable<CodeFixProvider> _codeFixProviders;
        private readonly ILogger<RazorSourceUpdater> _logger;

        public string Id => typeof(RazorSourceUpdater).FullName!;

        public string Title => "Apply code fixes to Razor documents";

        public string Description => "Update code within Razor documents to fix diagnostics according to registered Roslyn analyzers and code fix providers";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public RazorSourceUpdater(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<CodeFixProvider> codeFixProviders, ILogger<RazorSourceUpdater> logger)
        {
            _analyzers = analyzers?.OrderBy(a => a.SupportedDiagnostics.First().Id) ?? throw new ArgumentNullException(nameof(analyzers));
            _codeFixProviders = codeFixProviders?.OrderBy(c => c.FixableDiagnosticIds.First()) ?? throw new ArgumentNullException(nameof(codeFixProviders));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var project = GetProjectWithGeneratedCode(context.CurrentProject.Required(), inputs);
            _logger.LogDebug("Running upgrade analyzers on Razor files in {ProjectName}", project.Name);

            // Use mapped locations so that we only get a number of diagnostics corresponding to the number of cshtml locations that need fixed.
            var mappedDiagnostics = await GetDiagnosticsAsync(project, context, inputs.Select(GetGeneratedFilePath), new LocationAndIDComparer(true), token).ConfigureAwait(false);
            _logger.LogInformation("Identified {DiagnosticCount} fixable diagnostics in Razor files in project {ProjectName} ()", mappedDiagnostics.Count(), project.Name);
            var diagnosticsByFile = mappedDiagnostics.GroupBy(d => d.Location.GetMappedLineSpan().Path);
            foreach (var diagnosticsGroup in diagnosticsByFile)
            {
                _logger.LogInformation("  {DiagnosticsCount} diagnostics need fixed in {FilePath}", diagnosticsGroup.Count(), diagnosticsGroup.Key);
            }

            return new FileUpdaterResult(mappedDiagnostics.Any(), diagnosticsByFile.Select(g => g.Key));
        }

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
            // in a single file. To try and minimize the number of time diagnostics are gathered, fix one diagnostic each
            // from multiple files before regenerating diagnostics.
            var diagnostics = await GetDiagnosticsAsync(project, context, generatedFilePaths, new LocationAndIDComparer(false), token).ConfigureAwait(false);
            var diagnosticsCount = diagnostics.Count();
            while (diagnosticsCount > 0)
            {
                // Iterate through the first diagnostic from each document
                foreach (var diagnostic in diagnostics.GroupBy(d => d.Location.SourceTree?.FilePath).Select(g => g.First()))
                {
                    var doc = project.GetDocument(diagnostic.Location.SourceTree);
                    if (doc is null)
                    {
                        continue;
                    }

                    project = await TryFixDiagnosticAsync(diagnostic, doc).ConfigureAwait(false);
                }

                diagnostics = await GetDiagnosticsAsync(project, context, generatedFilePaths, new LocationAndIDComparer(false), token).ConfigureAwait(false);
                var newDiagnosticCount = diagnostics.Count();
                if (diagnosticsCount == newDiagnosticCount)
                {
                    _logger.LogWarning("Diagnostics could not be fixed as expected. This may be caused by the project being in a bad state (did NuGet packages restore correctly?) or by errors in analyzers or code fix providers related to diagnosticsL {DiagnosticIds}.", string.Join(", ", diagnostics.Select(d => d.Id).Distinct()));
                    break;
                }
                else
                {
                    diagnosticsCount = newDiagnosticCount;
                }
            }

            // Update cshtml
            _logger.LogDebug("Updating Razor documents based on changes made by code fix providers");
            var textReplacements = await GetReplacements(originalProject, project.Documents.Where(d => generatedFilePaths.Contains(d.FilePath)), token).ConfigureAwait(false);
            ApplyMappedCodeChanges(textReplacements, inputs, token);
            await FixUpProjectFileAsync(context, token).ConfigureAwait(false);

            if (diagnosticsCount > 0)
            {
                _logger.LogWarning("Completing Razor source updates with {DiagnosticCount} diagnostics still unaddressed", diagnosticsCount);
            }

            return new FileUpdaterResult(true, textReplacements.Select(r => r.OriginalText.FilePath).Distinct());
        }

        private void ApplyMappedCodeChanges(IList<TextReplacement> replacements, ImmutableArray<RazorCodeDocument> razorDocuments, CancellationToken token)
        {
            var replacementsByFile = replacements.Distinct().OrderByDescending(t => t.OriginalText.StartingLine).GroupBy(t => t.OriginalText.FilePath);
            foreach (var replacementGroup in replacementsByFile)
            {
                // TODO : Optimize this
                var documentText = new StringBuilder(File.ReadAllText(replacementGroup.Key));
                var razorDoc = razorDocuments.FirstOrDefault(d => d.Source.FilePath.Equals(replacementGroup.Key, StringComparison.OrdinalIgnoreCase));
                if (razorDoc is null)
                {
                    throw new InvalidOperationException($"No Razor document found for generated source file {replacementGroup.Key}");
                }

                foreach (var replacement in replacementGroup)
                {
                    _logger.LogInformation("Updating source code in Razor document {FilePath} at line {Line}", replacement.OriginalText.FilePath, replacement.OriginalText.StartingLine);

                    // Start looking for replacements at the start of the indicated line
                    var startOffset = GetLineOffset(razorDoc, replacement.OriginalText.StartingLine);

                    // Stop looking for replacements at the start of the first line after the indicated line plus the number of lines in the indicated text
                    var endOffset = GetLineOffset(razorDoc, replacement.OriginalText.StartingLine + replacement.OriginalText.Text.Lines.Count);

                    var originalText = replacement.OriginalText.Text.ToString();
                    foreach (var change in replacement.NewText.GetTextChanges(replacement.OriginalText.Text))
                    {
                        // Trim the string that's being replaced because code from Razor code blocks will include a couple extra spaces (to make room for @{)
                        // compared to the source that actually appeared in the cshtml file.
                        var original = originalText.Substring(change.Span.Start, change.Span.Length).TrimStart();

                        // If the original text was completely removed, also search for implicit and explicit Razor expression syntax (@ or @()) so that it will be cleaned up, too
                        if (original.Equals(originalText.TrimStart(), StringComparison.Ordinal) && string.IsNullOrWhiteSpace(change.NewText))
                        {
                            var implicitExpression = $"@{original.Replace(";", string.Empty)}";
                            var explicitExpression = $"@({original.Replace(";", string.Empty)})";
                            documentText.Replace(implicitExpression, string.Empty, startOffset, endOffset - startOffset);
                            documentText.Replace(explicitExpression, string.Empty, startOffset, endOffset - startOffset);
                        }

                        documentText.Replace(original, change.NewText?.TrimStart() ?? string.Empty, startOffset, endOffset - startOffset);
                    }
                }

                File.WriteAllText(replacementGroup.Key, documentText.ToString());
            }
        }

        private async Task<IList<TextReplacement>> GetReplacements(Project originalProject, IEnumerable<Document> updatedDocuments, CancellationToken token)
        {
            List<TextReplacement> replacements = new List<TextReplacement>();
            foreach (var updatedDocument in updatedDocuments)
            {
                var originalDocument = originalProject.GetDocument(updatedDocument.Id);
                if (originalDocument is null)
                {
                    _logger.LogWarning("Cannot find expected generated file {FilePath}", updatedDocument.FilePath);
                    continue;
                }

                // We want to correlate the mapped code blocks in the original document with corresponding ones in the updated document.
                // Unfortunately, this is non-trivial because some code blocks may have been removed and, in other cases, multiple code blocks
                // can have the same source location.
                var originalSubTextGroups = (await MappedSubText.GetMappedSubTextsAsync(originalDocument, token).ConfigureAwait(false)).ToLookup(m => m.SourceLocation);
                var updatedSubTextGroups = (await MappedSubText.GetMappedSubTextsAsync(updatedDocument, token).ConfigureAwait(false)).ToLookup(m => m.SourceLocation);

                var candidateReplacements = new List<TextReplacement>();

                foreach (var originalGroup in originalSubTextGroups)
                {
                    var updatedGroup = updatedSubTextGroups[originalGroup.Key] ?? Enumerable.Empty<MappedSubText>();

                    // If both groups have the same number of elements, then they pair in order
                    if (originalGroup.Count() == updatedGroup.Count())
                    {
                        candidateReplacements.AddRange(originalGroup.Zip(updatedGroup, (original, updated) => new TextReplacement(original, updated.Text)));
                    }

                    // If the updated group is empty, then the original elements all pair with empty source text
                    else if (!updatedGroup.Any())
                    {
                        candidateReplacements.AddRange(originalGroup.Select(m => new TextReplacement(m, SourceText.From(string.Empty))));
                    }

                    // This is the tricky one. If there are less updated code blocks than original code blocks, it will be necesary to guess which original code blocks
                    // pair with the remaining updated code blocks based on test similarity.
                    else
                    {
                        var originalList = originalGroup.ToList();
                        foreach (var updatedText in updatedGroup.Select(m => m.Text))
                        {
                            var bestMatch = originalList.OrderBy(m => updatedText.GetChangeRanges(m.Text).Sum(r => r.Span.Length)).First();
                            originalList.Remove(bestMatch);
                            candidateReplacements.Add(new TextReplacement(bestMatch, updatedText));
                        }

                        // Add remaining original code blocks paired with empty source text
                        candidateReplacements.AddRange(originalList.Select(m => new TextReplacement(m, SourceText.From(string.Empty))));
                    }
                }

                // Ignore any code block pairings without text changes
                replacements.AddRange(candidateReplacements.Where(c => !c.NewText.ContentEquals(c.OriginalText.Text)));
            }

            return replacements;
        }

        private static int GetLineOffset(RazorCodeDocument razorDoc, int startingLine)
        {
            var offset = 0;

            for (var i = 1; i < startingLine && i <= razorDoc.Source.Lines.Count; i++)
            {
                // StreamSourceDoc.Lines is 0-based but line directives (as used in MappedSubText) are 1-based,
                // so subtract one from i.
                offset += razorDoc.Source.Lines.GetLineLength(i - 1);
            }

            return offset;
        }

        private static string GetGeneratedFilePath(RazorCodeDocument doc) => $"{doc.Source.FilePath}.cs";

        private static async Task FixUpProjectFileAsync(IUpgradeContext context, CancellationToken token)
        {
            var file = context.CurrentProject.Required().GetFile();
            file.Simplify();
            await file.SaveAsync(token).ConfigureAwait(false);
        }

        private async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Project project, IUpgradeContext context, IEnumerable<string> targetFilePaths, IEqualityComparer<Diagnostic> diagnosticsComparer, CancellationToken token)
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
                .WithAnalyzers(ImmutableArray.CreateRange(applicableAnalyzers), new CompilationWithAnalyzersOptions(new AnalyzerOptions(default), ProcessAnalyzerException, true, true));

            var allDiagnostics = await updatedCompilation.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false);

            // Filter diagnostics to those that can be addressed by available code fix providers and that are located in generated Razor source
            var ret = allDiagnostics
                .Where(d => d.Location.IsInSource &&
                       targetFilePaths.Contains(d.Location.SourceTree.FilePath) &&
                       _codeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)))
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
            analyzers.ToAsyncEnumerable().WhereAwaitWithCancellation((a, token) => a.GetType().AppliesToProjectAsync(project, token));

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic) =>
            _logger.LogError(exc, "Analyzer error while running analyzer {AnalyzerId}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)));
    }
}
