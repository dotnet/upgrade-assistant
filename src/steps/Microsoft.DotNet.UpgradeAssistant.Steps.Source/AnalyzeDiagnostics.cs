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

        internal IEnumerable<Diagnostic> Diagnostics { get; set; } = Enumerable.Empty<Diagnostic>();

        public string Id => "UA102";

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
            _allAnalyzers = analyzers.OrderBy(a => a.SupportedDiagnostics.First().Id);
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
                await GetDiagnosticsAsync(project, token).ConfigureAwait(false);

                yield return new()
                {
                    Results = ProcessDiagnostics(),
                };
            }
        }

        private HashSet<ResultObj> ProcessDiagnostics()
        {
            var results = new HashSet<ResultObj>();
            foreach (var diag in Diagnostics)
            {
                results.Add(new()
                {
                    // Diagnostic line numbers are zero-based so offsetting by one
                    LineNumber = diag.Location.GetLineSpan().Span.End.Line + 1,
                    FileLocation = diag.Location.GetLineSpan().Path,
                    ResultMessage = diag.Descriptor.Description.ToString(System.Globalization.CultureInfo.InvariantCulture),
                });
            }

            return results;
        }

        public async Task GetDiagnosticsAsync(IProject project, CancellationToken token)
        {
            if (project is null)
            {
                _logger.LogWarning("No project available.");
                return;
            }

            var roslynProject = project.GetRoslynProject();

            if (roslynProject is null)
            {
                _logger.LogWarning("No project available.");
                return;
            }

            _logger.LogInformation("Running analyzers on {ProjectName}", roslynProject.Name);

            // Compile with analyzers enabled
            var applicableAnalyzers = await GetApplicableAnalyzersAsync(_allAnalyzers, project).ToListAsync(token).ConfigureAwait(false);

            if (!applicableAnalyzers.Any())
            {
                Diagnostics = Enumerable.Empty<Diagnostic>();
            }
            else
            {
                var compilation = await roslynProject.GetCompilationAsync(token).ConfigureAwait(false);
                if (compilation is null)
                {
                    Diagnostics = Enumerable.Empty<Diagnostic>();
                }
                else
                {
                    var compilationWithAnalyzer = compilation
                        .WithAnalyzers(ImmutableArray.CreateRange(applicableAnalyzers), new CompilationWithAnalyzersOptions(new AnalyzerOptions(_additionalTexts), ProcessAnalyzerException, true, true));

                    // Find all diagnostics that registered analyzers produce
                    Diagnostics = (await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false)).Where(d => d.Location.IsInSource);
                    _logger.LogInformation("Identified {DiagnosticCount} diagnostics in project {ProjectName}", Diagnostics.Count(), roslynProject.Name);
                }
            }
        }

        private static IAsyncEnumerable<DiagnosticAnalyzer> GetApplicableAnalyzersAsync(IEnumerable<DiagnosticAnalyzer> analyzers, IProject project)
            => analyzers.ToAsyncEnumerable()
                        .WhereAwaitWithCancellation((a, token) => a.GetType().AppliesToProjectAsync(project, token));

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            _logger.LogError(exc, "Analyzer error while running analyzer {AnalyzerId}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)));
        }
    }
}
