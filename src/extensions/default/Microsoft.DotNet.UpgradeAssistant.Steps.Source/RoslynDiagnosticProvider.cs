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
                .WhereAwaitWithCancellation((a, token) => a.GetType().AppliesToProjectAsync(project, token))
                .ToListAsync(token).ConfigureAwait(false);

            if (applicableAnalyzers.Any())
            {
                var compilation = await roslynProject.GetCompilationAsync(token).ConfigureAwait(false);
                if (compilation is not null)
                {
                    var compilationWithAnalyzer = compilation
                        .WithAnalyzers(ImmutableArray.CreateRange(applicableAnalyzers), new CompilationWithAnalyzersOptions(new AnalyzerOptions(_additionalTexts), ProcessAnalyzerException, true, true));

                    // Find all diagnostics that registered analyzers produce
                    var diagnostics = (await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false)).Where(d => d.Location.IsInSource);
                    _logger.LogInformation("Identified {DiagnosticCount} diagnostics in project {ProjectName}", diagnostics.Count(), roslynProject.Name);
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
    }
}
