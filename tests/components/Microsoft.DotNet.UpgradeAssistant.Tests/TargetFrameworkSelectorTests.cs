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
        private const string LTS = "net0.0lts";
        private const string Windows = "-windows";
        private const string WinRT = Windows + "10.0.19041.0";

        private readonly TFMSelectorOptions _options = new TFMSelectorOptions
        {
            CurrentTFMBase = Current,
            LTSTFMBase = LTS,
        };

        [InlineData(new[] { "netstandard2.0" }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.None, "netstandard2.0")]
        [InlineData(new[] { "netstandard2.0" }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, "netstandard2.0")]
        [InlineData(new[] { "netstandard2.1" }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.None, "netstandard2.1")]
        [InlineData(new[] { "netstandard2.1" }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, "netstandard2.1")]
        [InlineData(new[] { "net45" }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.None, "netstandard2.0")]
        [InlineData(new[] { "net45" }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.WindowsDesktop, LTS + "-windows")]
        [InlineData(new[] { "net45" }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.WindowsDesktop, Current + Windows)]
        [InlineData(new[] { "net45" }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT, LTS + WinRT)]
        [InlineData(new[] { "net45" }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT, Current + WinRT)]
        [InlineData(new[] { "net45" }, UpgradeTarget.LTS, ProjectOutputType.Exe, ProjectComponents.None, LTS)]
        [InlineData(new[] { "net45" }, UpgradeTarget.Current, ProjectOutputType.Exe, ProjectComponents.None, Current)]
        [InlineData(new[] { "net45" }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNet, Current)]
        [InlineData(new[] { "net45" }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.AspNet, LTS)]
        [InlineData(new[] { "net45" }, UpgradeTarget.Current, ProjectOutputType.Library, ProjectComponents.AspNetCore, Current)]
        [InlineData(new[] { "net45" }, UpgradeTarget.LTS, ProjectOutputType.Library, ProjectComponents.AspNetCore, LTS)]
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

            var selector = mock.Create<TargetTFMSelector>();

            // Act
            var tfm = selector.SelectTFM(project.Object);

            // Assert
            Assert.Equal(expected, tfm.Name);
        }
    }
}
