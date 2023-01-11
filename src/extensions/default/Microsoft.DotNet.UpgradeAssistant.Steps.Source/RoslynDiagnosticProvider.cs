// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public class RoslynDiagnosticProvider : IRoslynDiagnosticProvider
    {
        private readonly IEnumerable<DiagnosticAnalyzer> _allAnalyzers;
        private readonly ImmutableArray<AdditionalText> _additionalTexts;
        private readonly IEnumerable<CodeFixProvider> _codeFixProviders;
        private readonly ILogger<RoslynDiagnosticProvider> _logger;

        public RoslynDiagnosticProvider(
            IEnumerable<DiagnosticAnalyzer> analyzers,
            IEnumerable<AdditionalText> additionalTexts,
            IEnumerable<CodeFixProvider> codeFixProviders,
            ILogger<RoslynDiagnosticProvider> logger)
        {
            if (analyzers is null)
            {
                throw new ArgumentNullException(nameof(analyzers));
            }

            if (additionalTexts is null)
            {
                throw new ArgumentNullException(nameof(additionalTexts));
            }

            _codeFixProviders = codeFixProviders ?? throw new ArgumentNullException(nameof(codeFixProviders));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _allAnalyzers = analyzers.OrderBy(a => a.SupportedDiagnostics.First().Id);
            _additionalTexts = ImmutableArray.CreateRange(additionalTexts);
        }

        public async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                _logger.LogWarning("No project available.");
                return Enumerable.Empty<Diagnostic>();
            }

            var roslynProject = project.GetRoslynProject();

            if (roslynProject is null)
            {
                _logger.LogWarning("No project available.");
                return Enumerable.Empty<Diagnostic>();
            }

            _logger.LogInformation("Running analyzers on {ProjectName}", roslynProject.Name);

            // Compile with analyzers enabled
            var applicableAnalyzers = await _allAnalyzers
                .ToAsyncEnumerable()
                .WhereAwaitWithCancellation((a, token) => project.IsApplicableAsync(a, token))
                .ToListAsync(token).ConfigureAwait(false);

            if (applicableAnalyzers.Any())
            {
                var compilation = await roslynProject.GetCompilationAsync(token).ConfigureAwait(false);
                if (compilation is not null)
                {
                    // Include *.xaml files
                    List<AdditionalText> xamlFiles = new List<AdditionalText>();
                    foreach (var document in roslynProject.AdditionalDocuments)
                    {
                        if (document.FilePath?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            SourceText sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
                            xamlFiles.Add(new AdditionalTextWrapper(document, sourceText));
                        }
                    }

                    var texts = _additionalTexts.AddRange(xamlFiles);
                    var compilationWithAnalyzer = compilation
                        .WithAnalyzers(ImmutableArray.CreateRange(applicableAnalyzers), new CompilationWithAnalyzersOptions(new AnalyzerOptions(texts), ProcessAnalyzerException, true, true));

                    // Find all diagnostics that registered analyzers produce
                    var diagnostics = (await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false))
                        /*.Where(d => d.Location.IsInSource)*/.ToList();
                    _logger.LogInformation("Identified {DiagnosticCount} diagnostics in project {ProjectName}", diagnostics.Count, roslynProject.Name);
                    return diagnostics;
                }
            }

            return Enumerable.Empty<Diagnostic>();

            void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
            {
                _logger.LogError(exc, "Analyzer error while running analyzer {AnalyzerId}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)));
            }
        }

        public IEnumerable<CodeFixProvider> GetCodeFixProviders() => _codeFixProviders;

        public IEnumerable<DiagnosticDescriptor> GetDiagnosticDescriptors(CodeFixProvider provider)
            => _allAnalyzers.SelectMany(a => a.SupportedDiagnostics).Where(d => provider.FixableDiagnosticIds.Contains(d.Id));

        private class AdditionalTextWrapper : AdditionalText
        {
            private readonly TextDocument document;
            private SourceText text;

            public AdditionalTextWrapper(TextDocument document, SourceText text)
            {
                this.document = document ?? throw new ArgumentNullException(nameof(document));
                this.text = text ?? throw new ArgumentNullException(nameof(text));
            }

            public override string Path => this.document.FilePath ?? throw new InvalidOperationException();

            public override SourceText? GetText(CancellationToken cancellationToken = default)
            {
                this.document.TryGetText(out var sourceText);
                return sourceText;
            }
        }
    }
}
