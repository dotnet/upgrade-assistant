// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater.Tests
{
    public class WCFUpdateStepTest
    {
        private const string Proj = "TestInputFiles\\SampleProj.txt";
        private const string Main = "TestInputFiles\\SampleSourceCode.txt";
        private const string Directive = "TestInputFiles\\SampleDirective.txt";
        private const string Config = "TestInputFiles\\SampleConfig.txt";
        private const string Config_NA = "TestInputFiles\\SampleConfig_NA.txt";
        private readonly ITestOutputHelper _output;
        private Dictionary<string, string> original;

        public WCFUpdateStepTest(ITestOutputHelper output)
        {
            _output = output;
            original = new Dictionary<string, string>();
            original.Add("directive", File.ReadAllText(Directive));
            original.Add("proj", File.ReadAllText(Proj));
            original.Add("main", File.ReadAllText(Main));
            original.Add("config", File.ReadAllText(Config));
        }

        [Theory]
        [InlineData(Proj, Main, "", Directive, UpgradeStepStatus.Complete)] // can't find path case
        [InlineData(Proj, Main, Config_NA, Directive, UpgradeStepStatus.Complete)] // find path but not applicable case
        [InlineData(Proj, Main, Config, Directive, UpgradeStepStatus.Incomplete)] // success case
        [InlineData(Proj, Main, Config, "", UpgradeStepStatus.Incomplete)] // no directive cs file but still success case.
        public void WCFUpdateTest(string proj, string main, string config, string directive, UpgradeStepStatus expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new TestOutputHelperLoggerProvider(_output));
            var logger = loggerFactory.CreateLogger<WCFUpdateStep>();
            var updater = new WCFUpdateStep(logger, loggerFactory);

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);
            projectFile.Setup(f => f.FilePath).Returns(Path.Combine(Directory.GetCurrentDirectory(), proj));

            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.FileInfo).Returns(new FileInfo("./test"));
            if (config != string.Empty)
            {
                project.Setup(p => p.FindFiles(".config", ProjectItemType.None)).Returns(new List<string> { Path.Combine(Directory.GetCurrentDirectory(), config) });
            }
            else
            {
                project.Setup(p => p.FindFiles(".config", ProjectItemType.None)).Returns(new List<string>());
            }

            if (directive != string.Empty)
            {
                project.Setup(p => p.FindFiles(".cs", ProjectItemType.Compile)).Returns(new List<string>
            { Path.Combine(Directory.GetCurrentDirectory(), main), Path.Combine(Directory.GetCurrentDirectory(), directive) });
            }
            else
            {
                project.Setup(p => p.FindFiles(".cs", ProjectItemType.Compile)).Returns(new List<string> { Path.Combine(Directory.GetCurrentDirectory(), main) });
            }

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(new[] { project.Object });

            // Act
            var result = updater.Initialize(project.Object);

            // Assert
            Assert.Equal(expected, result.Result.Status);
            if (expected == UpgradeStepStatus.Incomplete && directive != string.Empty)
            {
                updater.Apply();
                Assert.Equal(File.ReadAllLines("TestExpectedFiles\\ExpectedConfig.txt"), File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "TestInputFiles\\wcf.config")));
                Assert.Equal(File.ReadAllLines("TestExpectedFiles\\ExpectedOldConfig.txt"), File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), Config)));
                Assert.Equal(File.ReadAllLines("TestExpectedFiles\\ExpectedSourceCode.txt"), File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), Main)));
                Assert.Equal(File.ReadAllLines("TestExpectedFiles\\ExpectedDirective.txt"), File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), Directive)));
                Assert.Equal(File.ReadAllLines("TestExpectedFiles\\ExpectedProj.txt"), File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), Proj)));
                Reset();
            }
        }

        private void Reset()
        {
            File.WriteAllText(Main, original["main"]);
            File.WriteAllText(Proj, original["proj"]);
            File.WriteAllText(Config, original["config"]);
            File.WriteAllText(Directive, original["directive"]);
        }
    }
}
