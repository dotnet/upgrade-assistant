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
            Assert.Equal(new[] { "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep" }, step.DependencyOf);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void IsApplicable(bool isApplicable)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var step = mock.Create<TryConvertProjectConverterStep>();

            var context = mock.Mock<IUpgradeContext>();

            if (isApplicable)
            {
                context.Setup(m => m.CurrentProject).Returns(mock.Mock<IProject>().Object);
            }

            // Act
            var result = step.IsApplicable(context.Object);

            // Assert
            Assert.Equal(isApplicable, result);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task InitializeTests(bool isSdk)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var step = mock.Create<TryConvertProjectConverterStep>();

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(m => m.IsSdk).Returns(isSdk);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(m => m.CurrentProject).Returns(project.Object);

            // Act
            await step.InitializeAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            var expected = isSdk ? UpgradeStepStatus.Complete : UpgradeStepStatus.Incomplete;
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
            step.SetStatus(UpgradeStepStatus.Incomplete);

            var project = mock.Mock<IProject>();

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(m => m.CurrentProject).Returns(project.Object);

            mock.Mock<ITryConvertTool>().Setup(m => m.RunAsync(context.Object, project.Object, default)).ReturnsAsync(isSuccess);

            // Act
            await step.ApplyAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            var expected = isSuccess ? UpgradeStepStatus.Complete : UpgradeStepStatus.Failed;
            Assert.Equal(expected, step.Status);
        }
    }
}
