// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Autofac.Extras.Moq;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution.Tests
{
    public class EntrypointResolverTests
    {
        [InlineData(new string[] { }, new string[] { }, new string[] { })]
        [InlineData(new[] { "file1" }, new[] { "*" }, new[] { "file1" })]
        [InlineData(new[] { "file1" }, new[] { "f" }, new string[] { })]
        [InlineData(new[] { "file1" }, new string[] { }, new string[] { })]
        [InlineData(new[] { "file1" }, new[] { "i*" }, new string[] { })]
        [InlineData(new[] { "file1" }, new[] { "i*", "f*" }, new[] { "file1" })]
        [InlineData(new[] { "file1", "other" }, new[] { "i*", "f*" }, new[] { "file1" })]
        [InlineData(new[] { "file1", "other" }, new[] { "o*", "f*" }, new[] { "file1", "other" })]
        [Theory]
        public void GlobbingTests(string[] fileNames, string[] globs, string[] expected)
        {
            // Arrange
            using var mock = AutoMock.GetLoose();

            var projects = fileNames.Select(f =>
            {
                var project = new Mock<IProject>();

                project.Setup(p => p.FileInfo).Returns(new FileInfo(f));
                return project.Object;
            }).ToList();

            var resolver = mock.Create<EntrypointResolver>();

            var elementInspectors = expected.Select<string, Action<IProject>>(e =>
            {
                return AssertProject;

                void AssertProject(IProject project)
                    => Assert.Equal(project.FileInfo.Name, e);
            }).ToArray();

            // Act
            var result = resolver.GetEntrypoints(projects, globs);

            // Assert
            Assert.Collection(result, elementInspectors);
        }
    }
}
