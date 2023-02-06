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
            using var mock = GetMock("RazorSourceUpdaterStepViews/Test.csproj", Array.Empty<LocationLookup[]>());
            var analyzers = mock.Container.Resolve<IEnumerable<DiagnosticAnalyzer>>();
            var textMatcher = mock.Container.Resolve<ITextMatcher>();
            var codeFixProviders = mock.Container.Resolve<IEnumerable<CodeFixProvider>>();
            var textReplacer = mock.Mock<IMappedTextReplacer>();
            var logger = mock.Mock<ILogger<RazorSourceUpdater>>();

            Assert.Throws<ArgumentNullException>("analyzers", () => new RazorSourceUpdater(null!, codeFixProviders, ImmutableArray<AdditionalText>.Empty, textMatcher, textReplacer.Object, logger.Object));
            Assert.Throws<ArgumentNullException>("codeFixProviders", () => new RazorSourceUpdater(analyzers, null!, ImmutableArray<AdditionalText>.Empty, textMatcher, textReplacer.Object, logger.Object));
            Assert.Throws<ArgumentNullException>("textMatcher", () => new RazorSourceUpdater(analyzers, codeFixProviders, ImmutableArray<AdditionalText>.Empty, null!, textReplacer.Object, logger.Object));
            Assert.Throws<ArgumentNullException>("textReplacer", () => new RazorSourceUpdater(analyzers, codeFixProviders, ImmutableArray<AdditionalText>.Empty, textMatcher, null!, logger.Object));
            Assert.Throws<ArgumentNullException>("logger", () => new RazorSourceUpdater(analyzers, codeFixProviders, ImmutableArray<AdditionalText>.Empty, textMatcher, textReplacer.Object, null!));
            Assert.Throws<ArgumentNullException>("additionalTexts", () => new RazorSourceUpdater(analyzers, codeFixProviders, null!, textMatcher, textReplacer.Object, logger.Object));
        }

        [Fact]
        public void PropertyTests()
        {
            using var mock = GetMock("RazorSourceUpdaterStepViews/Test.csproj", Array.Empty<LocationLookup[]>());
            var updater = mock.Create<RazorSourceUpdater>();

            Assert.Equal("Microsoft.DotNet.UpgradeAssistant.Steps.Razor.RazorSourceUpdater", updater.Id);
            Assert.Equal("Apply code fixes to Razor documents", updater.Title);
            Assert.Equal("Update code within Razor documents to fix diagnostics according to registered Roslyn analyzers and code fix providers", updater.Description);
            Assert.Equal(BuildBreakRisk.Medium, updater.Risk);
        }

        [Fact]
        public async Task IsApplicableNegativeTests()
        {
            using var mock = GetMock(null, Array.Empty<LocationLookup[]>());
            var updater = mock.Create<RazorSourceUpdater>();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => updater.IsApplicableAsync(null!, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None)).ConfigureAwait(true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => updater.IsApplicableAsync(mock.Mock<IUpgradeContext>().Object, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None)).ConfigureAwait(true);
        }

        [Theory]
        [MemberData(nameof(IsApplicableData))]
        public async Task IsApplicableTests(LocationLookup[][] diagnosticLocations, string[] expectedApplicableFilePaths)
        {
            // Arrange
            using var mock = GetMock("RazorSourceUpdaterStepViews/Test.csproj", diagnosticLocations);
            var razorDocs = await GetRazorCodeDocumentsAsync(mock).ConfigureAwait(true);
            var context = mock.Mock<IUpgradeContext>();
            var updater = mock.Create<RazorSourceUpdater>();

            // Act
            var result = (FileUpdaterResult)await updater.IsApplicableAsync(context.Object, ImmutableArray.CreateRange(razorDocs), CancellationToken.None).ConfigureAwait(true);
            var resultWithoutDocs = (FileUpdaterResult)await updater.IsApplicableAsync(context.Object, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.Equal(expectedApplicableFilePaths.Any(), result.Result);
            Assert.Collection(result.FilePaths.OrderBy(f => f), expectedApplicableFilePaths.OrderBy(f => f).Select<string, Action<string>>(expected => actual => Assert.Equal(expected, actual)).ToArray());
            Assert.False(resultWithoutDocs.Result);
            Assert.Empty(resultWithoutDocs.FilePaths);
        }

        public static IEnumerable<object[]> IsApplicableData =>
            new List<object[]>
            {
                // No diagnostcs reported
                new object[]
                {
                    Array.Empty<LocationLookup[]>(),
                    Array.Empty<string>(),
                },

                // Diagnostics in non-Razor files
                new object[]
                {
                    new[] { new[] { new LocationLookup("Foo.cs", null, 10, 15) } },
                    Array.Empty<string>(),
                },

                // Diagnostic in Razor file
                new object[]
                {
                    new[]
                    {
                        new[] { new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "Model[0]") },
                    },
                    new[] { GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml") },
                },

                // Diagnostic mapped to shared Razor file
                new object[]
                {
                    new[]
                    {
                        new[] { new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "using Microsoft.AspNetCore.Mvc;") },
                    },
                    new[] { GetFullPath("RazorSourceUpdaterStepViews\\_ViewImports.cshtml") },
                },

                // Diagnostic in unmapped portions of generated files
                new object[]
                {
                    new[]
                    {
                        new[]
                        {
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "assembly: global::Microsoft.AspNetCore"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "WriteLiteral(\"    <div>\\r\\n        <p>\")"),
                        },
                    },
                    new[]
                    {
                        GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs"),
                    },
                },

                // Diagnostics in multiple files (from multiple analyzers)
                new object[]
                {
                    new[]
                    {
                        new[]
                        {
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "using Microsoft.AspNetCore.Mvc;"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "Model[0]"),
                        },
                        new[]
                        {
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "Model[1]"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\Simple.cshtml.cs", "using Microsoft.AspNetCore.Mvc;"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\Simple.cshtml.cs", "DateTime.Now.ToString()"),
                        }
                    },
                    new[]
                    {
                        GetFullPath("RazorSourceUpdaterStepViews\\_ViewImports.cshtml"),
                        GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml"),
                        GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\Simple.cshtml"),
                    },
                }
            };

        [Fact]
        public async Task ApplyNegativeTests()
        {
            using var mock = GetMock(null, Array.Empty<LocationLookup[]>());
            var updater = mock.Create<RazorSourceUpdater>();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => updater.ApplyAsync(null!, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None)).ConfigureAwait(true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => updater.ApplyAsync(mock.Mock<IUpgradeContext>().Object, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None)).ConfigureAwait(true);
        }

        [Theory]
        [MemberData(nameof(ApplyData))]
        public async Task ApplyTests(LocationLookup[][] diagnosticLocations, string[] expectedUpdatedFiles, MappedTextReplacement[] expectedReplacements)
        {
            // Arrange
            using var mock = GetMock("RazorSourceUpdaterStepViews/Test.csproj", diagnosticLocations);
            var razorDocs = await GetRazorCodeDocumentsAsync(mock).ConfigureAwait(true);
            var context = mock.Mock<IUpgradeContext>();
            var updater = mock.Create<RazorSourceUpdater>();
            var replacements = new List<MappedTextReplacement>();
            var textReplacer = mock.Mock<IMappedTextReplacer>();
            textReplacer.Setup(r => r.ApplyTextReplacements(It.IsAny<IEnumerable<MappedTextReplacement>>()))
                .Callback<IEnumerable<MappedTextReplacement>>(newReplacements => replacements.AddRange(newReplacements));

            // Act
            var result = (FileUpdaterResult)await updater.ApplyAsync(context.Object, ImmutableArray.CreateRange(razorDocs), CancellationToken.None).ConfigureAwait(true);
            var resultWithoutDocs = (FileUpdaterResult)await updater.ApplyAsync(context.Object, ImmutableArray.Create<RazorCodeDocument>(), CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.True(result.Result);
            Assert.Collection(result.FilePaths.OrderBy(f => f), expectedUpdatedFiles.OrderBy(f => f).Select<string, Action<string>>(expected => actual => Assert.Equal(expected, actual)).ToArray());
            Assert.True(resultWithoutDocs.Result);
            Assert.Empty(resultWithoutDocs.FilePaths);
            Assert.Collection(replacements, expectedReplacements.Select<MappedTextReplacement, Action<MappedTextReplacement>>(e => a =>
            {
                if (string.IsNullOrEmpty(e.OriginalText.ToString()) && string.IsNullOrEmpty(e.NewText.ToString()))
                {
                    Assert.Equal(e.FilePath, a.FilePath);
                    Assert.Equal(e.StartingLine, a.StartingLine);
                }
                else
                {
                    Assert.Equal(e, a);
                }
            }).ToArray());
        }

        public static IEnumerable<object[]> ApplyData =>
            new List<object[]>
            {
                // No diagnostcs reported
                new object[]
                {
                    Array.Empty<LocationLookup[]>(),
                    Array.Empty<string>(),
                    Array.Empty<MappedTextReplacement>(),
                },

                // Diagnostics in non-Razor files
                new object[]
                {
                    new[] { new[] { new LocationLookup("Foo.cs", null, 10, 15) } },
                    Array.Empty<string>(),
                    Array.Empty<MappedTextReplacement>(),
                },

                // Diagnostic in Razor file
                new object[]
                {
                    new[]
                    {
                        new[] { new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "Model[0]") },
                    },
                    new[] { GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml") },
                    new[] { new MappedTextReplacement("      Write(Model[0]);\r\n".ReplaceLineEndings(), "      Write(Model[0] /* Test! */);\r\n".ReplaceLineEndings(), GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml"), 6) }
                },

                // Diagnostic mapped to shared Razor file
                new object[]
                {
                    new[]
                    {
                        new[] { new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "using Microsoft.AspNetCore.Mvc;") },
                    },
                    new[] { GetFullPath("RazorSourceUpdaterStepViews\\_ViewImports.cshtml") },
                    new[] { new MappedTextReplacement("using Microsoft.AspNetCore.Mvc;\r\n".ReplaceLineEndings(), "using Microsoft.AspNetCore.Mvc; /* Test! */\r\n".ReplaceLineEndings(), GetFullPath("RazorSourceUpdaterStepViews\\_ViewImports.cshtml"), 1) }
                },

                // Diagnostic in unmapped portions of generated files
                new object[]
                {
                    new[]
                    {
                        new[]
                        {
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "assembly: global::Microsoft.AspNetCore"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "WriteLiteral(\"    <div>\\r\\n        <p>\")".ReplaceLineEndings()),
                        },
                    },
                    new[] { GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml") },
                    new[]
                    {
                        // The first one *does* generate a replacement because it represents text being prepended to the beginning of the source file
                        // Don't check the actual text, though, since it will include file path-specific values that will change
                        new MappedTextReplacement(string.Empty, string.Empty, GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml"), 0),
                    }
                },

                // Diagnostics in multiple files (from multiple analyzers)
                new object[]
                {
                    new[]
                    {
                        new[]
                        {
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "using Microsoft.AspNetCore.Mvc;"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "Model[0]"),
                        },
                        new[]
                        {
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml.cs", "Model[1]"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\Simple.cshtml.cs", "using Microsoft.AspNetCore.Mvc;"),
                            new LocationLookup("RazorSourceUpdaterStepViews\\TestViews\\Simple.cshtml.cs", "DateTime.Now.ToString()"),
                        }
                    },
                    new[]
                    {
                        GetFullPath("RazorSourceUpdaterStepViews\\_ViewImports.cshtml"),
                        GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml"),
                        GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\Simple.cshtml"),
                    },
                    new[]
                    {
                        new MappedTextReplacement("using Microsoft.AspNetCore.Mvc;\r\n".ReplaceLineEndings(), "using Microsoft.AspNetCore.Mvc; /* Test! */\r\n".ReplaceLineEndings(), GetFullPath("RazorSourceUpdaterStepViews\\_ViewImports.cshtml"), 1),
                        new MappedTextReplacement(" Write(DateTime.Now.ToString());\r\n".ReplaceLineEndings(), " Write(DateTime.Now.ToString() /* Test! */);\r\n".ReplaceLineEndings(), GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\Simple.cshtml"), 1),
                        new MappedTextReplacement("      Write(Model[0]);\r\n".ReplaceLineEndings(), "      Write(Model[0] /* Test! */);\r\n".ReplaceLineEndings(), GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml"), 6),
                        new MappedTextReplacement("      Write(Model[1]);\r\n".ReplaceLineEndings(), "      Write(Model[1] /* Test! */);\r\n".ReplaceLineEndings(), GetFullPath("RazorSourceUpdaterStepViews\\TestViews\\View.cshtml"), 18),
                    }
                }
            };

        private static AutoMock GetMock(string? projectPath, LocationLookup[][] diagnosticLocations)
        {
            var mock = AutoMock.GetLoose(builder =>
            {
                builder.RegisterType<DefaultTextMatcher>().As<ITextMatcher>();
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
                            foreach (var lookup in locations.Where(l => GetFullPath(l.Path).Equals(x.Tree.FilePath, StringComparison.Ordinal)))
                            {
                                var start = lookup.StartOffset;
                                var end = lookup.EndOffset;

                                if (lookup.Keyword is not null)
                                {
                                    var index = x.Tree.GetText().ToString().IndexOf(lookup.Keyword, StringComparison.Ordinal);
                                    (start, end) = (index, index + lookup.Keyword.Length);
                                }

                                // If the 'test' trivia hasn't been added by the code fix yet, report a diagnostic
                                var line = x.Tree.GetText().Lines.GetLineFromPosition(start);
                                var lineText = x.Tree.GetText().GetSubText(line.Span).ToString();
                                if (!lineText.Contains("Test!", StringComparison.Ordinal))
                                {
                                    var location = Location.Create(x.Tree, TextSpan.FromBounds(start, end));
                                    var diagnostic = Diagnostic.Create(descriptor, location);
                                    x.ReportDiagnostic(diagnostic);
                                }
                            }
                        }));
                        builder.RegisterMock(analyzer);

                        builder.RegisterInstance<CodeFixProvider>(new TestCodeFixProvider(new[] { descriptor.Id }));
                    }
                }
                else
                {
                    builder.RegisterInstance(Enumerable.Empty<DiagnosticAnalyzer>());
                    builder.RegisterInstance(Enumerable.Empty<CodeFixProvider>());
                }
            });

            var projectFile = mock.Mock<IProjectFile>();
            var project = projectPath is not null ? mock.Mock<IProject>() : null;
            project?.Setup(p => p.FileInfo).Returns(new FileInfo(projectPath!));
            project?.Setup(p => p.GetFile()).Returns(projectFile.Object);
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

        private static string GetFullPath(string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);

            return Path.IsPathFullyQualified(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        }
    }
}
