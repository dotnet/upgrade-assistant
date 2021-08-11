// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source.Tests
{
    public class RoslynDiagnosticProviderTests
    {
        private readonly Fixture _fixture;

        public RoslynDiagnosticProviderTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task DiagnosticsWithEmptyProjectTest()
        {
            // Arrange
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetAnalyzer()));

            var project = new Mock<IProject>();
            project.Setup(p => p.GetRoslynProject())
                .Returns(() => new AdhocWorkspace().AddProject(_fixture.Create<string>(), LanguageNames.CSharp));

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });

            // Act
            var diagnostics = await mock.Create<RoslynDiagnosticProvider>().GetDiagnosticsAsync(project.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DiagnosticsWithProjectFilesTest(bool includeLocation)
        {
            // Arrange
            var analyzer = GetAnalyzer(includeLocation: includeLocation);
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(analyzer));

            var project = new Mock<IProject>();
            project.Setup(p => p.GetRoslynProject()).Returns(() => new AdhocWorkspace()
                .AddProject(_fixture.Create<string>(), LanguageNames.CSharp)
                .AddDocument(_fixture.Create<string>(), string.Empty)
                .Project);

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });

            // Act
            var diagnostics = await mock.Create<RoslynDiagnosticProvider>().GetDiagnosticsAsync(project.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            if (includeLocation)
            {
                Assert.Collection(diagnostics, d => Assert.Equal(analyzer.SupportedDiagnostics.Single().Id, d.Id));
            }
            else
            {
                Assert.Empty(diagnostics);
            }
        }

        [Fact]
        public void GetEmptyCodeFixResult()
        {
            // Arrange
            using var mock = AutoMock.GetLoose(b =>
            {
                b.RegisterInstance(Enumerable.Empty<CodeFixProvider>());
            });

            // Act
            var result = mock.Create<RoslynDiagnosticProvider>().GetCodeFixProviders();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetFromList()
        {
            // Arrange
            var codeFixer1 = new Mock<CodeFixProvider>();
            var codeFixer2 = new Mock<CodeFixProvider>();

            using var mock = AutoMock.GetLoose(b =>
            {
                b.RegisterInstance(codeFixer1.Object);
                b.RegisterInstance(codeFixer2.Object);
            });

            // Act
            var result = mock.Create<RoslynDiagnosticProvider>().GetCodeFixProviders();

            // Assert
            Assert.Collection(result,
                r => Assert.Same(codeFixer1.Object, r),
                r => Assert.Same(codeFixer2.Object, r));
        }

        [Fact]
        public void GetApplicableDescriptors()
        {
            // Arrange
            var analyzer1 = GetAnalyzer(1);
            var analyzer2 = GetAnalyzer(2);
            var analyzer3 = GetAnalyzer(3);

            var codeFixer1 = new Mock<CodeFixProvider>();
            codeFixer1.Setup(c => c.FixableDiagnosticIds).Returns(ImmutableArray.Create(analyzer1.SupportedDiagnostics[0].Id, analyzer3.SupportedDiagnostics[0].Id));

            using var mock = AutoMock.GetLoose(b =>
            {
                b.RegisterInstance(analyzer1);
                b.RegisterInstance(analyzer2);
                b.RegisterInstance(analyzer3);
            });

            // Act
            var result = mock.Create<RoslynDiagnosticProvider>().GetDiagnosticDescriptors(codeFixer1.Object).ToList();

            // Assert
            Assert.Collection(result,
                r => Assert.Same(analyzer1.SupportedDiagnostics[0], r),
                r => Assert.Same(analyzer3.SupportedDiagnostics[0], r));
        }

        private static DiagnosticAnalyzer GetAnalyzer(int? suffix = null, bool includeLocation = false)
        {
            if (suffix is null)
            {
                suffix = 1;
            }

            var descriptor = new DiagnosticDescriptor($"Test{suffix}", $"Test diagnostic 1", $"Test message 1", "Test", DiagnosticSeverity.Warning, true);

            var analyzer = new Mock<DiagnosticAnalyzer>();
            analyzer.Setup(a => a.SupportedDiagnostics).Returns(ImmutableArray.Create(descriptor));
            analyzer.Setup(a => a.Initialize(It.IsAny<AnalysisContext>())).Callback<AnalysisContext>(context => context.RegisterSyntaxTreeAction(x =>
            {
                var location = includeLocation ? x.Tree.GetRoot().GetLocation() : null;
                var diagnostic = Diagnostic.Create(descriptor, location);
                x.ReportDiagnostic(diagnostic);
            }));

            return analyzer.Object;
        }
    }
}
