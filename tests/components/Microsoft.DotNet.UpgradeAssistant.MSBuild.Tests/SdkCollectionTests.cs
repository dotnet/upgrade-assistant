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

        [InlineData(DefaultSDK)]
        [Theory]
        public void SdkCollectionContainsTest(string sdk)
        {
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = sdk;

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);
            projectFile.Setup(f => f.Sdk).Returns(new SdkCollection(projectRoot));

            Assert.True(projectFile.Object.Sdk.Contains(sdk));
        }

        [InlineData("Microsoft.NET.Sdk.Web")]
        [Theory]
        public void SdkCollectionAddTest(string sdk)
        {
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = DefaultSDK;

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);
            projectFile.Setup(f => f.Sdk).Returns(new SdkCollection(projectRoot));

            projectFile.Object.Sdk.Add(sdk);

            Assert.Equal(2, projectFile.Object.Sdk.Count);
            Assert.Equal(projectFile.Object.Sdk, new string[] { DefaultSDK, sdk });
        }

        [InlineData("Microsoft.NET.Sdk.Web")]
        [Theory]
        public void SdkCollectionRemoveTest(string sdk)
        {
            using var mock = AutoMock.GetLoose();

            var projectRoot = ProjectRootElement.Create();
            projectRoot.Sdk = string.Concat(DefaultSDK, ";", sdk);

            var projectFile = mock.Mock<IProjectFile>();
            projectFile.Setup(f => f.IsSdk).Returns(true);
            projectFile.Setup(f => f.Sdk).Returns(new SdkCollection(projectRoot));

            projectFile.Object.Sdk.Remove(sdk);

            Assert.Equal(1, projectFile.Object.Sdk.Count);
            Assert.Equal(projectFile.Object.Sdk, new string[] { DefaultSDK });
        }
    }
}
