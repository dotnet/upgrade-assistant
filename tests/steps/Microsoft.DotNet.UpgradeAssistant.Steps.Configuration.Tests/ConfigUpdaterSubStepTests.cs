// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            // Arrange & Act
            (_, var step) = GetMockContextAndStep(new[] { "a" }, true, new[] { new TestConfigUpdater(BuildBreakRisk.Medium, false) });

            // Assert
            Assert.Equal(typeof(ConfigUpdaterStep).FullName, step.ParentStep!.Id);
            Assert.Equal(BuildBreakRisk.Unknown, step.Risk);
            Assert.Equal("Test title", step.Title);
            Assert.Equal("Test description", step.Description);
            Assert.Equal("Test ConfigUpdater", step.Id);
            Assert.Equal(Array.Empty<string>(), step.DependsOn);
            Assert.Equal(Array.Empty<string>(), step.DependencyOf);
            Assert.Equal(UpgradeStepStatus.Unknown, step.Status);
            Assert.False(step.IsDone);
        }

        [Fact]
        public void NegativeCtorTests()
        {
            var goodParent = new ConfigUpdaterStep(Enumerable.Empty<IUpdater<ConfigFile>>(), new ConfigUpdaterOptions(), new NullLogger<ConfigUpdaterStep>());
            var badParent = new TestUpgradeStep("Test step");

            Assert.Throws<ArgumentNullException>("parentStep", () => new ConfigUpdaterSubStep(null!, new Mock<IUpdater<ConfigFile>>().Object, new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("parentStep", () => new ConfigUpdaterSubStep(badParent, new Mock<IUpdater<ConfigFile>>().Object, new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("configUpdater", () => new ConfigUpdaterSubStep(goodParent, null!, new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("logger", () => new ConfigUpdaterSubStep(goodParent, new Mock<IUpdater<ConfigFile>>().Object, null!));
        }

        [Theory]
        [MemberData(nameof(IsApplicableData))]
        public async Task IsApplicableTests(bool projectLoaded, ProjectComponents? projectComponents, IUpdater<ConfigFile> updater, bool expectedResult)
        {
            // Arrange
            (var context, var step) = GetMockContextAndStep(new[] { "a" }, projectLoaded, new[] { updater }, projectComponents);

            // Act
            var result = await step.IsApplicableAsync(context, default).ConfigureAwait(false);

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
                new object?[] { true, ProjectComponents.WinRT | ProjectComponents.AspNetCore, new WebWinRTTestConfigUpdater(BuildBreakRisk.None, false), true },

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
            IUpdater<ConfigFile> updater = failingUpdater
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

            IUpdater<ConfigFile> GetUpdater(bool succeeds) =>
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
                                                                                         IUpdater<ConfigFile>[] updaters,
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
                    cfg.RegisterInstance(updater).As<IUpdater<ConfigFile>>();
                }
            });

            var project = mock.Mock<IProject>();
            project.Setup(p => p.FileInfo).Returns(new FileInfo("./test"));
            if (projectComponents.HasValue)
            {
                project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(projectComponents.Value));
            }

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(projectLoaded ? project.Object : null);
            var parentStep = mock.Create<ConfigUpdaterStep>();

            return (context.Object, (ConfigUpdaterSubStep)parentStep.SubSteps.First());
        }
    }
}
