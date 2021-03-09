// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Tests
{
    public class ConfigUpdaterStepTests
    {
        [Fact]
        public void PropertyTests()
        {
            // Arrange
            using var mock = AutoMock.GetLoose(cfg =>
            {
                cfg.RegisterInstance(new ConfigUpdaterOptions
                {
                    ConfigFilePaths = new[] { "a", "b" }
                });
                RegisterConfigUpdaters(cfg, 0, 2);
            });
            var step = mock.Create<ConfigUpdaterStep>();

            // Act
            var dependencyOf = step.DependencyOf;
            var dependsOn = step.DependsOn;
            var description = step.Description;
            var id = step.Id;
            var title = step.Title;
            var subSteps = step.SubSteps;

            // Assert
            Assert.Equal(new[] { "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep" }, dependencyOf);
            Assert.Equal(
                new[] { "Microsoft.DotNet.UpgradeAssistant.Steps.Backup.BackupStep", "Microsoft.DotNet.UpgradeAssistant.Steps.Templates.TemplateInserterStep" }.OrderBy(x => x),
                dependsOn.OrderBy(x => x));
            Assert.Equal("Update project based on settings in app config files (a, b)", description);
            Assert.Equal(typeof(ConfigUpdaterStep).FullName, id);
            Assert.Equal("Upgrade app config files", title);
            Assert.Equal(new[] { "ConfigUpdater #0", "ConfigUpdater #1" }, subSteps.Select(s => s.Id));
        }

        [Fact]
        public void NegativeCtorTests()
        {
            Assert.Throws<ArgumentNullException>("configUpdaters", () => new ConfigUpdaterStep(null!, new ConfigUpdaterOptions(), new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("configUpdaterOptions", () => new ConfigUpdaterStep(Enumerable.Empty<IConfigUpdater>(), null!, new NullLogger<ConfigUpdaterStep>()));
            Assert.Throws<ArgumentNullException>("logger", () => new ConfigUpdaterStep(Enumerable.Empty<IConfigUpdater>(), new ConfigUpdaterOptions(), null!));
        }

        [Theory]
        [InlineData(new[] { "TestConfig.xml" }, 1, true, true)] // Vanilla positive case
        [InlineData(new[] { "DoesNotExist.xml", "TestConfig.xml" }, 1, true, true)] // Some config files exist
        [InlineData(new[] { "DoesNotExist.xml" }, 1, true, false)] // Non-existent config file
        [InlineData(new[] { "TestConfig.xml" }, 0, true, false)] // No config updaters
        [InlineData(new[] { "TestConfig.xml" }, 1, false, false)] // No project loaded
        public void IsApplicableTests(string[] configPaths, int subStepCount, bool projectLoaded, bool expectedValue)
        {
            // Arrange
            (var context, var step) = GetMockContextAndStep(configPaths, projectLoaded, 0, subStepCount);

            // Act
            var result = step.IsApplicable(context);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Theory]
        [InlineData(new[] { "TestConfig.xml" }, new[] { "TestConfig.xml" }, 1, 1)] // Vanilla positive case
        [InlineData(new[] { "TestConfig.xml", "DoesNotExist" }, new[] { "TestConfig.xml" }, 2, 2)] // Non-existent config file
        [InlineData(new[] { "TestConfig.xml", "TestConfig.xml" }, new[] { "TestConfig.xml", "TestConfig.xml" }, 0, 1)] // Multiple config files
        [InlineData(new[] { "TestConfig.xml" }, new[] { "TestConfig.xml" }, 1, 0)] // Vanilla positive case, complete
        [InlineData(new[] { "TestConfig.xml", "DoesNotExist" }, new[] { "TestConfig.xml" }, 2, 0)] // Non-existent config file, complete
        [InlineData(new[] { "TestConfig.xml" }, new[] { "TestConfig.xml" }, 0, 0)] // No config updaters
        [InlineData(new string[0], new string[0], 2, 0)] // No config files
        [InlineData(new string[0], new string[0], 2, 1)] // No config files, incomplete
        public async Task InitializeTests(string[] configPaths, string[] expectedConfigPaths, int completeConfigUpdaterCount, int incompleteConfigUpdaterCount)
        {
            // Arrange
            (var context, var step) = GetMockContextAndStep(configPaths, true, completeConfigUpdaterCount, incompleteConfigUpdaterCount);

            // Act
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.Equal(expectedConfigPaths.Select(f => XDocument.Load(f, LoadOptions.SetLineInfo).ToString()).ToArray(), step.ConfigFiles.Select(c => c.Contents.ToString()).ToArray());
            Assert.Equal(expectedConfigPaths.Select(f => Path.Combine(".", f)).ToArray(), step.ConfigFiles.Select(c => c.Path).ToArray());
            if (incompleteConfigUpdaterCount > 0)
            {
                Assert.Equal(UpgradeStepStatus.Incomplete, step.Status);
                Assert.Equal($"{incompleteConfigUpdaterCount} config updaters need applied", step.StatusDetails);
                Assert.Equal(BuildBreakRisk.Medium, step.Risk);
            }
            else
            {
                Assert.Equal(UpgradeStepStatus.Complete, step.Status);
                Assert.Equal("No config updaters need applied", step.StatusDetails);
                Assert.Equal(BuildBreakRisk.None, step.Risk);
            }
        }

        [Fact]
        public async Task NegativeInitializeTests()
        {
            (var contextWithNoProject, var step1) = GetMockContextAndStep(Array.Empty<string>(), false, 0, 1);
            (var contextWithInvalidConfig, var step2) = GetMockContextAndStep(new[] { "Microsoft.DotNet.UpgradeAssistant.Steps.Configuration.Tests.dll" }, true, 0, 1);

            // Null context
            await Assert.ThrowsAsync<ArgumentNullException>("context", () => step1.InitializeAsync(null!, CancellationToken.None)).ConfigureAwait(true);

            // No project
            await step1.InitializeAsync(contextWithNoProject, CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(UpgradeStepStatus.Failed, step1.Status);

            // Non-xml config file
            await step2.InitializeAsync(contextWithInvalidConfig, CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(UpgradeStepStatus.Failed, step2.Status);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        public async Task ApplyTests(int completeConfigUpdaterCount, int incompleteConfigUpdaterCount)
        {
            // Arrange
            (var context, var step) = GetMockContextAndStep(Array.Empty<string>(), true, completeConfigUpdaterCount, incompleteConfigUpdaterCount);
            step.SetStatus(UpgradeStepStatus.Incomplete);
            foreach (var subStep in step.SubSteps)
            {
                await subStep.InitializeAsync(context, CancellationToken.None).ConfigureAwait(true);
            }

            // Act
            await step.ApplyAsync(context, CancellationToken.None).ConfigureAwait(true);

            // Assert
            if (incompleteConfigUpdaterCount > 0)
            {
                Assert.Equal(UpgradeStepStatus.Incomplete, step.Status);
                Assert.Equal($"{incompleteConfigUpdaterCount} config updaters need applied", step.StatusDetails);
            }
            else
            {
                Assert.Equal(UpgradeStepStatus.Complete, step.Status);
                Assert.Equal(string.Empty, step.StatusDetails);
            }
        }

        private static (IUpgradeContext Context, ConfigUpdaterStep Step) GetMockContextAndStep(string[] configPaths, bool projectLoaded, int completeConfigUpdaterCount, int incompleteConfigUpdaterCount)
        {
            using var mock = AutoMock.GetLoose(cfg =>
            {
                cfg.RegisterInstance(new ConfigUpdaterOptions
                {
                    ConfigFilePaths = configPaths
                });
                RegisterConfigUpdaters(cfg, completeConfigUpdaterCount, incompleteConfigUpdaterCount);
            });
            var project = mock.Mock<IProject>();
            project.Setup(p => p.Directory).Returns(".");
            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.CurrentProject).Returns(projectLoaded ? project.Object : null);
            var step = mock.Create<ConfigUpdaterStep>();

            return (context.Object, step);
        }

        private static void RegisterConfigUpdaters(ContainerBuilder builder, int completeCount, int incompleteCount)
        {
            if (completeCount + incompleteCount == 0)
            {
                builder.RegisterInstance(Enumerable.Empty<IConfigUpdater>());
            }
            else
            {
                for (var i = 0; i < completeCount + incompleteCount; i++)
                {
                    var mock = new Mock<IConfigUpdater>();
                    mock.Setup(c => c.Id).Returns($"ConfigUpdater #{i}");
                    mock.Setup(c => c.IsApplicableAsync(It.IsAny<IUpgradeContext>(),
                                                        It.IsAny<ImmutableArray<ConfigFile>>(),
                                                        It.IsAny<CancellationToken>())).Returns(Task.FromResult(i >= completeCount));
                    mock.Setup(c => c.Risk).Returns(BuildBreakRisk.Medium);
                    builder.RegisterMock(mock);
                }
            }
        }
    }
}
