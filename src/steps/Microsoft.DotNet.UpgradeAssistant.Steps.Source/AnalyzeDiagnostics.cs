// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public class AnalyzeDiagnostics : IAnalyzeResultProvider
    {
        private readonly IEnumerable<DiagnosticAnalyzer> _allAnalyzers;
        private readonly ImmutableArray<AdditionalText> _additionalTexts;
        private readonly ILogger _logger;

        public string Name => "API Upgradability";

        public Uri InformationURI => new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");

        public AnalyzeDiagnostics(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<AdditionalText> additionalTexts, ILogger<AnalyzeDiagnostics> logger)
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
            _allAnalyzers = analyzers;
            _additionalTexts = ImmutableArray.CreateRange(additionalTexts);
        }

        public async IAsyncEnumerable<AnalyzeResult> AnalyzeAsync(AnalyzeContext analysis, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
            var projects = context.Projects.ToList();

            foreach (var project in projects)
            {
                var diagnostics = await this.GetDiagnosticsAsync(project, token).ConfigureAwait(false);

                foreach (var r in ProcessDiagnostics(diagnostics))
                {
                    yield return r;
                }
            }
        }

        private static HashSet<AnalyzeResult> ProcessDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            var results = new HashSet<AnalyzeResult>();
            foreach (var diag in diagnostics)
            {
                results.Add(new()
                {
                    RuleId = diag.Id,
                    RuleName = diag.Descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture),

                    // Since the first line in a file is defined as line 0 (zero based line
                    // numbering) by the LinePostion struct offsetting by one to support VS 1-based line numbering.
                    LineNumber = diag.Location.GetLineSpan().Span.End.Line + 1,
                    FileLocation = diag.Location.GetLineSpan().Path,
                    ResultMessage = diag.Descriptor.Description.ToString(System.Globalization.CultureInfo.InvariantCulture),
                });
            }

            return results;
        }

        private async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(IProject project, CancellationToken token)
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
