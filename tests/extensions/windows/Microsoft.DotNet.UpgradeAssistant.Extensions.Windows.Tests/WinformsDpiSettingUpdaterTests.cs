// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows.Tests
{
    public class WinformsDpiSettingUpdaterTests
    {
        [InlineData("TestInputFiles/HighDpiHappyPathInput.txt", false, true, "TestExpectedFiles/HighDpiHappyPathExpected.txt")] // Happy Path
        [InlineData("TestInputFiles/HighDpiNoUpdateInput.txt", true, false, "TestExpectedFiles/HighDpiEmptyExpected.txt")] // Do Nothing Scenario
        [InlineData("TestInputFiles/HighDpiEmptyInput.txt", false, false, "TestExpectedFiles/HighDpiEmptyExpected.txt")] // No Program.cs
        [InlineData("TestInputFiles/HighDpiEmptyInput.txt", true, false, "TestExpectedFiles/HighDpiEmptyExpected.txt")] // Negative Test
        [InlineData("TestInputFiles/HighDpiNoNewLineAddedInput.txt", false, true, "TestExpectedFiles/HighDpiNoNewLineAddedExpected.txt")] // No new line added since existing line does not exist
        [Theory]
        public async Task UpdateDpiSettingTests(string inputFilePath, bool isDpiSettingSetInProgramFile, bool expectedOutputFile, string expectedFilePath)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var random = new Random();

            var logger = mock.Mock<ILogger<WinformsDpiSettingUpdater>>();
            var updater = new WinformsDpiSettingUpdater(logger.Object);

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.FileInfo).Returns(new FileInfo("./test"));

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });

            var programFilePath = Path.Combine(Path.GetTempPath(), string.Concat("TestFile", random.Next(), ".txt"));
            var programFileContent = File.ReadAllLines(inputFilePath);
            var expectedFileContent = File.ReadAllLines(expectedFilePath);

            // Act
            await updater.UpdateHighDPISetting(project.Object, programFileContent, isDpiSettingSetInProgramFile, programFilePath);

            // Assert
            Assert.Equal(File.Exists(programFilePath), expectedOutputFile);
            if (File.Exists(programFilePath))
            {
                Assert.Equal(File.ReadAllLines(programFilePath), expectedFileContent);
                File.Delete(programFilePath);
            }
        }
    }
}
