// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Abstractions.Tests
{
    /// <summary>
    /// Tests that validate methods in the ProjectExtensions.cs class.
    /// </summary>
    public class ProjectExtensionsTests
    {
        /// <summary>
        /// Checks to see if AppliesToProjectAsync will show a codefixer that appies to multiple languages.
        /// </summary>
        /// <param name="components">A component of the project being tested.</param>
        /// <param name="language">The language of the project being tested.</param>
        /// <param name="expected">The expected test outcome.</param>
        /// <returns>A task.</returns>
        [Theory]
        [InlineData(ProjectComponents.AspNetCore, Language.CSharp, true)]
        [InlineData(ProjectComponents.AspNetCore, Language.VisualBasic, false)]
        [InlineData(ProjectComponents.AspNetCore, Language.FSharp, true)]
        public async Task CanCodeFixersApplyToMultipleLanguagesAsync(ProjectComponents components, Language language, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var testobj = new TestCodeFixer();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.Language).Returns(language);
            project.Setup(p => p.GetComponentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(components);

            // Act
            var actual = await project.Object.IsApplicableAsync(testobj, default).ConfigureAwait(false);

            // Assert
            if (expected)
            {
                Assert.True(actual, "TestCodeFixer should apply to C# and F# for ASP.NET Core");
            }
            else
            {
                Assert.False(actual, "TestCodeFixer only applies to C# and F# for ASP.NET Core");
            }
        }

        [Theory]
        [InlineData(ProjectComponents.AspNetCore, true)]
        [InlineData(ProjectComponents.AspNet, true)]
        [InlineData(ProjectComponents.AspNet | ProjectComponents.AspNetCore, true)]
        [InlineData(ProjectComponents.AspNet | ProjectComponents.WinRT, true)]
        [InlineData(ProjectComponents.None, false)]
        [InlineData(ProjectComponents.WinRT, false)]
        public async Task MultipleComponentTypes(ProjectComponents components, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var testobj = new TestObjectMultipleComponents();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetComponentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(components);

            // Act
            var actual = await project.Object.IsApplicableAsync(testobj, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(ProjectComponents.AspNetCore, true)]
        [InlineData(ProjectComponents.AspNet, true)]
        [InlineData(ProjectComponents.AspNet | ProjectComponents.AspNetCore, true)]
        [InlineData(ProjectComponents.None, true)]
        [InlineData(ProjectComponents.WinRT, true)]
        public async Task NoneMarkedOnItem(ProjectComponents components, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var testobj = new TestObjectNone();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.GetComponentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(components);

            // Act
            var actual = await project.Object.IsApplicableAsync(testobj, default).ConfigureAwait(false);

            // Assert
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Checks to see if AppliesToProjectAsync will show an IConfigUpdater that appies to multiple languages.
        /// </summary>
        /// <param name="components">A component of the project being tested.</param>
        /// <param name="language">The language of the project being tested.</param>
        /// <param name="expected">The expected test outcome.</param>
        /// <returns>A task.</returns>
        [Theory]
        [InlineData(ProjectComponents.AspNetCore, Language.CSharp, true)]
        [InlineData(ProjectComponents.AspNetCore, Language.VisualBasic, false)]
        [InlineData(ProjectComponents.AspNetCore, Language.FSharp, true)]
        public async Task CanConfigUpdaterApplyToMultipleLanguagesAsync(ProjectComponents components, Language language, bool expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();
            var testobj = new TestObjectMultipleLanguage();
            var project = mock.Mock<IProject>();
            project.Setup(p => p.Language).Returns(language);
            project.Setup(p => p.GetComponentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(components);

            // Act
            var actual = await project.Object.IsApplicableAsync(testobj, default).ConfigureAwait(false);

            // Assert
            if (expected)
            {
                Assert.True(actual, "TestConfigUpdater should apply to C# and F# for ASP.NET Core");
            }
            else
            {
                Assert.False(actual, "TestConfigUpdater only applies to C# and F# for ASP.NET Core");
            }
        }

        [ApplicableComponents(ProjectComponents.AspNetCore | ProjectComponents.AspNet)]
        private class TestObjectMultipleComponents
        {
        }

        [ApplicableComponents(ProjectComponents.None)]
        private class TestObjectNone
        {
        }

        [ApplicableComponents(ProjectComponents.AspNetCore)]
        [ApplicableLanguage(Language.CSharp, Language.FSharp)]
        private class TestObjectMultipleLanguage
        {
        }

        [ApplicableComponents(ProjectComponents.AspNetCore)]
        [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.FSharp, Name = "UHOH1 CodeFix Provider")]
        private class TestCodeFixer
        {
        }
    }
}
