// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Commands;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class TargetFrameworkSelectorTests
    {
        private const string Current = "net0.0current";
        private const string LaterCurrent = "net0.0latercurrent";
        private const string LTS = "net0.0lts";
        private const string Windows = "-windows";
        private const string WinRT = Windows + "10.0.19041.0";
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
            Assert.True(new TargetFrameworkMoniker(LaterCurrent).IsNetCore);
            Assert.True(new TargetFrameworkMoniker(NetStandard20).IsNetStandard);
        }

        [InlineData(new[] { NetStandard20 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.None, NetStandard20)]
        [InlineData(new[] { NetStandard20 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, NetStandard20)]
        [InlineData(new[] { NetStandard21 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.None, NetStandard21)]
        [InlineData(new[] { NetStandard21 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, NetStandard21)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, NetStandard20)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.WindowsDesktop, LTS + "-windows")]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.WindowsDesktop, Current + Windows)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT, LTS + WinRT)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT, Current + WinRT)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Exe, ProjectComponents.None, LTS)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Exe, ProjectComponents.None, Current)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNet, Current)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.AspNet, LTS)]
        [InlineData(new[] { Net45 }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNetCore, Current)]
        [InlineData(new[] { Net45 }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.AspNetCore, LTS)]
        [InlineData(new[] { LaterCurrent }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNetCore, LaterCurrent)]
        [InlineData(new[] { Net45, LaterCurrent }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNetCore, LaterCurrent)]
        [Theory]
        public void NoDependencies(string[] currentTfms, UpgradeTarget target, ProjectOutputType outputType, ProjectComponents components, string expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = mock.Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(currentTfms.Select(t => new TargetFrameworkMoniker(t)).ToArray());
            project.Setup(p => p.Components).Returns(components);
            project.Setup(p => p.OutputType).Returns(outputType);

            mock.Create<UpgradeOptions>().UpgradeTarget = target;

            mock.Mock<IOptions<TFMSelectorOptions>>().Setup(o => o.Value).Returns(_options);

            mock.Mock<ITargetFrameworkMonikerComparer>()
                .Setup(t => t.Compare(new TargetFrameworkMoniker(LaterCurrent), new TargetFrameworkMoniker(Current))).Returns(1);

            var selector = mock.Create<TargetTFMSelector>();

            // Act
            var tfm = selector.SelectTFM(project.Object);

            // Assert
            Assert.Equal(expected, tfm.Name);
        }

        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { NetStandard20 }, NetStandard20)]
        [InlineData(NetStandard20, new[] { NetStandard21 }, new[] { NetStandard20 }, NetStandard21)]
        [InlineData(NetStandard20, new[] { Current }, new[] { NetStandard20 }, Current)]
        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { Current }, Current)]
        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { NetStandard20, Current }, Current)]
        [InlineData(NetStandard20, new[] { NetStandard20 }, new[] { Current, NetStandard20 }, Current)]
        [Theory]
        public void WithDependencies(string inputTfm, string[] dep1Tfms, string[] dep2tfms, string expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var depProject1 = new Mock<IProject>();
            depProject1.Setup(p => p.TargetFrameworks).Returns(dep1Tfms.Select(t => new TargetFrameworkMoniker(t)).ToArray());

            var depProject2 = new Mock<IProject>();
            depProject2.Setup(p => p.TargetFrameworks).Returns(dep2tfms.Select(t => new TargetFrameworkMoniker(t)).ToArray());

            var project = mock.Mock<IProject>();
            project.Setup(p => p.ProjectReferences).Returns(new[] { depProject1.Object, depProject2.Object });

            mock.Mock<IOptions<TFMSelectorOptions>>().Setup(o => o.Value).Returns(_options);

            var comparer = mock.Mock<ITargetFrameworkMonikerComparer>();
            comparer.Setup(t => t.Compare(new TargetFrameworkMoniker(NetStandard20), new TargetFrameworkMoniker(NetStandard20))).Returns(0);
            comparer.Setup(t => t.Compare(new TargetFrameworkMoniker(NetStandard21), new TargetFrameworkMoniker(NetStandard21))).Returns(0);
            comparer.Setup(t => t.Compare(new TargetFrameworkMoniker(Current), new TargetFrameworkMoniker(Current))).Returns(0);
            comparer.Setup(t => t.Compare(new TargetFrameworkMoniker(NetStandard21), new TargetFrameworkMoniker(NetStandard20))).Returns(1);
            comparer.Setup(t => t.Compare(new TargetFrameworkMoniker(NetStandard20), new TargetFrameworkMoniker(NetStandard21))).Returns(-1);
            comparer.Setup(t => t.Compare(new TargetFrameworkMoniker(Current), new TargetFrameworkMoniker(NetStandard20))).Returns(1);
            comparer.Setup(t => t.Compare(new TargetFrameworkMoniker(NetStandard20), new TargetFrameworkMoniker(Current))).Returns(-1);

            var selector = mock.Create<TargetTFMSelector>();

            // Act
            var tfm = selector.EnsureProjectDependenciesNoDowngrade(inputTfm, project.Object);

            // Assert
            Assert.Equal(expected, tfm.Name);
        }
    }
}
