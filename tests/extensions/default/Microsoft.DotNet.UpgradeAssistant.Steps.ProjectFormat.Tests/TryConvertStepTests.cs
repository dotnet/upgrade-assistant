// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert.Tests
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

            Assert.Equal(new[] { WellKnownStepIds.BackupStepId }, step.DependsOn);
            Assert.Equal(new[] { WellKnownStepIds.NextProjectStepId }, step.DependencyOf);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task IsApplicable(bool isApplicable)
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
            var result = await step.IsApplicableAsync(context.Object, default).ConfigureAwait(false);

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
            var context = mock.Mock<IUpgradeContext>();

            // Act
            await step.InitializeAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(UpgradeStepStatus.Failed, step.Status);
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
            context.Setup(c => c.Results).Returns(new Mock<ICollector<OutputResultDefinition>>().Object);

            mock.Mock<ITryConvertTool>().Setup(m => m.RunAsync(context.Object, project.Object, default)).ReturnsAsync(isSuccess);

            // Act
            await step.ApplyAsync(context.Object, default).ConfigureAwait(false);

            // Assert
            var expected = isSuccess ? UpgradeStepStatus.Complete : UpgradeStepStatus.Failed;
            Assert.Equal(expected, step.Status);
        }
    }
}
