// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Checks;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Tests.Checks
{
    public class VisualBasicWpfCheckTests
    {
        public static IEnumerable<object[]> TestData =>
            new List<object[]>
            {
                            new object[] { GetValidProject(), true },
                            new object[] { GetInvalidProject(), false },
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task OnlyVisualBasicWpfApplicationsFailCheck(IProject project, bool isReady)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var readyCheck = mock.Create<VisualBasicWpfCheck>();

            // Act
            var result = await readyCheck.IsReadyAsync(project, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(isReady, result);
        }

        private static IProject GetValidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.Language).Returns(Language.CSharp);
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(ProjectComponents.Wpf));

            return project.Object;
        }

        private static IProject GetInvalidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.Language).Returns(Language.VisualBasic);
            project.Setup(p => p.GetComponentsAsync(default)).Returns(new ValueTask<ProjectComponents>(ProjectComponents.Wpf));

            return project.Object;
        }
    }
}
