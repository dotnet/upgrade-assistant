// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;

using Autofac;
using Autofac.Extras.Moq;

using AutoFixture;

using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Microsoft.Extensions.Options;

using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class TargetFrameworkSelectorTests
    {
        private readonly DefaultTfmOptions _options = new DefaultTfmOptions
        {
            Current = Current,
            LTS = LTS,
            Preview = Preview,
        };

        [Fact]
        public void VerifyTestTfms()
        {
            Assert.True(ParseTfm(Current).IsNetCore);
            Assert.True(ParseTfm(LTS).IsNetCore);
            Assert.True(ParseTfm(Preview).IsNetCore);
            Assert.True(ParseTfm(NetStandard20).IsNetStandard);
        }

        [InlineData(new[] { NetStandard20 }, NetStandard20, UpgradeTarget.Current, ProjectComponents.None)]
        [InlineData(new[] { NetStandard20 }, NetStandard20, UpgradeTarget.LTS, ProjectComponents.None)]
        [InlineData(new[] { NetStandard21 }, NetStandard21, UpgradeTarget.Current, ProjectComponents.None)]
        [InlineData(new[] { NetStandard21 }, NetStandard21, UpgradeTarget.LTS, ProjectComponents.None)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.LTS, ProjectComponents.None)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.LTS, ProjectComponents.WindowsDesktop)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.Current, ProjectComponents.WindowsDesktop)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.LTS, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.Current, ProjectComponents.WindowsDesktop | ProjectComponents.WinRT)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.Current, ProjectComponents.None)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.Current, ProjectComponents.AspNet)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.LTS, ProjectComponents.AspNet)]
        [InlineData(new[] { Net45 }, NetStandard20, UpgradeTarget.Current, ProjectComponents.AspNetCore)]
        [InlineData(new[] { Preview }, NetStandard20, UpgradeTarget.Current, ProjectComponents.AspNetCore)]
        [InlineData(new[] { Net45, Preview }, NetStandard20, UpgradeTarget.Current, ProjectComponents.AspNetCore)]
        [Theory]
        public async Task NoDependencies(string[] currentTfms, string current, UpgradeTarget target, ProjectComponents components)
        {
            // Arrange
            var fixture = new Fixture();
            var tfm = fixture.Create<TargetFrameworkMoniker>();
            var finalTfm = fixture.Create<TargetFrameworkMoniker>();
            var filter = new TestFilter(tfm);

            using var mock = AutoMock.GetLoose(b => b.RegisterInstance(filter).As<ITargetFrameworkSelectorFilter>());

            var project = mock.Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(currentTfms.Select(t => ParseTfm(t)).ToArray());
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(components));

            mock.Mock<IOptions<DefaultTfmOptions>>()
                .Setup(o => o.Value)
                .Returns(new DefaultTfmOptions
                {
                    Current = Current,
                    LTS = LTS,
                    Preview = Preview,
                    TargetTfmSupport = target
                });

            var moniker = mock.Mock<ITargetFrameworkMonikerComparer>();
            moniker.Setup(c => c.TryMerge(ParseTfm(current), tfm, out finalTfm)).Returns(true);
            moniker.SetupTryParse();

            var appBase = target == UpgradeTarget.Current ? _options.Current : _options.LTS;

            var selector = mock.Create<TargetFrameworkSelector>();

            // Act
            var result = await selector.SelectTargetFrameworkAsync(project.Object, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(finalTfm, result);
            Assert.Equal(components, filter.State.Components);
            Assert.Equal(appBase, filter.State.AppBase.Name);
            Assert.Equal(project.Object, filter.State.Project);
        }

        private class TestFilter : ITargetFrameworkSelectorFilter
        {
            private readonly TargetFrameworkMoniker _tfm;

            public ITargetFrameworkSelectorFilterState State { get; private set; } = null!;

            public TestFilter(TargetFrameworkMoniker tfm)
            {
                _tfm = tfm;
            }

            public void Process(ITargetFrameworkSelectorFilterState tfm)
            {
                State = tfm;
                tfm.TryUpdate(_tfm);
            }
        }
    }
}
