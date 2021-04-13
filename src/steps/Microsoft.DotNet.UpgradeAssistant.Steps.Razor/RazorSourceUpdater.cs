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
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public class RazorSourceUpdater : IUpdater<RazorCodeDocument>
    {
        private readonly IEnumerable<DiagnosticAnalyzer> _analyzers;
        private readonly IEnumerable<CodeFixProvider> _codeFixProviders;
        private readonly ILogger<RazorSourceUpdater> _logger;

        private IEnumerable<Diagnostic> _diagnostics;

        public string Id => typeof(RazorSourceUpdater).FullName!;

        public string Title => $"Apply Roslyn code fixes to Razor documents";

        public string Description => $"Update code within Razor documents to fix diagnostics according to registered Roslyn analyzers and code fix providers";

        public BuildBreakRisk Risk => BuildBreakRisk.Medium;

        public RazorSourceUpdater(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<CodeFixProvider> codeFixProviders, ILogger<RazorSourceUpdater> logger)
        {
            _analyzers = analyzers?.OrderBy(a => a.SupportedDiagnostics.First().Id) ?? throw new ArgumentNullException(nameof(analyzers));
            _codeFixProviders = codeFixProviders?.OrderBy(c => c.FixableDiagnosticIds.First()) ?? throw new ArgumentNullException(nameof(codeFixProviders));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _diagnostics = Enumerable.Empty<Diagnostic>();
        }

        public async Task<bool> IsApplicableAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await GetDiagnosticsAsync(context, inputs, token).ConfigureAwait(false);

            return _diagnostics.Any();
        }

        public Task<bool> ApplyAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        private async Task GetDiagnosticsAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            _diagnostics = Enumerable.Empty<Diagnostic>();
            var project = context.CurrentProject.Required().GetRoslynProject();

            if (project is null)
            {
                _logger.LogWarning("No project available.");
                return;
            }

            _logger.LogTrace("Running upgrade analyzers on Razor files in {ProjectName}", project.Name);

            var applicableAnalyzers = await GetApplicableAnalyzersAsync(_analyzers, context.CurrentProject!).ToListAsync(token).ConfigureAwait(false);

            if (!applicableAnalyzers.Any())
            {
                return;
            }

            var compilation = await project.GetCompilationAsync(token).ConfigureAwait(false);
            if (compilation is null)
            {
                return;
            }

            var updatedCompilation = compilation
                .AddSyntaxTrees(inputs.Select(d => CSharpSyntaxTree.ParseText(d.GetCSharpDocument().GeneratedCode, path: $"{d.Source.FilePath}.cs", cancellationToken: token)))
                .WithAnalyzers(ImmutableArray.CreateRange(applicableAnalyzers), new CompilationWithAnalyzersOptions(new AnalyzerOptions(default), ProcessAnalyzerException, true, true));

            var allDiagnostics = await updatedCompilation.GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false);

            // Filter diagnostics to those that can be addressed by available code fix providers and that are located in generated Razor source
            // Also only return each mapped location once since files like _ViewImports that are included in multiple generated files may have many diagnostics
            // referring to the same original location.
            var comparer = new MappedLocationAndIDComparer();
            _diagnostics = allDiagnostics
                .Where(d => d.Location.IsInSource &&
                       inputs.Select(i => $"{i.Source.FilePath}.cs").Contains(d.Location.SourceTree.FilePath) &&
                       _codeFixProviders.Any(f => f.FixableDiagnosticIds.Contains(d.Id)))
                .Distinct(comparer);

            _logger.LogDebug("Identified {DiagnosticCount} fixable diagnostics in Razor files in project {ProjectName}", _diagnostics.Count(), project.Name);
        }

        private static IAsyncEnumerable<DiagnosticAnalyzer> GetApplicableAnalyzersAsync(IEnumerable<DiagnosticAnalyzer> analyzers, IProject project) =>
            analyzers.ToAsyncEnumerable().WhereAwaitWithCancellation((a, token) => a.GetType().AppliesToProjectAsync(project, token));

        private void ProcessAnalyzerException(Exception exc, DiagnosticAnalyzer analyzer, Diagnostic diagnostic) =>
            _logger.LogError(exc, "Analyzer error while running analyzer {AnalyzerId}", string.Join(", ", analyzer.SupportedDiagnostics.Select(d => d.Id)));
    }
}
