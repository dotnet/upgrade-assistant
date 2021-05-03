// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public class RazorSourceUpdaterTests
    {
        [Fact]
        public void CtorNegativeTests()
        {
            using var mock = GetMock("RazorUpdaterStepViews/Test.csproj", Array.Empty<Location[]>());
            var analyzers = mock.Container.Resolve<IEnumerable<DiagnosticAnalyzer>>();
            var codeFixProviders = mock.Container.Resolve<IEnumerable<CodeFixProvider>>();
            var textMatcher = mock.Mock<ITextMatcher>();
            var textReplacer = mock.Mock<ITextReplacer>();
            var logger = mock.Mock<ILogger<RazorSourceUpdater>>();

            Assert.Throws<ArgumentNullException>("analyzers", () => new RazorSourceUpdater(null!, codeFixProviders, textMatcher.Object, textReplacer.Object, logger.Object));
            Assert.Throws<ArgumentNullException>("codeFixProviders", () => new RazorSourceUpdater(analyzers, null!, textMatcher.Object, textReplacer.Object, logger.Object));
            Assert.Throws<ArgumentNullException>("textMatcher", () => new RazorSourceUpdater(analyzers, codeFixProviders, null!, textReplacer.Object, logger.Object));
            Assert.Throws<ArgumentNullException>("textReplacer", () => new RazorSourceUpdater(analyzers, codeFixProviders, textMatcher.Object, null!, logger.Object));
            Assert.Throws<ArgumentNullException>("logger", () => new RazorSourceUpdater(analyzers, codeFixProviders, textMatcher.Object, textReplacer.Object, null!));
        }

        [Fact]
        public void PropertyTests()
        {
            using var mock = GetMock("RazorUpdaterStepViews/Test.csproj", Array.Empty<Location[]>());
            var updater = mock.Create<RazorSourceUpdater>();

            Assert.Equal("Microsoft.DotNet.UpgradeAssistant.Steps.Razor.RazorSourceUpdater", updater.Id);
            Assert.Equal("Apply code fixes to Razor documents", updater.Title);
            Assert.Equal("Update code within Razor documents to fix diagnostics according to registered Roslyn analyzers and code fix providers", updater.Description);
            Assert.Equal(BuildBreakRisk.Medium, updater.Risk);
        }

        [Fact]
        public async Task IsApplicableNegativeTests()
        {
            using var mock = GetMock(null, Array.Empty<Location[]>());
            var updater = mock.Create<RazorSourceUpdater>();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => updater.IsApplicableAsync(null!, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None)).ConfigureAwait(true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => updater.IsApplicableAsync(mock.Mock<IUpgradeContext>().Object, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None)).ConfigureAwait(true);
        }

        [Theory]
        [MemberData(nameof(IsApplicableData))]
        public async Task IsApplicableTests(Location[][] diagnosticLocations, string[] expectedApplicableFilePaths)
        {
            // Arrange
            using var mock = GetMock("RazorUpdaterStepViews/Test.csproj", diagnosticLocations);
            var razorDocs = await GetRazorCodeDocumentsAsync(mock).ConfigureAwait(true);
            var context = mock.Mock<IUpgradeContext>();
            var updater = mock.Create<RazorSourceUpdater>();

            // Act
            var result = (FileUpdaterResult)(await updater.IsApplicableAsync(context.Object, ImmutableArray.CreateRange(razorDocs), CancellationToken.None).ConfigureAwait(true));

            // Assert
            Assert.Equal(expectedApplicableFilePaths.Any(), result.Result);
            Assert.Collection(result.FilePaths.OrderBy(f => f), expectedApplicableFilePaths.OrderBy(f => f).Select<string, Action<string>>(expected => actual => Assert.Equal(expected, actual)).ToArray());
        }

        public static IEnumerable<object[]> IsApplicableData =>
            new List<object[]>
            {
                // No diagnostcs reported
                new object[]
                {
                    Array.Empty<Location[]>(),
                    Array.Empty<string>(),
                },

                // Diagnostics in non-Razor files
                new object[]
                {
                    new[] { new[] { GetLocation("Foo.cs", 10, 15) } },
                    Array.Empty<string>(),
                },

                // Diagnostic in Razor file
                new object[]
                {
                    new[]
                    {
                        new[] { GetLocation("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs", 2087, 2103) },
                    },
                    new[] { GetFullPath("RazorUpdaterStepViews\\TestViews\\View.cshtml") },
                },

                // Diagnostic mapped to shared Razor file
                new object[]
                {
                    new[]
                    {
                        new[] { GetLocation("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs", 889, 920) },
                    },
                    new[] { GetFullPath("RazorUpdaterStepViews\\_ViewImports.cshtml") },
                },

                // Diagnostic in unmapped portions of generated files
                new object[]
                {
                    new[]
                    {
                        new[]
                        {
                            GetLocation("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs", 10, 15),
                            GetLocation("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs", 1855, 1867),
                        },
                    },
                    new[]
                    {
                        GetFullPath("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs"),
                    },
                },

                // Diagnostics in multiple files (from multiple analyzers)
                new object[]
                {
                    new[]
                    {
                        new[]
                        {
                            GetLocation("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs", 889, 920),
                            GetLocation("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs", 2088, 2089),
                        },
                        new[]
                        {
                            GetLocation("RazorUpdaterStepViews\\TestViews\\View.cshtml.cs", 3030, 3038),
                            GetLocation("RazorUpdaterStepViews\\TestViews\\Simple.cshtml.cs", 895, 926),
                            GetLocation("RazorUpdaterStepViews\\TestViews\\Simple.cshtml.cs", 1747, 1778),
                        }
                    },
                    new[]
                    {
                        GetFullPath("RazorUpdaterStepViews\\_ViewImports.cshtml"),
                        GetFullPath("RazorUpdaterStepViews\\TestViews\\View.cshtml"),
                        GetFullPath("RazorUpdaterStepViews\\TestViews\\Simple.cshtml"),
                    },
                }
            };

        private static AutoMock GetMock(string? projectPath, Location[][] diagnosticLocations)
        {
            var mock = AutoMock.GetLoose(builder =>
            {
                var analyzers = new List<DiagnosticAnalyzer>();
                var codeFixProviders = new List<CodeFixProvider>();

                if (diagnosticLocations.Any())
                {
                    for (var i = 0; i < diagnosticLocations.Length; i++)
                    {
                        var analyzer = new Mock<DiagnosticAnalyzer>();
                        var descriptor = new DiagnosticDescriptor($"Test{i}", $"Test diagnostic {i}", $"Test message {i}", "Test", DiagnosticSeverity.Warning, true);
                        var locations = diagnosticLocations[i];
                        analyzer.Setup(a => a.SupportedDiagnostics).Returns(ImmutableArray.Create(descriptor));
                        analyzer.Setup(a => a.Initialize(It.IsAny<AnalysisContext>())).Callback<AnalysisContext>(context => context.RegisterSyntaxTreeAction(x =>
                        {
                            foreach (var location in locations.Where(l => GetFullPath(l.GetLineSpan().Path).Equals(x.Tree.FilePath, StringComparison.Ordinal)))
                            {
                                var diagnostic = Diagnostic.Create(descriptor, Location.Create(x.Tree, location.SourceSpan));
                                x.ReportDiagnostic(diagnostic);
                            }
                        }));
                        builder.RegisterMock(analyzer);

                        var codeFixProvider = new Mock<CodeFixProvider>();
                        codeFixProvider.Setup(x => x.FixableDiagnosticIds).Returns(ImmutableArray.Create(descriptor.Id));
                        codeFixProvider.Setup(x => x.RegisterCodeFixesAsync(It.IsAny<CodeFixContext>())).Callback<CodeFixContext>(context =>
                        {
                            context.RegisterCodeFix(CodeAction.Create($"Fix {descriptor.Id}", ct =>
                            {
                                // TODO
                                return Task.FromResult(context.Document);
                            }), context.Diagnostics);
                        });
                        builder.RegisterMock(codeFixProvider);
                    }
                }
                else
                {
                    builder.RegisterInstance(Enumerable.Empty<DiagnosticAnalyzer>());
                    builder.RegisterInstance(Enumerable.Empty<CodeFixProvider>());
                }
            });

            var project = projectPath is not null ? mock.Mock<IProject>() : null;
            project?.Setup(p => p.FileInfo).Returns(new FileInfo(projectPath!));
            project?.Setup(p => p.GetRoslynProject()).Returns(() =>
            {
                var ws = new AdhocWorkspace();
                var name = Path.GetFileNameWithoutExtension(projectPath)!;
                return ws.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, name, name, "C#", filePath: projectPath));
            });
            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(project?.Object);

            return mock;
        }

        private static async Task<IEnumerable<RazorCodeDocument>> GetRazorCodeDocumentsAsync(AutoMock mock)
        {
            var updaterStep = new RazorUpdaterStep(Enumerable.Empty<IUpdater<RazorCodeDocument>>(), mock.Mock<ILogger<RazorUpdaterStep>>().Object);
            await updaterStep.InitializeAsync(mock.Mock<IUpgradeContext>().Object, CancellationToken.None).ConfigureAwait(true);
            return updaterStep.RazorDocuments;
        }

        private static Location GetLocation(string path, int start, int end) => Location.Create(path, TextSpan.FromBounds(start, end), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero));

        private static string GetFullPath(string path) =>
            Path.IsPathFullyQualified(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);
    }
}
