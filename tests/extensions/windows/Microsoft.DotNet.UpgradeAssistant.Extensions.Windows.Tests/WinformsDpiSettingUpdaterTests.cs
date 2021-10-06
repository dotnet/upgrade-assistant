// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.Tests
{
    public class WinformsDpiSettingUpdaterTests
    {
        [InlineData(new string[] { "Application.EnableVisualStyles();", "Application.SetCompatibleTextRenderingDefault(false);" }, false, true, new string[] { "Application.EnableVisualStyles();", "Application.SetHighDpiMode(HighDpiMode.SystemAware);", "Application.SetCompatibleTextRenderingDefault(false);" })] // Happy Path
        [InlineData(new string[] { "Application.EnableVisualStyles();", "Application.SetHighDpiMode(HighDpiMode.SystemAware);", "Application.SetCompatibleTextRenderingDefault(false);" }, true, false, new string[] { })] // Do Nothing Scenario
        [InlineData(new string[] { }, false, false, new string[] { })] // No Program.cs
        [InlineData(new string[] { }, true, false, new string[] { })] // Negative Test
        [InlineData(new string[] { "Application.SetCompatibleTextRenderingDefault(false);" }, false, true, new string[] { "Application.SetCompatibleTextRenderingDefault(false);" })] // No new line added since existing line does not exist
        [Theory]
        public async Task UpdateDpiSettingTests(string[] programFileContent, bool isDpiSettingSetInProgramFile, bool expectedProgramFile, string[] expectedFileContent)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var logger = mock.Mock<ILogger<WinformsDpiSettingUpdater>>();
            var updater = new WinformsDpiSettingUpdater(logger.Object);

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.FileInfo).Returns(new FileInfo("./test"));

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });

            var programFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Program.cs");

            // cleanup from previous test run
            if (File.Exists(programFilePath))
            {
                File.Delete(programFilePath);
            }

            // Act
            await updater.UpdateHighDPISetting(project.Object, programFileContent, isDpiSettingSetInProgramFile, programFilePath);

            // Assert
            Assert.Equal(File.Exists(programFilePath), expectedProgramFile);
            if (File.Exists(programFilePath))
            {
                Assert.Equal(File.ReadAllLines(programFilePath), expectedFileContent);
            }
        }
    }
}
