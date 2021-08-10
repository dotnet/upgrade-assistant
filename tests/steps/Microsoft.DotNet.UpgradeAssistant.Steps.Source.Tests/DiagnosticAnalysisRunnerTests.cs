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
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source.Tests
{
    public class DiagnosticAnalysisRunnerTests
    {
        private readonly Fixture _fixture;
        internal const string TestProjectPath = @"assets\TestProject.csproj";

        private static IEnumerable<AdditionalText> AdditionalTexts
        {
            get
            {
                return Array.Empty<AdditionalText>();
            }
        }

        public DiagnosticAnalysisRunnerTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task DiagnosticsWithEmptyProjectTest()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var loggerMock = new Mock<ILogger<DiagnosticAnalysisRunner>>();

            var project = CreateProject(mock);
            project.Setup(p => p.GetRoslynProject()).Returns(() =>
            {
                var ws = new AdhocWorkspace();
                var name = Path.GetFileNameWithoutExtension(TestProjectPath)!;
                return ws.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, name, name, "C#", filePath: TestProjectPath));
            });

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });
            var diagnosticAnalysisRunner = new DiagnosticAnalysisRunner(GetAnalyzers(false), AdditionalTexts, loggerMock.Object);

            // Act
            var diagnostics = await diagnosticAnalysisRunner.GetDiagnosticsAsync(project.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 3)]
        public async Task DiagnosticsWithProjectFilesTest(bool includeLocation, int expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var loggerMock = new Mock<ILogger<DiagnosticAnalysisRunner>>();

            var project = CreateProject(mock);
            project.Setup(p => p.GetRoslynProject()).Returns(() =>
            {
                var ws = new AdhocWorkspace();
                var name = Path.GetFileNameWithoutExtension(TestProjectPath)!;
                var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, name, name, "C#", filePath: TestProjectPath);
                var documents = new List<DocumentInfo>() { DocumentInfo.Create(DocumentId.CreateNewId(projectInfo.Id), "Foo.cs").WithFilePath(@"assets\TestClasses\Foo.cs") };
                var proj = ws.AddProject(projectInfo.WithDocuments(documents));
                return proj;
            });

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });
            var diagnosticAnalysisRunner = new DiagnosticAnalysisRunner(GetAnalyzers(includeLocation), AdditionalTexts, loggerMock.Object);

            // Act
            var diagnostics = await diagnosticAnalysisRunner.GetDiagnosticsAsync(project.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(diagnostics.Count() == expected);
        }

        private Mock<IProject> CreateProject(AutoMock mock)
        {
            var project = mock.Mock<IProject>();
            var nugetReferences = mock.Mock<INuGetReferences>();
            nugetReferences.Setup(n => n.IsTransitivelyAvailableAsync(It.IsAny<string>(), default))
                .Returns(new ValueTask<bool>(false));
            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);
            projectFile.Setup(f => f.Sdk).Returns(new HashSet<string>(new[] { "Microsoft.NET.Sdk.Web" }));
            projectFile.Setup(f => f.GetPropertyValue(It.IsAny<string>())).Returns(string.Empty);

            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.TargetFrameworks).Returns(new[] { TargetFrameworkMoniker.Net50 });
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(ProjectComponents.AspNetCore));
            project.Setup(p => p.OutputType).Returns(ProjectOutputType.Exe);
            project.Setup(p => p.NuGetReferences).Returns(nugetReferences.Object);
            project.Setup(p => p.FileInfo).Returns(new FileInfo(_fixture.Create<string>()));

            return project;
        }

        private IEnumerable<DiagnosticAnalyzer> GetAnalyzers(bool includeLocation)
        {
            var analyzers = new List<DiagnosticAnalyzer>();
            for (int i = 0; i < 3; i++)
            {
                var analyzer = new Mock<DiagnosticAnalyzer>();
                var descriptor = new DiagnosticDescriptor($"Test{i}", $"Test diagnostic {i}", $"Test message {i}", "Test", DiagnosticSeverity.Warning, true);
                analyzer.Setup(a => a.SupportedDiagnostics).Returns(ImmutableArray.Create(descriptor));
                analyzer.Setup(a => a.Initialize(It.IsAny<AnalysisContext>())).Callback<AnalysisContext>(context => context.RegisterSyntaxTreeAction(x =>
                {
                    var location = includeLocation ? x.Tree.GetRoot().GetLocation() : null;
                    var diagnostic = Diagnostic.Create(descriptor, location);
                    x.ReportDiagnostic(diagnostic);
                }));
                analyzers.Add(analyzer.Object);
            }

            return analyzers;
        }
    }
}
