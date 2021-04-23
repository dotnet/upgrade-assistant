﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Autofac;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.TargetFramework;
using Moq;
using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.Tests
{
    public class DependencyMinimumTargetFrameworkSelectorFilterTests
    {
        [InlineData(new string[] { }, new string[] { }, null)]
        [InlineData(new[] { NetStandard20 }, new[] { NetStandard20 }, NetStandard20)]
        [InlineData(new[] { NetStandard21 }, new[] { NetStandard20 }, NetStandard21)]
        [InlineData(new[] { Current }, new[] { NetStandard20 }, Current)]
        [InlineData(new[] { NetStandard20 }, new[] { Current }, Current)]
        [InlineData(new[] { NetStandard20 }, new[] { NetStandard20, Current }, NetStandard20)]
        [InlineData(new[] { NetStandard20 }, new[] { Current, NetStandard20 }, NetStandard20)]
        [InlineData(new[] { Current, LTS }, new[] { Current, NetStandard20 }, LTS)]
        [InlineData(new[] { LTS, Current }, new[] { Current, NetStandard20 }, LTS)]
        [InlineData(new[] { Current, NetStandard20 }, new[] { Current, LTS }, LTS)]
        [InlineData(new[] { Current }, new[] { Current, NetStandard20 }, Current)]
        [InlineData(new[] { Current, NetStandard20 }, new[] { Current }, Current)]
        [InlineData(new[] { Current, NetStandard20 }, new[] { NetStandard20 }, NetStandard20)]
        [Theory]
        public void WithDependencies(string[] dep1Tfms, string[] dep2tfms, string? expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose(b => b.RegisterType<TestTfmComparer>().As<ITargetFrameworkMonikerComparer>());

            var depProject1 = new Mock<IProject>();
            depProject1.Setup(p => p.TargetFrameworks).Returns(dep1Tfms.Select(t => ParseTfm(t)).ToArray());

            var depProject2 = new Mock<IProject>();
            depProject2.Setup(p => p.TargetFrameworks).Returns(dep2tfms.Select(t => ParseTfm(t)).ToArray());

            var project = mock.Mock<IProject>();
            project.Setup(p => p.ProjectReferences).Returns(new[] { depProject1.Object, depProject2.Object });

            var state = new Mock<ITargetFrameworkSelectorFilterState>();
            state.Setup(s => s.Project).Returns(project.Object);

            var selector = mock.Create<DependencyMinimumTargetFrameworkSelectorFilter>();

            // Act
            selector.Process(state.Object);

            // Assert
            var count = expected is null ? Times.Never() : Times.AtLeastOnce();
            var tfm = expected is null ? null : ParseTfm(expected);
            state.Verify(s => s.TryUpdate(tfm!), count);
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

            public bool TryMerge(TargetFrameworkMoniker tfm1, TargetFrameworkMoniker tfm2, out TargetFrameworkMoniker result)
            {
                throw new NotImplementedException();
            }

            public bool TryParse(string input, out TargetFrameworkMoniker tfm)
            {
                throw new NotImplementedException();
            }
        }
    }
}
