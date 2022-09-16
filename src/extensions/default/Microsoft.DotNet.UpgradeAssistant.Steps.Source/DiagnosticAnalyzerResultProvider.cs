// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public class DiagnosticAnalyzerResultProvider : IAnalyzeResultProvider
    {
        private readonly IRoslynDiagnosticProvider _diagnosticAnalysisRunner;
        private readonly ILogger _logger;

        public string Name => "API Upgradability";

        public Uri InformationUri => new("https://docs.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview");

        public DiagnosticAnalyzerResultProvider(IRoslynDiagnosticProvider diagnosticAnalysisRunner, ILogger<DiagnosticAnalyzerResultProvider> logger)
        {
            if (diagnosticAnalysisRunner is null)
            {
                throw new ArgumentNullException(nameof(diagnosticAnalysisRunner));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _diagnosticAnalysisRunner = diagnosticAnalysisRunner;
            _logger = logger;
        }

        public async Task<bool> IsApplicableAsync(AnalyzeContext analysis, CancellationToken token)
        {
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<OutputResult> AnalyzeAsync(AnalyzeContext analysis, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            if (analysis is null)
            {
                throw new ArgumentNullException(nameof(analysis));
            }

            var context = analysis.UpgradeContext;
            var projects = context.Projects.ToList();

            foreach (var project in projects)
            {
                var diagnostics = await _diagnosticAnalysisRunner.GetDiagnosticsAsync(project, token).ConfigureAwait(false);

                foreach (var r in ProcessDiagnostics(diagnostics))
                {
                    yield return r;
                }
            }
        }

        private HashSet<OutputResult> ProcessDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            var results = new HashSet<OutputResult>();
            foreach (var diag in diagnostics)
            {
                _logger.LogInformation("Diagnostic {Id} with the message {Message} generated", diag.Id, diag.Descriptor.Description.ToString(System.Globalization.CultureInfo.InvariantCulture));
                results.Add(new()
                {
                    RuleId = diag.Id,
                    RuleName = diag.Descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture),

                    // Since the first line in a file is defined as line 0 (zero based line
                    // numbering) by the LinePostion struct offsetting by one to support VS 1-based line numbering.
                    LineNumber = diag.Location.GetLineSpan().Span.End.Line + 1,
                    FileLocation = diag.Location.GetLineSpan().Path,
                    ResultMessage = diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture),
                });
            }

            return results;
        }
    }
}
