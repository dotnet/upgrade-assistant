// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Windows;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.Tests
{
    public static class TestHelper
    {
        // Path relative from .\bin\debug\net5.0
        // TODO : Make this configurable so the test can pass from other working dirs
        internal const string TestProjectPath = @"assets\TestProject.{lang}proj";

        internal static ImmutableArray<DiagnosticAnalyzer> AllAnalyzers => ImmutableArray.Create<DiagnosticAnalyzer>(
            new WinUIContentDialogAnalyzer(),
            new WinUIInitializeWindowAnalyzer(),
            new WinUIDataTransferManagerAnalyzer(),
            new WinUIInteropAnalyzer(),
            new WinUIMRTResourceManagerAnalyzer(),
            new WinUIBackButtonAnalyzer(),
            new WinUIAppWindowAnalyzer());

        internal static ImmutableArray<CodeFixProvider> AllCodeFixProviders => ImmutableArray.Create<CodeFixProvider>(
            new WinUIContentDialogCodeFixer(),
            new WinUIInitializeWindowCodeFixer(),
            new WinUIDataTransferManagerCodeFixer(),
            new WinUIInteropCodeFixer(),
            new WinUIMRTResourceManagerCodeFixer(),
            new WinUIBackButtonCodeFixer(null),
            new WinUIAppWindowCodeFixer());

        public static Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(this AdhocWorkspace workspace, string documentPath, IEnumerable<string> diagnosticIds, bool isFramework)
        {
            return workspace.GetDiagnosticsAsync(Language.CSharp, documentPath, isFramework, diagnosticIds);
        }

        public static async Task<Project> CreateProjectAsync(this AdhocWorkspace workspace, Language language, bool isFramework, CancellationToken cancellationToken = default)
        {
            var project = workspace.AddProject("testProject", language.ToLanguageName());

            ReferenceAssemblies GetReferenceAssemblies()
            {
                var packages = new[]
                {
                    new PackageIdentity("Microsoft.WindowsAppSDK", "1.0.0"),
                    new PackageIdentity("CommunityToolkit.WinUI.UI.Animations", "7.1.2"),
                    new PackageIdentity("Microsoft.Graphics.Win2D", "1.0.0.30")
                };

                return ReferenceAssemblies.Net.Net50
                    .AddPackages(ImmutableArray.Create(packages))
                    .AddAssemblies(ImmutableArray.Create("System"));
            }

            var references = await Policy.Handle<Exception>()
                .RetryAsync(3)
                .ExecuteAsync(cancellationToken => GetReferenceAssemblies().ResolveAsync(language.ToLanguageName(), cancellationToken), cancellationToken)
                .ConfigureAwait(false);

            project = project.WithMetadataReferences(references);

            foreach (var file in Directory.EnumerateFiles("assets", $"*.{language.GetFileExtension()}", SearchOption.AllDirectories))
            {
                if (!file.Contains(".Fixed.", StringComparison.Ordinal))
                {
                    project = project.AddDocument(Path.GetFileName(file), File.ReadAllText(file))
                        .Project;
                }
            }

            return project;
        }

        public static async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(this AdhocWorkspace workspace, Language lang, string documentPath, bool isFramework, IEnumerable<string> diagnosticIds)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            var project = await workspace.CreateProjectAsync(lang, isFramework).ConfigureAwait(false);

            return await GetDiagnosticsFromProjectAsync(project, documentPath, diagnosticIds).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<Diagnostic>> GetDiagnosticsFromProjectAsync(Project project, string documentPath, IEnumerable<string> diagnosticIds)
        {
            var analyzersToUse = AllAnalyzers.Where(a => a.SupportedDiagnostics.Any(d => diagnosticIds.Contains(d.Id, StringComparer.Ordinal)));

            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

            if (compilation is null)
            {
                return Enumerable.Empty<Diagnostic>();
            }

            var compilationWithAnalyzers = compilation
                .WithAnalyzers(ImmutableArray.Create(analyzersToUse.ToArray()));

            return (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
                .Where(d => d.Location.IsInSource && documentPath.Equals(Path.GetFileNameWithoutExtension(d.Location.GetLineSpan().Path), StringComparison.Ordinal))
                .Where(d => diagnosticIds.Contains(d.Id, StringComparer.Ordinal));
        }

        public static string GetSource(string documentPath)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            var path = Directory.EnumerateFiles("assets", $"{documentPath}.cs", SearchOption.AllDirectories)
                .First();

            return File.ReadAllText(path);
        }

        public static async Task<string> FixSourceAsync(this AdhocWorkspace workspace, Language lang, string documentPath, IEnumerable<string> diagnosticIds)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            var project = await workspace.CreateProjectAsync(lang, isFramework: false).ConfigureAwait(false);
            var projectId = project.Id;

            var diagnosticFixed = false;
            var solution = project.Solution;

            const int MAX_TRIES = 100;
            var fixAttempts = 0;
            do
            {
                fixAttempts++;
                diagnosticFixed = false;
                project = solution.GetProject(projectId)!;
                var diagnostics = (await GetDiagnosticsFromProjectAsync(project, documentPath, diagnosticIds).ConfigureAwait(false))
                    .OrderBy(d => d.Location.SourceSpan.Start);

                foreach (var diagnostic in diagnostics)
                {
                    var doc = project.GetDocument(diagnostic.Location.SourceTree)!;
                    var fixedSolution = await TryFixDiagnosticAsync(diagnostic, doc).ConfigureAwait(false);
                    if (fixedSolution != null)
                    {
                        solution = fixedSolution;
                        diagnosticFixed = true;
                        break;
                    }
                }

                if (fixAttempts + 1 == MAX_TRIES)
                {
                    Assert.True(false, $"The code fixers were unable to resolve the following diagnostic(s):{Environment.NewLine}   {string.Join(',', diagnostics.Select(d => d.Id))}");
                }
            }
            while (diagnosticFixed);

            project = solution.GetProject(projectId)!;

            var result = await project.Documents
                .First(d => documentPath.Equals(Path.GetFileNameWithoutExtension(d.Name), StringComparison.Ordinal))
                .GetTextAsync().ConfigureAwait(false);

            return result.ToString();
        }

        private static async Task<Solution?> TryFixDiagnosticAsync(Diagnostic diagnostic, Document document)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var provider = AllCodeFixProviders.FirstOrDefault(p => p.FixableDiagnosticIds.Contains(diagnostic.Id));

            if (provider is null)
            {
                return null;
            }

            CodeAction? fixAction = null;
            var context = new CodeFixContext(document, diagnostic, (action, _) => fixAction = action, CancellationToken.None);
            await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            // fixAction may not be null if the code fixer is applied.
#pragma warning disable CA1508 // Avoid dead conditional code
            if (fixAction is null)
#pragma warning restore CA1508 // Avoid dead conditional code
            {
                return null;
            }

            var applyOperation = (await fixAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false)).OfType<ApplyChangesOperation>().FirstOrDefault();

            if (applyOperation is null)
            {
                return null;
            }

            return applyOperation.ChangedSolution;
        }
    }
}
