// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Web.Tests
{
    public class WebNamespaceConfigUpdaterTests
    {
        /// <summary>
        /// Checks to see if AppliesToProjectAsync will show WebNamespaceConfigUpdater when applicable.
        /// </summary>
        /// <param name="components">A component of the project being tested.</param>
        /// <param name="language">The language of the project being tested.</param>
        /// <param name="expected">The expected test outcome.</param>
        /// <returns>a task.</returns>
        [Theory]
        [InlineData(ProjectComponents.AspNetCore, Language.CSharp, true)]
        [InlineData(ProjectComponents.AspNetCore, Language.VisualBasic, false)]
        [InlineData(ProjectComponents.Wpf, Language.CSharp, false)]
        public async Task DoesWebNamespaceConfigUpdaterShowForCSharpAspNetCoreAsync(ProjectComponents components, Language language, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var webNamespaceConfigUpdater = mock.Create<WebNamespaceConfigUpdater>();
            var project = new Mock<IProject>();
            project.Setup(p => p.Language).Returns(language);
            project.Setup(p => p.GetComponentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(components);

            // Act
            var actual = await project.Object.IsApplicableAsync(webNamespaceConfigUpdater, default).ConfigureAwait(false);

            // Assert
            if (expected)
            {
                Assert.True(actual, "WebNamespaceConfigUpdater should apply to C# ASP.NET Core");
            }
            else
            {
                Assert.False(actual, "WebNamespaceConfigUpdater only applies to C# ASP.NET Core");
            }
        }
    }
}
