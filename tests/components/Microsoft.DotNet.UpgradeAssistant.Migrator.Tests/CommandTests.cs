using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Migrator.Commands;
using NSubstitute;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Migrator.Tests
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
            using var context = Substitute.For<IMigrationContext>();
            var step = new TestMigrationStep(string.Empty);
            var command = new ApplyNextCommand(step);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ApplyNextCommandAppliesSteps()
        {
            using var context = Substitute.For<IMigrationContext>();
            var stepTitle = "Test step!";
            var step = new TestMigrationStep(stepTitle);
            var command = new ApplyNextCommand(step);

            // Initialize step
            Assert.Equal(MigrationStepStatus.Unknown, step.Status);
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(MigrationStepStatus.Incomplete, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);

            // Apply command
            Assert.Equal(0, step.ApplicationCount);
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm command text and step state are as expected
            Assert.Equal($"Apply next step ({stepTitle})", command.CommandText);
            Assert.Equal(1, step.ApplicationCount);
            Assert.Equal(MigrationStepStatus.Complete, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);
            Assert.Equal(step.AppliedMessage, step.StatusDetails);

            // Confirm steps are only applied once
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));
            Assert.Equal(1, step.ApplicationCount);
        }

        [Fact]
        public async Task FailedStepsAreNotApplied()
        {
            using var context = Substitute.For<IMigrationContext>();
            var stepTitle = "Failed test step!";
            var step = new FailedTestMigrationStep(stepTitle);
            var command = new ApplyNextCommand(step);

            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);

            // Apply command
            Assert.Equal(0, step.ApplicationCount);
            Assert.False(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm step was not applied
            Assert.Equal($"Apply next step ({stepTitle})", command.CommandText);
            Assert.Equal(0, step.ApplicationCount);
            Assert.Equal(MigrationStepStatus.Failed, step.Status);
            Assert.Equal(BuildBreakRisk.Unknown, step.Risk);
            Assert.Equal(step.InitializedMessage, step.StatusDetails);
        }

        [Fact]
        public async Task SkipNextCommandSkips()
        {
            using var context = Substitute.For<IMigrationContext>();
            var stepTitle = "Test step!";
            var step = new TestMigrationStep(stepTitle);
            var command = new SkipNextCommand(step);

            // Initialize step
            Assert.Equal(MigrationStepStatus.Unknown, step.Status);
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(MigrationStepStatus.Incomplete, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);

            // Apply command
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));

            // Confirm command text and step state are as expected
            Assert.Equal($"Skip next step ({stepTitle})", command.CommandText);
            Assert.Equal(0, step.ApplicationCount);
            Assert.Equal(MigrationStepStatus.Skipped, step.Status);
            Assert.Equal(BuildBreakRisk.Low, step.Risk);
            Assert.Equal("Step skipped", step.StatusDetails);
        }

        [Fact]
        public async Task ExitCommandExits()
        {
            using var context = Substitute.For<IMigrationContext>();
            var exitCalled = false;
            var command = new ExitCommand(() => exitCalled = true);

            Assert.Equal("Exit", command.CommandText);
            Assert.False(exitCalled);
            Assert.True(await command.ExecuteAsync(context, CancellationToken.None).ConfigureAwait(false));
            Assert.True(exitCalled);
        }
    }
}
