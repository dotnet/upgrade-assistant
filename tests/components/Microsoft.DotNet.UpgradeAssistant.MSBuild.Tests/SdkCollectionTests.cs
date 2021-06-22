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
        private const string WebSDK = "Microsoft.NET.Sdk.Web";

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

        [InlineData(WebSDK)]
        [Theory]
        public void SdkCollectionNotContainsTest(string sdk)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = DefaultSDK;
            var sdkCollection = new SdkCollection(projectRoot);

            // Assert
            Assert.DoesNotContain(sdk, sdkCollection);
        }

        [InlineData(WebSDK, new string[] { DefaultSDK, WebSDK })]
        [Theory]
        public void SdkCollectionAddTest(string sdk, string[] expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = DefaultSDK;

            var sdkCollection = new SdkCollection(projectRoot);

            // Act
            sdkCollection.Add(sdk);

            // Assert
            Assert.Equal(
                sdkCollection,
                expected);
        }

        [InlineData(WebSDK, new string[] { DefaultSDK })]
        [InlineData("System.NET.Sdk.Test", new string[] { DefaultSDK, WebSDK })]
        [Theory]
        public void SdkCollectionRemoveTest(string sdkToRemove, string[] expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = string.Concat(DefaultSDK, ";", WebSDK);

            var sdkCollection = new SdkCollection(projectRoot);

            // Act
            sdkCollection.Remove(sdkToRemove);

            // Assert
            Assert.Equal(
                sdkCollection,
                expected);
        }
    }
}
