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

namespace Microsoft.DotNet.UpgradeAssistant.Migrator.Test
{
    public class MigratorTests
    {
        [Fact]
        public async Task NegativeTests()
        {
            var unknownStep = new[] { new UnknownTestMigrationStep("Unknown step") };
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(unknownStep)));

            var migrator = mock.Create<MigratorManager>();
            var context = mock.Mock<IMigrationContext>().Object;

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await migrator.GetNextStepAsync(context, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task MigratorStepsEnumeration()
        {
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(GetMigrationSteps())));

            var migrator = mock.Create<MigratorManager>();

            var expectedTopLevelStepsAndSubSteps = new[]
            {
                ("Step 1", 3),
                ("Step 2", 0),
                ("Step 3", 1)
            };

            // Substeps should be enumerated before their parents
            var expectAllSteps = new[]
            {
                "Substep 1", "Subsubstep 1", "Subsubstep 2", "Substep 2", "Substep 3", "Step 1", "Step 2", "Substep A", "Step 3"
            };

            // Get both the steps property and the GetAllSteps enumeration and confirm that both return steps
            // in the expected order
            using var context = mock.Mock<IMigrationContext>().Object;

            var steps = migrator.GetStepsForContext(context);
            var allSteps = new List<string>();

            var nextStep = await migrator.GetNextStepAsync(context, CancellationToken.None).ConfigureAwait(false);
            while (nextStep is not null)
            {
                allSteps.Add(nextStep.Title);
                await nextStep.ApplyAsync(context, CancellationToken.None).ConfigureAwait(false);
                nextStep = await migrator.GetNextStepAsync(context, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.Equal(expectedTopLevelStepsAndSubSteps, steps.Select(s => (s.Title, s.SubSteps.Count())).ToArray());
            Assert.Equal(expectAllSteps, allSteps);
        }

        [MemberData(nameof(CompletedStepsAreNotEnumeratedData))]
        [Theory]
        public async Task CompletedStepsAreNotEnumerated(MigrationStep[] steps, string[] expectedSteps)
        {
            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(steps)));
            var migrator = mock.Create<MigratorManager>();
            using var context = mock.Mock<IMigrationContext>().Object;

            var allSteps = new List<string>();

            var nextStep = await migrator.GetNextStepAsync(context, CancellationToken.None).ConfigureAwait(false);
            while (nextStep is not null)
            {
                allSteps.Add(nextStep.Title);
                await nextStep.ApplyAsync(context, CancellationToken.None).ConfigureAwait(false);
                nextStep = await migrator.GetNextStepAsync(context, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.Equal(expectedSteps, allSteps);
        }

        [Fact]
        public async Task FailedStepsAreEnumerated()
        {
            var steps = new MigrationStep[] { new SkippedTestMigrationStep("Step 1"), new FailedTestMigrationStep("Step 2"), new CompletedTestMigrationStep("Step 3") };
            var expectedNextStepId = "Step 2";

            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(GetOrderer(steps)));
            var migrator = mock.Create<MigratorManager>();
            using var context = mock.Mock<IMigrationContext>().Object;

            var nextStep = await migrator.GetNextStepAsync(context, CancellationToken.None).ConfigureAwait(false);

            // The failed step is next
            Assert.Equal(expectedNextStepId, nextStep?.Title);
            Assert.False(await nextStep!.ApplyAsync(context, CancellationToken.None).ConfigureAwait(false));

            // The failed step is still next after failing again
            Assert.Equal(expectedNextStepId, nextStep?.Title);
        }

        public static IEnumerable<object[]> CompletedStepsAreNotEnumeratedData =>
            new[]
            {
                // Incomplete steps are enumerated
                new object[]
                {
                    new[] { new TestMigrationStep("Step 1"), new TestMigrationStep("Step 2"), new TestMigrationStep("Step 3") },
                    new[] { "Step 1", "Step 2", "Step 3" }
                },

                // Completed steps are not enumerated
                new object[]
                {
                    new[] { new TestMigrationStep("Step 1"), new CompletedTestMigrationStep("Step 2"), new TestMigrationStep("Step 3") },
                    new[] { "Step 1", "Step 3" }
                },

                // Skipped steps are not enumerated
                new object[]
                {
                    new[] { new SkippedTestMigrationStep("Step 1"), new TestMigrationStep("Step 2"), new CompletedTestMigrationStep("Step 3") },
                    new[] { "Step 2" }
                },

                // Make sure enumerating an empty step list doesn't cause problems
                new object[]
                {
                    Array.Empty<MigrationStep>(),
                    Array.Empty<string>()
                }
            };

        private static MigrationStep[] GetMigrationSteps()
        {
            var subsubsteps = new[]
            {
                new TestMigrationStep("Subsubstep 1"),
                new TestMigrationStep("Subsubstep 2")
            };

            var substeps = new[]
            {
                new TestMigrationStep("Substep 1"),
                new TestMigrationStep("Substep 2", subSteps: subsubsteps),
                new TestMigrationStep("Substep 3")
            };

            var otherSubsteps = new[]
            {
                new TestMigrationStep("Substep A")
            };

            return new[]
            {
                new TestMigrationStep("Step 1", subSteps: substeps),
                new TestMigrationStep("Step 2"),
                new TestMigrationStep("Step 3", subSteps: otherSubsteps)
            };
        }

        private static IMigrationStepOrderer GetOrderer(IEnumerable<MigrationStep> steps) => new MigrationStepOrderer(steps, new NullLogger<MigrationStepOrderer>());
    }
}
