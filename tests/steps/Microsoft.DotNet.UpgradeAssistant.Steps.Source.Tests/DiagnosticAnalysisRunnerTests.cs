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
using AutoFixture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source.Tests
{
    public class DiagnosticAnalysisRunnerTests
    {
        private readonly Fixture _fixture;

        public DiagnosticAnalysisRunnerTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task DiagnosticsWithEmptyProjectTest()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var loggerMock = new Mock<ILogger<RoslynDiagnosticProvider>>();

            var project = CreateProject(mock);
            project.Setup(p => p.GetRoslynProject()).Returns(() =>
            {
                var ws = new AdhocWorkspace();
                var name = Path.GetFileNameWithoutExtension("TestProject.csproj")!;
                return ws.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, name, name, "C#", filePath: "TestProject.csproj"));
            });

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });
            var diagnosticAnalysisRunner = new RoslynDiagnosticProvider(GetAnalyzers(false), Array.Empty<AdditionalText>(), loggerMock.Object);

            // Act
            var diagnostics = await diagnosticAnalysisRunner.GetDiagnosticsAsync(project.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(false, 0, null)]
        [InlineData(true, 1, "Test1")]
        public async Task DiagnosticsWithProjectFilesTest(bool includeLocation, int expectedDiagnosticCount, string? expectedDiagnosticId)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var loggerMock = new Mock<ILogger<RoslynDiagnosticProvider>>();

            var project = CreateProject(mock);
            project.Setup(p => p.GetRoslynProject()).Returns(() =>
            {
                var ws = new AdhocWorkspace();
                var name = Path.GetFileNameWithoutExtension("TestProject.csproj")!;
                var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, name, name, "C#", filePath: "TestProject.csproj");
                var documents = new List<DocumentInfo>() { DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Foo.cs") };
                var proj = ws.AddProject(projectInfo.WithDocuments(documents));
                return proj;
            });

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });
            var diagnosticAnalysisRunner = new RoslynDiagnosticProvider(GetAnalyzers(includeLocation), Array.Empty<AdditionalText>(), loggerMock.Object);

            // Act
            var diagnostics = await diagnosticAnalysisRunner.GetDiagnosticsAsync(project.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(diagnostics.Count() == expectedDiagnosticCount);
            if (diagnostics.Any())
            {
                Assert.Collection(diagnostics, r => Assert.Equal(r.Id, expectedDiagnosticId));
            }
        }

        private Mock<IProject> CreateProject(AutoMock mock)
        {
            var project = mock.Mock<IProject>();
            var projectFile = mock.Mock<IProjectFile>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.FileInfo).Returns(new FileInfo(_fixture.Create<string>()));

            return project;
        }

        private IEnumerable<DiagnosticAnalyzer> GetAnalyzers(bool includeLocation)
        {
            var analyzer = new Mock<DiagnosticAnalyzer>();
            var descriptor = new DiagnosticDescriptor($"Test1", $"Test diagnostic 1", $"Test message 1", "Test", DiagnosticSeverity.Warning, true);
            analyzer.Setup(a => a.SupportedDiagnostics).Returns(ImmutableArray.Create(descriptor));
            analyzer.Setup(a => a.Initialize(It.IsAny<AnalysisContext>())).Callback<AnalysisContext>(context => context.RegisterSyntaxTreeAction(x =>
            {
                var location = includeLocation ? x.Tree.GetRoot().GetLocation() : null;
                var diagnostic = Diagnostic.Create(descriptor, location);
                x.ReportDiagnostic(diagnostic);
            }));

            return new List<DiagnosticAnalyzer>() { analyzer.Object };
        }
    }
}
