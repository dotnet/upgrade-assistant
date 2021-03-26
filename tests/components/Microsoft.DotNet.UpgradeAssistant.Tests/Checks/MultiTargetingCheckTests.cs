// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using AutoFixture;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests.Checks
{
    public class MultiTargetingCheckTests
    {
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        public async Task IsReadyTest(int tfmCount, bool isValid)
        {
            // Arrange
            var fixture = new Fixture();
            var tfms = fixture.CreateMany<TargetFrameworkMoniker>(tfmCount).ToArray();

            using var mock = AutoMock.GetLoose();
            var readyCheck = mock.Create<TargetFrameworkCheck>();

            var project = new Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(tfms);

            // Act
            var result = await readyCheck.IsReadyAsync(project.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(isValid, result);
        }
    }
}
