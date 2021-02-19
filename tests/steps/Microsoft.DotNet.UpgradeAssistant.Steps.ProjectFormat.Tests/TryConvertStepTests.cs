﻿using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.Tests
{
    public class TryConvertStepTests
    {
        private readonly Fixture _fixture;

        public TryConvertStepTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void DependencyTests()
        {
            using var mock = AutoMock.GetLoose();

            var step = mock.Create<TryConvertProjectConverterStep>();

            Assert.Equal(new[] { "Microsoft.DotNet.UpgradeAssistant.Steps.Backup.BackupStep" }, step.DependsOn);
            Assert.Equal(new[] { "Microsoft.DotNet.UpgradeAssistant.Migrator.Steps.NextProjectStep" }, step.DependencyOf);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void IsApplicable(bool isApplicable)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var step = mock.Create<TryConvertProjectConverterStep>();

            var context = mock.Mock<IMigrationContext>();

            if (isApplicable)
            {
                context.Setup(m => m.CurrentProject).Returns(mock.Mock<IProject>().Object);
            }

            // Act
            var result = step.IsApplicable(context.Object);

            // Assert
            Assert.Equal(isApplicable, result);
        }

        [Fact]
        public async Task ToolNotAvailable()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            mock.Mock<ITryConvertTool>().Setup(m => m.IsAvailable).Returns(false);

            var step = mock.Create<TryConvertProjectConverterStep>();
            var context = mock.Mock<IMigrationContext>();

            // Act
            await step.InitializeAsync(context.Object, default);

            // Assert
            Assert.Equal(MigrationStepStatus.Failed, step.Status);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task InitializeTests(bool isSdk)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            mock.Mock<ITryConvertTool>().Setup(m => m.IsAvailable).Returns(true);

            var step = mock.Create<TryConvertProjectConverterStep>();

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(m => m.IsSdk).Returns(isSdk);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);

            var context = mock.Mock<IMigrationContext>();
            context.Setup(m => m.CurrentProject).Returns(project.Object);

            // Act
            await step.InitializeAsync(context.Object, default);

            // Assert
            var expected = isSdk ? MigrationStepStatus.Complete : MigrationStepStatus.Incomplete;
            Assert.Equal(expected, step.Status);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task ApplyTests(bool isSuccess)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var step = mock.Create<TryConvertProjectConverterStep>();
            step.SetStatus(MigrationStepStatus.Incomplete);

            var project = mock.Mock<IProject>();

            var context = mock.Mock<IMigrationContext>();
            context.Setup(m => m.CurrentProject).Returns(project.Object);

            mock.Mock<ITryConvertTool>().Setup(m => m.RunAsync(context.Object, project.Object, default)).ReturnsAsync(isSuccess);

            // Act
            await step.ApplyAsync(context.Object, default);

            // Assert
            var expected = isSuccess ? MigrationStepStatus.Complete : MigrationStepStatus.Failed;
            Assert.Equal(expected, step.Status);
        }
    }
}
