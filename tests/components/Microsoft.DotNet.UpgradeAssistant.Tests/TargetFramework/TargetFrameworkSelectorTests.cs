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

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class TargetFrameworkSelectorTests
    {
        private const string Current = "net0.0current";
        private const string Preview = "net0.0laterpreview";
        private const string LTS = "net0.0lts";
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
            project.Setup(p => p.TargetFrameworks).Returns(currentTfms.Select(t => new TargetFrameworkMoniker(t)).ToArray());
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(components));

            mock.Create<UpgradeOptions>().UpgradeTarget = target;
            mock.Mock<IOptions<TFMSelectorOptions>>().Setup(o => o.Value).Returns(_options);
            mock.Mock<ITargetFrameworkMonikerComparer>().Setup(c => c.TryMerge(new TargetFrameworkMoniker(current), tfm, out finalTfm)).Returns(true);

            var appBase = target == UpgradeTarget.Current ? _options.CurrentTFMBase : _options.LTSTFMBase;

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
