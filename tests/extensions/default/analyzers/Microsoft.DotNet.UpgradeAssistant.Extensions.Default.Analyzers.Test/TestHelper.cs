// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Steps.Source;
using Polly;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public static class TestHelper
    {
        // Path relative from .\bin\debug\net5.0
        // TODO : Make this configurable so the test can pass from other working dirs
        internal const string TestProjectPath = @"assets\TestProject.{lang}proj";
        private const string TypeMapPath = "WebTypeReplacements.typemap";

        internal static ImmutableArray<DiagnosticAnalyzer> AllAnalyzers => ImmutableArray.Create<DiagnosticAnalyzer>(
            new AttributeUpgradeAnalyzer(),
            new BinaryFormatterUnsafeDeserializeAnalyzer(),
            new HtmlHelperAnalyzer(),
            new HttpContextCurrentAnalyzer(),
            new HttpContextIsDebuggingEnabledAnalyzer(),
            new TypeUpgradeAnalyzer(),
            new UsingSystemWebAnalyzer(),
            new UrlHelperAnalyzer(),
            new ApiAlertAnalyzer());

        internal static ImmutableArray<CodeFixProvider> AllCodeFixProviders => ImmutableArray.Create<CodeFixProvider>(
            new AttributeUpgradeCodeFixer(),
            new BinaryFormatterUnsafeDeserializeCodeFixer(),
            new HtmlHelperCodeFixer(),
            new HttpContextCurrentCodeFixer(),
            new HttpContextIsDebuggingEnabledCodeFixer(),
            new TypeUpgradeCodeFixer(),
            new UsingSystemWebCodeFixer(),
            new UrlHelperCodeFixer());

        private static ImmutableArray<AdditionalText> AdditionalTexts => ImmutableArray.Create<AdditionalText>(new AdditionalFileText(Path.Combine(AppContext.BaseDirectory, TypeMapPath)));

        public static Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(string documentPath, IEnumerable<string> diagnosticIds, bool isFramework)
        {
            return GetDiagnosticsAsync(Language.CSharp, documentPath, isFramework, diagnosticIds);
        }

        public static async Task<Project> CreateProjectAsync(Language language, bool isFramework, CancellationToken cancellationToken = default)
        {
            var workspace = new AdhocWorkspace();

            var project = workspace.AddProject("testProject", language.ToLanguageName());

            ReferenceAssemblies GetReferenceAssemblies()
            {
                if (isFramework)
                {
                    var packages = new[]
                    {
                        new PackageIdentity("Microsoft.AspNet.Razor", "5.2.7"),
                        new PackageIdentity("Microsoft.AspNet.Mvc", "5.2.7"),
                        new PackageIdentity("Microsoft.Owin", "4.2.0")
                    };

                    return ReferenceAssemblies.NetFramework.Net48.Default
                        .AddPackages(ImmutableArray.Create(packages))
                        .AddAssemblies(ImmutableArray.Create("System.Web"));
                }
                else
                {
                    return ReferenceAssemblies.NetCore.NetCoreApp31
                        .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.AspNetCore.App.Ref", "3.1.10")));
                }
            }

            var references = await Policy.Handle<Exception>()
                .RetryAsync(3)
                .ExecuteAsync(cancellationToken => GetReferenceAssemblies().ResolveAsync(language.ToLanguageName(), cancellationToken), cancellationToken);

            project = project.WithMetadataReferences(references);

            foreach (var file in Directory.EnumerateFiles("assets", $"*.{language.GetFileExtension()}", SearchOption.AllDirectories))
            {
                if (!file.Contains(".Fixed."))
                {
                    project = project.AddDocument(Path.GetFileName(file), File.ReadAllText(file))
                        .Project;
                }
            }

            return project;
        }

        public static async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Language lang, string documentPath, bool isFramework, IEnumerable<string> diagnosticIds)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            var project = await CreateProjectAsync(lang, isFramework);

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
                .WithAnalyzers(ImmutableArray.Create(analyzersToUse.ToArray()), new AnalyzerOptions(AdditionalTexts));

            return (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
                .Where(d => d.Location.IsInSource && documentPath.Equals(Path.GetFileNameWithoutExtension(d.Location.GetLineSpan().Path), StringComparison.Ordinal))
                .Where(d => diagnosticIds.Contains(d.Id, StringComparer.Ordinal));
        }

        public static string GetSource(Language lang, string documentPath)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            var path = Directory.EnumerateFiles("assets", $"{documentPath}.{lang.GetFileExtension()}", SearchOption.AllDirectories)
                .First();

            return File.ReadAllText(path);
        }

        public static async Task<string> FixSourceAsync(Language lang, string documentPath, IEnumerable<string> diagnosticIds)
        {
            if (documentPath is null)
            {
                throw new ArgumentNullException(nameof(documentPath));
            }

            var project = await CreateProjectAsync(lang, isFramework: false);
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
                .GetTextAsync();

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
