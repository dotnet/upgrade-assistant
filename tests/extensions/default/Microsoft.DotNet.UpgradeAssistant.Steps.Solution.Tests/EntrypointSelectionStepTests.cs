// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution.Tests
{
    public class EntrypointSelectionStepTests
    {
        private readonly Fixture _fixture;

        public EntrypointSelectionStepTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task InitializeTestsEntrypointSet()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.EntryPoints).Returns(new[] { new Mock<IProject>().Object });

            mock.Mock<IOptions<SolutionOptions>>().Setup(o => o.Value).Returns(new SolutionOptions());
            var step = mock.Create<EntrypointSelectionStep>();

            // Act
            await step.InitializeAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(UpgradeStepStatus.Complete, step.Status);
        }

        [Fact]
        public async Task InitializeTestsSingleEntrypoint()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            mock.Mock<IOptions<SolutionOptions>>().Setup(o => o.Value).Returns(new SolutionOptions());

            var project = new Mock<IProject>().Object;
            var context = mock.Mock<IUpgradeContext>();
            context.SetupProperty(t => t.EntryPoints);
            context.Setup(c => c.Projects).Returns(new[] { project });
            context.Object.EntryPoints = Enumerable.Empty<IProject>();

            var step = mock.Create<EntrypointSelectionStep>();

            // Act
            await step.InitializeAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(UpgradeStepStatus.Complete, step.Status);
            Assert.Collection(context.Object.EntryPoints, e => Assert.Equal(e, project));
        }

        [Fact]
        public async Task InitializeTestsNotSolution()
        {
            // Arrange
            const int ProjectCount = 10;
            using var mock = AutoMock.GetLoose();

            var projects = Enumerable.Range(0, ProjectCount).Select(_ =>
            {
                var project = new Mock<IProject>();

                project.Setup(p => p.FileInfo).Returns(new FileInfo(_fixture.Create<string>()));

                return project.Object;
            }).ToList();

            var inputProject = projects[ProjectCount / 2];

            mock.Mock<IOptions<SolutionOptions>>().Setup(o => o.Value).Returns(new SolutionOptions());

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.InputIsSolution).Returns(false);
            context.SetupProperty(t => t.EntryPoints);
            context.Setup(c => c.InputPath).Returns(inputProject.FileInfo.Name);
            context.Setup(c => c.Projects).Returns(projects);
            context.Object.EntryPoints = Enumerable.Empty<IProject>();

            var step = mock.Create<EntrypointSelectionStep>();

            // Act
            await step.InitializeAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(UpgradeStepStatus.Complete, step.Status);
            Assert.Collection(context.Object.EntryPoints, e => Assert.Equal(e, inputProject));
        }

        [Fact]
        public async Task InitializeTestsInSolution()
        {
            // Arrange
            const int ProjectCount = 10;
            using var mock = AutoMock.GetLoose();

            var projects = Enumerable.Range(0, ProjectCount).Select(_ =>
            {
                var project = new Mock<IProject>();

                project.Setup(p => p.FileInfo).Returns(new FileInfo(_fixture.Create<string>()));

                return project.Object;
            }).ToList();

            var selectedProject = projects[ProjectCount / 2];

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.InputIsSolution).Returns(true);
            context.SetupProperty(t => t.EntryPoints);
            context.Setup(c => c.Projects).Returns(projects);
            context.Object.EntryPoints = Enumerable.Empty<IProject>();

            var options = mock.Mock<IOptions<SolutionOptions>>();
            options.Setup(o => o.Value).Returns(new SolutionOptions { Entrypoints = _fixture.CreateMany<string>().ToArray() });

            var resolver = mock.Mock<IEntrypointResolver>();
            resolver.Setup(r => r.GetEntrypoints(projects, options.Object.Value.Entrypoints)).Returns(new[] { selectedProject });

            var step = mock.Create<EntrypointSelectionStep>();

            // Act
            await step.InitializeAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(UpgradeStepStatus.Complete, step.Status);
            Assert.Collection(context.Object.EntryPoints, e => Assert.Equal(e, selectedProject));
        }

        [InlineData(true, UpgradeStepStatus.Incomplete)]
        [InlineData(false, UpgradeStepStatus.Incomplete)]
        [Theory]
        public async Task InitializeTestsInSolutionNoSelection(bool isInteractive, UpgradeStepStatus status)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            mock.Mock<IUserInput>().Setup(u => u.IsInteractive).Returns(isInteractive);

            mock.Mock<IOptions<SolutionOptions>>().Setup(o => o.Value).Returns(new SolutionOptions());

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.InputIsSolution).Returns(true);
            context.SetupProperty(t => t.EntryPoints);
            context.Setup(c => c.Projects).Returns(Enumerable.Empty<IProject>());
            context.Object.EntryPoints = Enumerable.Empty<IProject>();

            var step = mock.Create<EntrypointSelectionStep>();

            // Act
            await step.InitializeAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(status, step.Status);
            Assert.Empty(context.Object.EntryPoints);
        }
    }
}
