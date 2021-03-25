// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests.Checks
{
    public class MultiTargetingCheckTests
    {
        public static IEnumerable<object[]> TestData =>
            new List<object[]>
            {
                new object[] { GetValidProject(), true },
                new object[] { GetInvalidProject(), false },
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task OnlyValidTFMsPassCheck(IProject project, bool isReady)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var readyCheck = mock.Create<MultiTargetingCheck>();

            // Act
            var result = await readyCheck.IsReadyAsync(project, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(isReady, result);
        }

        private static IProject GetValidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Returns(new[] { new TargetFrameworkMoniker("net5.0") });

            return project.Object;
        }

        private static IProject GetInvalidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.TargetFrameworks).Throws<UpgradeException>();

            return project.Object;
        }
    }
}
