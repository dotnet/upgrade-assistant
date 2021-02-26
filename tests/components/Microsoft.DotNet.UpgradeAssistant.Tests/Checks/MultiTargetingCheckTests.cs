// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests.Checks
{
    public class MultiTargetingCheckTests
    {
        [Theory]
        [MemberData(nameof(TestData))]
        public async Task OnlyValidTFMsPassCheck(IProject[] projects, bool isReady)
        {
            // Arrange
            var context = new Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(projects);

            var readyCheck = new MultiTargetingCheck(NullLogger<MultiTargetingCheck>.Instance);

            // Act
            var result = await readyCheck.IsReadyAsync(context.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(isReady, result);
        }

        public static IEnumerable<object[]> TestData =>
            new List<object[]>
            {
                    new object[] { new IProject[] { GetValidProject() }, true },
                    new object[] { new IProject[] { GetValidProject(), GetValidProject() }, true },
                    new object[] { new IProject[] { GetInvalidProject() }, false },
                    new object[] { new IProject[] { GetValidProject(), GetInvalidProject(), GetValidProject() }, false },
                    new object[] { Array.Empty<IProject>(), true },
            };

        private static IProject GetValidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.TFM).Returns(new TargetFrameworkMoniker("net5.0"));

            return project.Object;
        }

        private static IProject GetInvalidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.TFM).Throws<UpgradeException>();

            return project.Object;
        }
    }
}
