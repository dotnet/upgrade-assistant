// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public class DiagnosticAnalysisRunner : IDiagnosticAnalysisRunner
    {
        private readonly IEnumerable<DiagnosticAnalyzer> _allAnalyzers;
        private readonly ImmutableArray<AdditionalText> _additionalTexts;
        private readonly ILogger _logger;

        public DiagnosticAnalysisRunner(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<AdditionalText> additionalTexts, ILogger<AnalyzeDiagnostics> logger)
        {
            if (additionalTexts is null)
            {
                throw new ArgumentNullException(nameof(additionalTexts));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
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
            var applicableAnalyzers = _allAnalyzers.ToList();

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
        }

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            _logger.LogError(exc, "Analyzer error while running analyzer {AnalyzerId}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)));
        }
    }
}
