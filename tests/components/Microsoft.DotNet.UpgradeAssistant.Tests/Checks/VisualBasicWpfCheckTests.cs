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
                            new object[] { new IProject[] { GetValidProject() }, true },
                            new object[] { new IProject[] { GetValidProject(), GetValidProject() }, true },
                            new object[] { new IProject[] { GetInvalidProject() }, false },
                            new object[] { new IProject[] { GetValidProject(), GetInvalidProject(), GetValidProject() }, false },
                            new object[] { Array.Empty<IProject>(), true },
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task OnlyVisualBasicWpfApplicationsFailCheck(IProject[] projects, bool isReady)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var context = mock.Mock<IUpgradeContext>();
            context.Setup(c => c.Projects).Returns(projects);

            var readyCheck = mock.Create<VisualBasicWpfCheck>();

            // Act
            var result = await readyCheck.IsReadyAsync(context.Object, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(isReady, result);
        }

        private static IProject GetValidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.Language).Returns(Languages.CSharp);
            project.Setup(p => p.Components).Returns(ProjectComponents.WPF);

            return project.Object;
        }

        private static IProject GetInvalidProject()
        {
            var project = new Mock<IProject>();
            project.Setup(p => p.Language).Returns(Languages.VisualBasic);
            project.Setup(p => p.Components).Returns(ProjectComponents.WPF);

            return project.Object;
        }
    }
}
