// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class UpgraderTests
    {
        [Fact]
        public async Task NegativeTests()
        {
            var unknownStep = new[] { new UnknownTestUpgradeStep("Unknown step") };
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(unknownStep)));

            var upgrader = mock.Create<UpgraderManager>();
            var context = mock.Mock<IUpgradeContext>().Object;

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await upgrader.GetNextStepAsync(context, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpgraderStepsEnumeration()
        {
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(GetUpgradeSteps())));

            var upgrader = mock.Create<UpgraderManager>();

            var expectedTopLevelStepsAndSubSteps = new[]
            {
                ("Step 1", 3),
                ("Step 2", 0),
                ("Step 3", 1),
            };

            // Substeps should be enumerated before their parents
            var expectAllSteps = new[]
            {
                "Substep 1", "Subsubstep 1", "Subsubstep 2", "Substep 2", "Substep 3", "Step 1", "Step 2", "Substep A", "Step 3",
            };

            // Get both the steps property and the GetAllSteps enumeration and confirm that both return steps
            // in the expected order
            var context = mock.Mock<IUpgradeContext>();
            context.SetupProperty(c => c.CurrentStep);

            var steps = upgrader.AllSteps;
            var allSteps = new List<string>();

            var nextStep = await upgrader.GetNextStepAsync(context.Object, CancellationToken.None).ConfigureAwait(false);
            while (nextStep is not null)
            {
                allSteps.Add(nextStep.Title);
                await nextStep.ApplyAsync(context.Object, CancellationToken.None).ConfigureAwait(false);
                nextStep = await upgrader.GetNextStepAsync(context.Object, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.Equal(expectedTopLevelStepsAndSubSteps, steps.Select(s => (s.Title, s.SubSteps.Count())).ToArray());
            Assert.Equal(expectAllSteps, allSteps);
        }

        [MemberData(nameof(CompletedStepsAreNotEnumeratedData))]
        [Theory]
        public async Task CompletedStepsAreNotEnumerated(UpgradeStep[] steps, string[] expectedSteps)
        {
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(steps)));
            var upgrader = mock.Create<UpgraderManager>();

            mock.Mock<IUserInput>().Setup(u => u.IsInteractive).Returns(true);

            var context = mock.Mock<IUpgradeContext>();
            context.SetupProperty(c => c.CurrentStep);

            var allSteps = new List<string>();

            var nextStep = await upgrader.GetNextStepAsync(context.Object, CancellationToken.None).ConfigureAwait(false);
            while (nextStep is not null)
            {
                allSteps.Add(nextStep.Title);
                await nextStep.ApplyAsync(context.Object, CancellationToken.None).ConfigureAwait(false);
                nextStep = await upgrader.GetNextStepAsync(context.Object, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.Equal(expectedSteps, allSteps);
        }

        [Fact]
        public async Task FailedStepsAreEnumerated()
        {
            var steps = new UpgradeStep[] { new SkippedTestUpgradeStep("Step 1"), new FailedTestUpgradeStep("Step 2"), new CompletedTestUpgradeStep("Step 3") };
            var expectedNextStepId = "Step 2";

            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(steps)));
            var upgrader = mock.Create<UpgraderManager>();

            mock.Mock<IUserInput>().Setup(u => u.IsInteractive).Returns(true);

            var context = mock.Mock<IUpgradeContext>();
            context.SetupProperty(c => c.CurrentStep);

            var nextStep = await upgrader.GetNextStepAsync(context.Object, CancellationToken.None).ConfigureAwait(false);

            // The failed step is next
            Assert.Equal(expectedNextStepId, nextStep?.Title);
            Assert.False(await nextStep!.ApplyAsync(context.Object, CancellationToken.None).ConfigureAwait(false));

            // The failed step is still next after failing again
            Assert.Equal(expectedNextStepId, nextStep?.Title);
        }

        public static IEnumerable<object[]> CompletedStepsAreNotEnumeratedData =>
            new[]
            {
                // Incomplete steps are enumerated
                new object[]
                {
                    new[] { new TestUpgradeStep("Step 1"), new TestUpgradeStep("Step 2"), new TestUpgradeStep("Step 3") },
                    new[] { "Step 1", "Step 2", "Step 3" },
                },

                // Completed steps are not enumerated
                new object[]
                {
                    new[] { new TestUpgradeStep("Step 1"), new CompletedTestUpgradeStep("Step 2"), new TestUpgradeStep("Step 3") },
                    new[] { "Step 1", "Step 3" },
                },

                // Skipped steps are not enumerated
                new object[]
                {
                    new[] { new SkippedTestUpgradeStep("Step 1"), new TestUpgradeStep("Step 2"), new CompletedTestUpgradeStep("Step 3") },
                    new[] { "Step 2" },
                },

                // Make sure enumerating an empty step list doesn't cause problems
                new object[]
                {
                    Array.Empty<UpgradeStep>(),
                    Array.Empty<string>(),
                },
            };

        private static UpgradeStep[] GetUpgradeSteps()
        {
            var subsubsteps = new[]
            {
                new TestUpgradeStep("Subsubstep 1"),
                new TestUpgradeStep("Subsubstep 2"),
            };

            var substeps = new[]
            {
                new TestUpgradeStep("Substep 1"),
                new TestUpgradeStep("Substep 2", subSteps: subsubsteps),
                new TestUpgradeStep("Substep 3"),
            };

            var otherSubsteps = new[]
            {
                new TestUpgradeStep("Substep A"),
            };

            return new[]
            {
                new TestUpgradeStep("Step 1", subSteps: substeps),
                new TestUpgradeStep("Step 2"),
                new TestUpgradeStep("Step 3", subSteps: otherSubsteps),
            };
        }

        private static IUpgradeStepOrderer GetOrderer(IEnumerable<UpgradeStep> steps) => new UpgradeStepOrderer(steps, new NullLogger<UpgradeStepOrderer>());
    }
}
