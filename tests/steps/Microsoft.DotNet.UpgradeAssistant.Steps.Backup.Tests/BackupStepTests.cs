// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Cli;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Backup.Tests
{
    public class BackupStepTests
    {
        [Theory]
        [InlineData("A\\A.sln", "A\\Project1\\A.csproj", false, "A.backup\\Project1")]
        [InlineData("A\\A.csproj", "A\\A.csproj", false, "A.backup.2")]
        [InlineData("A\\A.sln", "A\\A.csproj", false, "A.backup\\A.0")]
        [InlineData("B\\B.sln", "B\\Project1\\B.csproj", false, "B.backup\\Project1")]
        [InlineData("B\\B.csproj", "B\\B.csproj", true, "B.backup")]
        [InlineData("B\\B.sln", "B\\B.csproj", true, "B.backup\\B.0")]
        public async Task InitializeTests(string inputPath, string projectPath, bool backupComplete, string expectedBackupPath)
        {
            // Arrange
            inputPath = Path.GetFullPath(Path.Combine("TestAssets", inputPath));
            projectPath = Path.GetFullPath(Path.Combine("TestAssets", projectPath));
            expectedBackupPath = Path.GetFullPath(Path.Combine("TestAssets", expectedBackupPath));

            using var mock = GetMock(GetDefaultNonInteractiveOptions());
            var context = GetContext(mock, inputPath, projectPath);
            var step = mock.Create<BackupStep>();
            var expectedStatus = backupComplete ? UpgradeStepStatus.Complete : UpgradeStepStatus.Incomplete;
            var expectedStatusDetails = backupComplete
                ? $"Existing backup found at {expectedBackupPath}"
                : $"No existing backup found. Applying this step will copy the contents of {Path.GetDirectoryName(projectPath)} (including subfolders) to {expectedBackupPath}";

            // Act
            await step.InitializeAsync(context, CancellationToken.None).ConfigureAwait(true);
            var status = step.Status;
            var statusDetails = step.StatusDetails;

            // Assert
            Assert.Equal(expectedStatus, step.Status);
            Assert.Equal(expectedStatusDetails, step.StatusDetails);
            Assert.Equal(BuildBreakRisk.None, step.Risk);

            var properties = mock.Container.Resolve<IUpgradeContextProperties>();
            Assert.Empty(properties.GetAllPropertyValues());
        }

        [Theory]
        [InlineData("A\\A.sln", "A\\Project1\\A.csproj", false, "A.backup", "A.backup\\Project1")]
        [InlineData("A\\A.csproj", "A\\A.csproj", false, "A.backup.2", "A.backup.2")]
        [InlineData("A\\A.sln", "A\\A.csproj", false, "A.backup", "A.backup\\A.0")]
        [InlineData("B\\B.sln", "B\\Project1\\B.csproj", false, "B.backup", "B.backup\\Project1")]
        [InlineData("B\\B.csproj", "B\\B.csproj", true, "B.backup", "B.backup")]
        [InlineData("B\\B.sln", "B\\B.csproj", true, "B.backup", "B.backup\\B.0")]
        public async Task ApplyTests(string inputPath, string projectPath, bool backupComplete, string expectedBaseBackupPath, string expectedBackupPath)
        {
            // Arrange
            inputPath = Path.GetFullPath(Path.Combine("TestAssets", inputPath));
            projectPath = Path.GetFullPath(Path.Combine("TestAssets", projectPath));
            expectedBaseBackupPath = Path.GetFullPath(Path.Combine("TestAssets", expectedBaseBackupPath));
            expectedBackupPath = Path.GetFullPath(Path.Combine("TestAssets", expectedBackupPath));

            using var mock = GetMock(GetDefaultNonInteractiveOptions());
            var context = GetContext(mock, inputPath, projectPath);
            var step = mock.Create<BackupStep>();
            step.SetStatus(UpgradeStepStatus.Incomplete);
            var expectedStatus = UpgradeStepStatus.Complete;
            var expectedStatusDetails = backupComplete
                ? $"Backup already exists at {expectedBackupPath}; nothing to do"
                : $"Project backed up to {expectedBackupPath}";

            // Act
            try
            {
                await step.ApplyAsync(context, CancellationToken.None).ConfigureAwait(true);
                var status = step.Status;
                var statusDetails = step.StatusDetails;

                // Assert
                Assert.Equal(expectedStatus, step.Status);
                Assert.Equal(expectedStatusDetails, step.StatusDetails);

                var properties = mock.Container.Resolve<IUpgradeContextProperties>();
                Assert.Single(properties.GetAllPropertyValues());
                Assert.Equal(expectedBaseBackupPath, properties.GetPropertyValue("BaseBackupLocation"));
            }
            finally
            {
                if (!backupComplete)
                {
                    Directory.Delete(expectedBackupPath, true);
                }
            }
        }

        private static UpgradeOptions GetDefaultNonInteractiveOptions() =>
            new()
            {
                NonInteractive = true,
                NonInteractiveWait = 0,
                SkipBackup = false,
            };

        private static AutoMock GetMock(UpgradeOptions options)
        {
            var mock = AutoMock.GetLoose(cfg =>
            {
                cfg.RegisterInstance(options);
                cfg.RegisterInstance<IUpgradeContextProperties>(new UpgradeContextProperties());
                cfg.RegisterType<NonInteractiveUserInput>().As<IUserInput>();
            });

            return mock;
        }

        private static IUpgradeContext GetContext(AutoMock mock, string inputPath, string? projectPath)
        {
            var context = new Mock<IUpgradeContext>();
            context.Setup(c => c.InputPath).Returns(inputPath);
            context.Setup(c => c.InputIsSolution).Returns(Path.GetExtension(inputPath).Equals(".sln", StringComparison.OrdinalIgnoreCase));
            context.Setup(c => c.Properties).Returns(mock.Container.Resolve<IUpgradeContextProperties>());

            if (projectPath is not null)
            {
                var project = mock.Mock<IProject>();
                project.Setup(p => p.FileInfo).Returns(new FileInfo(projectPath));
                context.Setup(c => c.CurrentProject).Returns(project.Object);
            }

            return context.Object;
        }
    }
}
