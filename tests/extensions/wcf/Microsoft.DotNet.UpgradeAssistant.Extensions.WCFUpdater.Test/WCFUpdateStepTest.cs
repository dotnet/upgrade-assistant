// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
            original.Add("directive", File.ReadAllText(Directive.Replace('\\', Path.DirectorySeparatorChar)));
            original.Add("proj", File.ReadAllText(Proj.Replace('\\', Path.DirectorySeparatorChar)));
            original.Add("main", File.ReadAllText(Main.Replace('\\', Path.DirectorySeparatorChar)));
            original.Add("config", File.ReadAllText(Config.Replace('\\', Path.DirectorySeparatorChar)));
        }

        [Theory]
        [InlineData(Proj, Main, "", Directive, UpgradeStepStatus.Complete)] // can't find path case
        [InlineData(Proj, Main, Config_NA, Directive, UpgradeStepStatus.Complete)] // find path but not applicable case
        [InlineData(Proj, Main, Config, Directive, UpgradeStepStatus.Incomplete)] // success case
        [InlineData(Proj, Main, Config, "", UpgradeStepStatus.Incomplete)] // no directive cs file but still success case.
        [InlineData(Proj, "TestInputFiles\\MultiServicesSourceCode.txt", "TestInputFiles\\MultiServicesConfig.txt", "", UpgradeStepStatus.Incomplete)] // Multiple services case
        public void WCFUpdateTest(string proj, string main, string config, string directive, UpgradeStepStatus expected)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            proj = proj.Replace('\\', Path.DirectorySeparatorChar);
            main = main.Replace('\\', Path.DirectorySeparatorChar);
            config = config.Replace('\\', Path.DirectorySeparatorChar);
            directive = directive.Replace('\\', Path.DirectorySeparatorChar);

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
                try
                {
                    updater.Apply();
                    Assert.Equal(File.ReadAllText(Path.Combine("TestExpectedFiles", "ExpectedConfig.txt")), File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "TestInputFiles", "wcf.config")));
                    Assert.Equal(File.ReadAllText(Path.Combine("TestExpectedFiles", "ExpectedOldConfig.txt")), File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), config)));
                    Assert.Equal(File.ReadAllText(Path.Combine("TestExpectedFiles", "ExpectedSourceCode.txt")), File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), main)));
                    Assert.Equal(File.ReadAllText(Path.Combine("TestExpectedFiles", "ExpectedDirective.txt")), File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), directive)));
                    Assert.Equal(File.ReadAllText(Path.Combine("TestExpectedFiles", "ExpectedProj.txt")), File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), proj)));
                    Reset();
                }
                catch
                {
                    Reset();
                    throw;
                }
            }
            else if (main.Equals(Path.Combine("TestInputFiles", "MultiServicesSourceCode.txt"), StringComparison.Ordinal))
            {
                try
                {
                    updater.Apply();
                    Assert.Equal(File.ReadAllText(Path.Combine("TestExpectedFiles", "ExpectedMultiServicesConfig.txt")), File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "TestInputFiles", "wcf.config")));
                    Assert.Equal(File.ReadAllText(Path.Combine("TestExpectedFiles", "ExpectedMultiServicesSourceCode.txt")), File.ReadAllText(main));
                    Reset();
                }
                catch
                {
                    Reset();
                    throw;
                }
            }
        }

        private void Reset()
        {
            File.WriteAllText(Main.Replace('\\', Path.DirectorySeparatorChar), original["main"]);
            File.WriteAllText(Proj.Replace('\\', Path.DirectorySeparatorChar), original["proj"]);
            File.WriteAllText(Config.Replace('\\', Path.DirectorySeparatorChar), original["config"]);
            File.WriteAllText(Directive.Replace('\\', Path.DirectorySeparatorChar), original["directive"]);
        }
    }
}
