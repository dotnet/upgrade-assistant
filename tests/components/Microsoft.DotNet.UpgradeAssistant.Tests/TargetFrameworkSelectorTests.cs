// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class TargetFrameworkSelectorTests
    {
        private const string Current = "net0.0current";
        private const string Preview = "net0.0laterpreview";
        private const string LTS = "net0.0lts";
        private const string LTSWindows = LTS + "-windows";
        private const string CurrentWindows = Current + "-windows";
        private const string LTSWinRT = LTSWindows + "10.0.19041.0";
        private const string CurrentWinRT = CurrentWindows + "10.0.19041.0";
        private const string NetStandard20 = "netstandard2.0";
        private const string NetStandard21 = "netstandard2.1";
        private const string Net45 = "net45";

        private readonly TFMSelectorOptions _options = new TFMSelectorOptions
        {
            CurrentTFMBase = Current,
            LTSTFMBase = LTS,
        };

        [Fact]
        public void VerifyTestTfms()
        {
            Assert.True(new TargetFrameworkMoniker(Current).IsNetCore);
            Assert.True(new TargetFrameworkMoniker(LTS).IsNetCore);
            Assert.True(new TargetFrameworkMoniker(Preview).IsNetCore);
            Assert.True(new TargetFrameworkMoniker(NetStandard20).IsNetStandard);
        }

        [InlineData(new[] { NetStandard20 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.None, NetStandard20)]
        [InlineData(new[] { NetStandard20 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, NetStandard20)]
        [InlineData(new[] { NetStandard21 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.None, NetStandard21)]
        [InlineData(new[] { NetStandard21 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, NetStandard21)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, NetStandard20)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.WindowsDesktop, LTSWindows)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.WindowsDesktop, CurrentWindows)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT, LTSWinRT)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT, CurrentWinRT)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Exe, ProjectComponents.None, LTS)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Exe, ProjectComponents.None, Current)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNet, Current)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.AspNet, LTS)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNetCore, Current)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.AspNetCore, LTS)]
        [InlineData(new[] { Preview }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNetCore, Preview)]
        [InlineData(new[] { Net45, Preview }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNetCore, Preview)]
        [Theory]
        public async Task NoDependencies(string[] currentTfms, UpgradeTarget target, ProjectOutputType outputType, ProjectComponents components, string expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose(b => b.RegisterType<TestTfmComparer>().As<ITargetFrameworkMonikerComparer>());

            var project = mock.Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(currentTfms.Select(t => new TargetFrameworkMoniker(t)).ToArray());
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(components));
            project.Setup(p => p.OutputType).Returns(outputType);

            mock.Create<UpgradeOptions>().UpgradeTarget = target;

            mock.Mock<IOptions<TFMSelectorOptions>>().Setup(o => o.Value).Returns(_options);

            var selector = mock.Create<TargetTFMSelector>();

            // Act
            var tfm = await selector.SelectTargetFrameworkAsync(project.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, tfm.Name);
        }

        [InlineData(LTS, new string[] { }, new string[] { }, LTS)]
        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { NetStandard20 }, NetStandard20)]
        [InlineData(NetStandard20, new[] { NetStandard21 }, new[] { NetStandard20 }, NetStandard21)]
        [InlineData(NetStandard20, new[] { Current }, new[] { NetStandard20 }, Current)]
        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { Current }, Current)]
        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { NetStandard20, Current }, NetStandard20)]
        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { Current, NetStandard20 }, NetStandard20)]
        [InlineData(LTS, new[] { Current, LTS }, new[] { Current, NetStandard20 }, LTS)]
        [InlineData(LTS, new[] { LTS, Current }, new[] { Current, NetStandard20 }, LTS)]
        [InlineData(LTS, new[] { Current, NetStandard20 }, new[] { Current, LTS }, LTS)]
        [InlineData(LTS, new[] { Current }, new[] { Current, NetStandard20 }, Current)]
        [InlineData(LTS, new[] { Current, NetStandard20 }, new[] { Current }, Current)]
        [InlineData(LTS, new[] { Current, NetStandard20 }, new[] { NetStandard20 }, LTS)]
        [Theory]
        public void WithDependencies(string inputTfm, string[] dep1Tfms, string[] dep2tfms, string expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose(b => b.RegisterType<TestTfmComparer>().As<ITargetFrameworkMonikerComparer>());

            var depProject1 = new Mock<IProject>();
            depProject1.Setup(p => p.TargetFrameworks).Returns(dep1Tfms.Select(t => new TargetFrameworkMoniker(t)).ToArray());

            var depProject2 = new Mock<IProject>();
            depProject2.Setup(p => p.TargetFrameworks).Returns(dep2tfms.Select(t => new TargetFrameworkMoniker(t)).ToArray());

            var project = mock.Mock<IProject>();
            project.Setup(p => p.ProjectReferences).Returns(new[] { depProject1.Object, depProject2.Object });

            mock.Mock<IOptions<TFMSelectorOptions>>().Setup(o => o.Value).Returns(_options);

            var selector = mock.Create<TargetTFMSelector>();

            // Act
            var tfm = selector.EnsureProjectDependenciesNoDowngrade(inputTfm, project.Object);

            // Assert
            Assert.Equal(expected, tfm.Name);
        }

        private class TestTfmComparer : ITargetFrameworkMonikerComparer
        {
            public int Compare(TargetFrameworkMoniker? x, TargetFrameworkMoniker? y)
                => (x?.Name, y?.Name) switch
                {
                    _ when x == y => 0,
                    (Net45, _) => -1,
                    (_, Net45) => 1,
                    (Preview, Current) => 1,
                    (Current, Preview) => -1,
                    (NetStandard20, NetStandard21) => -1,
                    (NetStandard21, NetStandard20) => 1,
                    (NetStandard20, Current) => -1,
                    (Current, NetStandard20) => 1,
                    (NetStandard20, LTS) => -1,
                    (LTS, NetStandard20) => 1,
                    (LTS, Current) => -1,
                    (Current, LTS) => 1,
                    _ => throw new NotImplementedException(),
                };

            public bool IsCompatible(TargetFrameworkMoniker tfm, TargetFrameworkMoniker other)
            {
                throw new NotImplementedException();
            }
        }
    }
}
