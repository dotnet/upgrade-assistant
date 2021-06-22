// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Autofac.Extras.Moq;
using Xunit;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMonikerParser;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Tests
{
    [Collection(MSBuildStepTestCollection.Name)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.", Justification = "Need to verify .Count property")]
    public class TargetFrameworkMonikerCollectionTests
    {
        private const string SdkSingleTargetFramework = "TargetFramework";
        private const string SdkMultipleTargetFrameworkName = "TargetFrameworks";
        private const string NonSdkSingleTargetFrameworkName = "TargetFrameworkVersion";

        [Fact]
        public void ThrowsOnNull()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new TargetFrameworkMonikerCollection(null!, null!));
        }

        [InlineData(Net50)]
        [Theory]
        public void SdkStyleSingle(string tfm)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = mock.Mock<IProjectFile>();
            project.Setup(p => p.IsSdk).Returns(true);
            project.Setup(p => p.GetPropertyValue(SdkMultipleTargetFrameworkName)).Returns(string.Empty);
            project.Setup(p => p.GetPropertyValue(SdkSingleTargetFramework)).Returns(tfm);

            mock.Mock<ITargetFrameworkMonikerComparer>().SetupTryParse();

            // Act
            var collection = mock.Create<TargetFrameworkMonikerCollection>();

            // Assert
            Assert.Equal(1, collection.Count);
            Assert.Collection(collection, t => Assert.Equal(tfm, t.Name));
        }

        [InlineData(Net50, new[] { Net50 })]
        [InlineData(Net50 + ";" + NetStandard20, new[] { Net50, NetStandard20 })]
        [Theory]
        public void SdkStyleMultiple(string value, string[] expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = mock.Mock<IProjectFile>();
            project.Setup(p => p.IsSdk).Returns(true);
            project.Setup(p => p.GetPropertyValue(SdkSingleTargetFramework)).Returns(string.Empty);
            project.Setup(p => p.GetPropertyValue(SdkMultipleTargetFrameworkName)).Returns(value);

            mock.Mock<ITargetFrameworkMonikerComparer>().SetupTryParse();

            // Act
            var collection = mock.Create<TargetFrameworkMonikerCollection>();

            // Assert
            Assert.Equal(expected.Length, collection.Count);
            Assert.Equal(expected, collection.Select(t => t.Name).ToArray());
        }

        [InlineData("v4.5", Net45)]
        [Theory]
        public void NonSdkStyle(string version, string expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = mock.Mock<IProjectFile>();
            project.Setup(p => p.IsSdk).Returns(false);
            project.Setup(p => p.GetPropertyValue(NonSdkSingleTargetFrameworkName)).Returns(version);

            var moniker = mock.Mock<ITargetFrameworkMonikerComparer>();
            var parsed = ParseTfm(expected);
            moniker.Setup(s => s.TryParse(expected, out parsed)).Returns(true);

            // Act
            var collection = mock.Create<TargetFrameworkMonikerCollection>();

            // Assert
            Assert.Equal(1, collection.Count);
            Assert.Collection(collection, t => Assert.Equal(expected, t.Name));
        }

        [Fact]
        public void SetNonSdkStyleThrows()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = mock.Mock<IProjectFile>();
            project.Setup(p => p.IsSdk).Returns(false);

            // Act
            var collection = mock.Create<TargetFrameworkMonikerCollection>();

            // Assert
            Assert.Throws<InvalidOperationException>(() => collection.SetTargetFramework(TargetFrameworkMoniker.NetStandard20));
        }

        [Fact]
        public void SetSdkStyleSingleProperty()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = mock.Mock<IProjectFile>();
            project.Setup(p => p.IsSdk).Returns(true);
            project.Setup(p => p.GetPropertyValue(SdkSingleTargetFramework)).Returns(Net45);
            project.Setup(p => p.GetPropertyValue(SdkMultipleTargetFrameworkName)).Returns(string.Empty);

            var collection = mock.Create<TargetFrameworkMonikerCollection>();
            var tfm = TargetFrameworkMoniker.NetStandard20;

            // Act
            collection.SetTargetFramework(tfm);

            // Assert
            project.Verify(p => p.SetPropertyValue(SdkSingleTargetFramework, tfm.ToString()));
        }

        [Fact]
        public void SetSdkStyleMultipleProperty()
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var project = mock.Mock<IProjectFile>();
            project.Setup(p => p.IsSdk).Returns(true);
            project.Setup(p => p.GetPropertyValue(SdkSingleTargetFramework)).Returns(string.Empty);
            project.Setup(p => p.GetPropertyValue(SdkMultipleTargetFrameworkName)).Returns(Net45);

            var collection = mock.Create<TargetFrameworkMonikerCollection>();
            var tfm = TargetFrameworkMoniker.NetStandard20;

            // Act
            collection.SetTargetFramework(tfm);

            // Assert
            project.Verify(p => p.SetPropertyValue(SdkMultipleTargetFrameworkName, tfm.ToString()));
        }
    }
}
