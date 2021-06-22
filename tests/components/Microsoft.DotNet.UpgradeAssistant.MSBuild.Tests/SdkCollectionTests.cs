// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Autofac.Extras.Moq;
using Microsoft.Build.Construction;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Tests
{
    [Collection(MSBuildStepTestCollection.Name)]
    public class SdkCollectionTests
    {
        private const string DefaultSDK = "Microsoft.NET.Sdk";

        [Fact]
        public void SdkCollectionEmptyTest()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            var sdkCollection = new SdkCollection(projectRoot);

            // Assert
            Assert.Empty(sdkCollection);
        }

        [InlineData(DefaultSDK)]
        [Theory]
        public void SdkCollectionContainsTest(string sdk)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = sdk;
            var sdkCollection = new SdkCollection(projectRoot);

            // Assert
            Assert.Contains(sdk, sdkCollection);
        }

        [InlineData("Microsoft.NET.Sdk.Web")]
        [Theory]
        public void SdkCollectionAddTest(string sdk)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = DefaultSDK;

            var sdkCollection = new SdkCollection(projectRoot);

            // Act
            sdkCollection.Add(sdk);

            // Assert
            Assert.Collection(
                sdkCollection,
                s => Assert.Equal(s, DefaultSDK),
                s => Assert.Equal(s, sdk));
        }

        [InlineData("Microsoft.NET.Sdk.Web")]
        [Theory]
        public void SdkCollectionRemoveTest(string sdk)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = string.Concat(DefaultSDK, ";", sdk);

            var sdkCollection = new SdkCollection(projectRoot);

            // Act
            sdkCollection.Remove(sdk);

            // Assert
            Assert.Collection(
                sdkCollection,
                s => Assert.Equal(s, DefaultSDK));
        }
    }
}
