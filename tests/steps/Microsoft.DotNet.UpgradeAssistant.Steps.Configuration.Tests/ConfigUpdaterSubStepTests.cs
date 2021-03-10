// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Tests
{
    public class ConfigUpdaterSubStepTests
    {
        [Fact]
        public void CtorTests()
        {
            // Arrange
            (_, var step) = GetMockContextAndStep(new[] { "a" }, true, new[] { new TestConfigUpdater(BuildBreakRisk.Medium, false) });

            // Act
            var risk = step.Risk;
            var parentId = step.ParentStep!.Id;
            var title = step.Title;
            var description = step.Description;
            var id = step.Id;
            var dependsOn = step.DependsOn;
            var dependencyOf = step.DependencyOf;
            var status = step.Status;
            var done = step.IsDone;

            // Assert
            Assert.Equal(typeof(ConfigUpdaterStep).FullName, parentId);
            Assert.Equal(BuildBreakRisk.Unknown, risk);
            Assert.Equal("Test title", title);
            Assert.Equal("Test description", description);
            Assert.Equal("Test ConfigUpdater", id);
            Assert.Equal(Array.Empty<string>(), dependsOn);
            Assert.Equal(Array.Empty<string>(), dependencyOf);
            Assert.Equal(UpgradeStepStatus.Unknown, status);
            Assert.False(done);
        }

        [Fact]
        public void NegativeCtorTests()
        {
            var goodParent = new ConfigUpdaterStep(Enumerable.Empty<IConfigUpdater>(), new ConfigUpdaterOptions(), new NullLogger<ConfigUpdaterStep>());
            var badParent = new TestUpgradeStep("Test step");

            Assert.Throws<ArgumentNullException>("parentStep", () => new ConfigUpdaterSubStep(null!, new Mock<IConfigUpdater>().Object, new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("parentStep", () => new ConfigUpdaterSubStep(badParent, new Mock<IConfigUpdater>().Object, new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("configUpdater", () => new ConfigUpdaterSubStep(goodParent, null!, new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("logger", () => new ConfigUpdaterSubStep(goodParent, new Mock<IConfigUpdater>().Object, null!));
        }

        [Theory]
        [MemberData(nameof(IsApplicableData))]
        public void IsApplicableTests(bool projectLoaded, ProjectComponents? projectComponents, IConfigUpdater updater, bool expectedResult)
        {
            // Arrange
            (var context, var step) = GetMockContextAndStep(new[] { "a" }, projectLoaded, new[] { updater }, projectComponents);

            // Act
            var result = step.IsApplicable(context);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        public static IEnumerable<object?[]> IsApplicableData =>
            new List<object?[]>
            {
                // Vanilla positive
                new object?[] { true, null, new TestConfigUpdater(BuildBreakRisk.None, false), true },

                // No project loaded
                new object?[] { false, null, new TestConfigUpdater(BuildBreakRisk.None, false), false },

                // Has more components than required
                new object?[] { true, ProjectComponents.WinForms, new TestConfigUpdater(BuildBreakRisk.None, false), true },

                // Satisfies 'None' component requirement
                new object?[] { true, ProjectComponents.WindowsDesktop, new NoneConfigUpdater(BuildBreakRisk.None, false), true },
                new object?[] { true, null, new NoneConfigUpdater(BuildBreakRisk.None, false), true },

                // Null components
                new object?[] { true, null, new WebWinRTTestConfigUpdater(BuildBreakRisk.None, false), false },

                // Missing components
                new object?[] { true, ProjectComponents.WinRT | ProjectComponents.WinForms, new WebWinRTTestConfigUpdater(BuildBreakRisk.None, false), false },

                // Components satisfied
                new object?[] { true, ProjectComponents.WinRT | ProjectComponents.Web, new WebWinRTTestConfigUpdater(BuildBreakRisk.None, false), true },

                // Invalid components
                new object?[] { true, (ProjectComponents)2048, new WebWinRTTestConfigUpdater(BuildBreakRisk.None, false), false }
            };

        [Theory]
        [InlineData(BuildBreakRisk.High, true, false, UpgradeStepStatus.Incomplete)]
        [InlineData(BuildBreakRisk.Low, true, false, UpgradeStepStatus.Incomplete)]
        [InlineData(BuildBreakRisk.High, false, false, UpgradeStepStatus.Complete)]
        [InlineData(BuildBreakRisk.High, true, true, UpgradeStepStatus.Failed)]
        public async Task InitializeTests(BuildBreakRisk risk, bool isApplicable, bool failingUpdater, UpgradeStepStatus expectedStatus)
        {
            // Arrange
            IConfigUpdater updater = failingUpdater
                ? new FailingConfigUpdater(risk)
                : new TestConfigUpdater(risk, isApplicable);
            (var context, var step) = GetMockContextAndStep(new[] { "TestConfig.xml" }, true, new[] { updater });

            // Act
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.Equal(expectedStatus, step.Status);

            if (expectedStatus == UpgradeStepStatus.Complete)
            {
                Assert.Equal(string.Empty, step.StatusDetails);
                Assert.Equal(BuildBreakRisk.None, step.Risk);
            }

            if (expectedStatus == UpgradeStepStatus.Incomplete)
            {
                Assert.Equal($"Config updater \"{updater.Title}\" needs applied", step.StatusDetails);
                Assert.Equal(updater.Risk, step.Risk);
            }

            if (expectedStatus == UpgradeStepStatus.Failed)
            {
                Assert.StartsWith($"Unexpected exception while initializing config updater \"{updater.Title}\":", step.StatusDetails, StringComparison.Ordinal);
            }
        }

        [Fact]
        public async Task NegativeInitializeTests()
        {
            (_, var step) = GetMockContextAndStep(new[] { "TestConfig.xml" }, true, new[] { new TestConfigUpdater(BuildBreakRisk.Medium, false) });

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => step.InitializeAsync(null!, CancellationToken.None)).ConfigureAwait(true);
        }

        [Theory]
        [InlineData(true, new[] { true })] // Update throws exception during Apply
        [InlineData(false, new[] { true })] // Update succeeds during Apply
        [InlineData(false, new[] { false })] // Update fails during Apply
        [InlineData(false, new[] { true, true })] // Update succeeds during Apply but other updates still need applied
        public async Task ApplyTests(bool failingUpdater, bool[] updaterApplicability)
        {
            // Arrange
            var updaters = updaterApplicability.Select(b => GetUpdater(b)).ToArray();
            (var context, var step) = GetMockContextAndStep(new[] { "TestConfig.xml" }, true, updaters);
            step.SetStatus(UpgradeStepStatus.Incomplete);
            step.ParentStep!.SetStatus(UpgradeStepStatus.Incomplete);

            // Act
            await step.ApplyAsync(context, CancellationToken.None).ConfigureAwait(true);

            // Assert
            if (failingUpdater)
            {
                Assert.Equal(UpgradeStepStatus.Failed, step.Status);
                Assert.StartsWith($"Unexpected exception while applying config updater \"Test title\":", step.StatusDetails, StringComparison.Ordinal);
            }
            else
            {
                var updater = (TestConfigUpdater)updaters.First();
                Assert.Equal(1, updater.ApplyCount);

                if (updater.IsApplicable)
                {
                    Assert.Equal(UpgradeStepStatus.Complete, step.Status);
                    Assert.Equal(string.Empty, step.StatusDetails);

                    // Confirm that the parent step is auto-completed iff all sub-steps are complete
                    if (updaters.Length == 1)
                    {
                        Assert.Equal(UpgradeStepStatus.Complete, step.ParentStep!.Status);
                    }
                    else
                    {
                        Assert.Equal(UpgradeStepStatus.Incomplete, step.ParentStep!.Status);
                    }
                }
                else
                {
                    Assert.Equal(UpgradeStepStatus.Failed, step.Status);
                    Assert.Equal($"Failed to apply config updater \"{updater.Title}\"", step.StatusDetails);

                    // Confirm the parent step is not completed since this substep was not completed
                    Assert.Equal(UpgradeStepStatus.Incomplete, step.ParentStep!.Status);
                }
            }

            IConfigUpdater GetUpdater(bool succeeds) =>
                failingUpdater ? new FailingConfigUpdater(BuildBreakRisk.High) : new TestConfigUpdater(BuildBreakRisk.Medium, succeeds);
        }

        [Fact]
        public async Task NegativeApplyTests()
        {
            (_, var step) = GetMockContextAndStep(new[] { "TestConfig.xml" }, true, new[] { new TestConfigUpdater(BuildBreakRisk.Medium, false) });

            await Assert.ThrowsAsync<ArgumentNullException>("context", () => step.ApplyAsync(null!, CancellationToken.None)).ConfigureAwait(true);
        }

        private static (IUpgradeContext C, ConfigUpdaterSubStep S) GetMockContextAndStep(string[] configPaths,
                                                                                         bool projectLoaded,
                                                                                         IConfigUpdater[] updaters,
                                                                                         ProjectComponents? projectComponents = null)
        {
            using var mock = AutoMock.GetLoose(cfg =>
            {
                cfg.RegisterInstance(new ConfigUpdaterOptions
                {
                    ConfigFilePaths = configPaths
                });

                foreach (var updater in updaters)
                {
                    cfg.RegisterInstance(updater).As<IConfigUpdater>();
                }
            });

            var project = mock.Mock<IProject>();
            project.Setup(p => p.Directory).Returns(".");
            if (projectComponents.HasValue)
            {
                project.Setup(p => p.Components).Returns(projectComponents.Value);
            }

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(projectLoaded ? project.Object : null);
            var parentStep = mock.Create<ConfigUpdaterStep>();

            return (context.Object, (ConfigUpdaterSubStep)parentStep.SubSteps.First());
        }
    }
}
