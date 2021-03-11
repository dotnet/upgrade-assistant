// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Commands;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class CommandTests
    {
        [Fact]
        public async Task NegativeTests()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplyNextCommand(null!));
            Assert.Throws<ArgumentNullException>(() => new SkipNextCommand(null!));
            Assert.Throws<ArgumentNullException>(() => new ExitCommand(null!));

            // Applying before intialization throws
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;
            var step = new TestUpgradeStep(string.Empty);
            var command = new ApplyNextCommand(step);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ApplyNextCommandAppliesSteps()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;

            var stepTitle = "Test step!";
            var step = new TestUpgradeStep(stepTitle);
            var command = new ApplyNextCommand(step);

            // Initialize step
            Assert.Equal(UpgradeStepStatus.Unknown, step.Status);
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(UpgradeStepStatus.Incomplete, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);

            // Apply command
            Assert.Equal(0, step.ApplicationCount);
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm command text and step state are as expected
            Assert.Equal($"Apply next step ({stepTitle})", command.CommandText);
            Assert.Equal(1, step.ApplicationCount);
            Assert.Equal(UpgradeStepStatus.Complete, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);
            Assert.Equal(step.AppliedMessage, step.StatusDetails);

            // Confirm steps are only applied once
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));
            Assert.Equal(1, step.ApplicationCount);
        }

        [Fact]
        public async Task FailedStepsAreNotApplied()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;

            var stepTitle = "Failed test step!";
            var step = new FailedTestUpgradeStep(stepTitle);
            var command = new ApplyNextCommand(step);

            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Apply command
            Assert.Equal(0, step.ApplicationCount);
            Assert.False(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm step was not applied
            Assert.Equal($"Apply next step ({stepTitle})", command.CommandText);
            Assert.Equal(0, step.ApplicationCount);
            Assert.Equal(UpgradeStepStatus.Failed, step.Status);
            Assert.Equal(BuildBreakRisk.Unknown, step.Risk);
            Assert.Equal(step.InitializedMessage, step.StatusDetails);
        }

        [Fact]
        public async Task SkipNextCommandSkips()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;

            var stepTitle = "Test step!";
            var step = new TestUpgradeStep(stepTitle);
            var command = new SkipNextCommand(step);

            // Initialize step
            Assert.Equal(UpgradeStepStatus.Unknown, step.Status);
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(UpgradeStepStatus.Incomplete, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);

            // Apply command
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm command text and step state are as expected
            Assert.Equal($"Skip next step ({stepTitle})", command.CommandText);
            Assert.Equal(0, step.ApplicationCount);
            Assert.Equal(UpgradeStepStatus.Skipped, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);
            Assert.Equal("Step skipped", step.StatusDetails);
        }

        [Fact]
        public async Task ExitCommandExits()
        {
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>().Object;

            var exitCalled = false;
            var command = new ExitCommand(() => exitCalled = true);

            Assert.Equal("Exit", command.CommandText);
            Assert.False(exitCalled);
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));
            Assert.True(exitCalled);
        }

        [Fact]
        public async Task SelectProjectCommandClearsProject()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var context = mock.Mock<IUpgradeContext>();

            var command = new SelectProjectCommand();

            // Act
            var result = await command.ExecuteAsync(context.Object, CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.True(result);
            Assert.Equal("Select different project", command.CommandText);
            Assert.True(command.IsEnabled);
            context.Verify(c => c.SetCurrentProject(null), Times.Once);
        }

        [Fact]
        public async Task NegativeSelectProjectCommandTests()
        {
            var command = new SelectProjectCommand();

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => command.ExecuteAsync(null!, CancellationToken.None)).ConfigureAwait(true);
        }
    }
}
