using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.ConfigUpdaters;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CSharp.CodeFixes;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Abstractions.Tests
{
    /// <summary>
    /// Tests that validate methods in the ProjectExtensions.cs class.
    /// </summary>
    public class ProjectExtensionTests
    {
        /// <summary>
        /// Checks to see if AppliesToProjectAsync will show HttpContextCurrentCodeFixer when applicable.
        /// </summary>
        /// <param name="components">A component of the project being tested.</param>
        /// <param name="language">The language of the project being tested.</param>
        /// <param name="expected">The expected test outcome.</param>
        /// <returns>A task.</returns>
        [Theory]
        [InlineData(ProjectComponents.AspNetCore, Language.CSharp, true)]
        [InlineData(ProjectComponents.AspNetCore, Language.VisualBasic, false)]
        [InlineData(ProjectComponents.Wpf, Language.CSharp, false)]
        public async Task DoesHttpContextCurrentCodeFixerShowForAspNetCoreCSharpAsync(ProjectComponents components, Language language, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var httpCommonCodeFixer = typeof(HttpContextCurrentCodeFixer);
            var project = mock.Mock<IProject>();
            project.Setup(p => p.Language).Returns(language);
            project.Setup(p => p.GetComponentsAsync(It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<ProjectComponents>(Task.FromResult(components)));

            // Act
            var actual = await httpCommonCodeFixer.AppliesToProjectAsync(project.Object, default);

            // Assert
            if (expected)
            {
                Assert.True(actual, "HttpContextCurrentCodeFixer should apply to C# ASP.NET Core");
            }
            else
            {
                Assert.False(actual, "HttpContextCurrentCodeFixer only applies to C# ASP.NET Core");
            }
        }

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
            var httpCommonCodeFixer = typeof(WebNamespaceConfigUpdater);
            var project = mock.Mock<IProject>();
            project.Setup(p => p.Language).Returns(language);
            project.Setup(p => p.GetComponentsAsync(It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<ProjectComponents>(Task.FromResult(components)));

            // Act
            var actual = await httpCommonCodeFixer.AppliesToProjectAsync(project.Object, default);

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
