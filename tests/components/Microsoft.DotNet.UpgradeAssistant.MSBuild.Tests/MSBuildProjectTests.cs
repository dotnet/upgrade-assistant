// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Tests
{
    [Collection(MSBuildStepTestCollection.Name)]
    public class MSBuildProjectTests
    {
        private readonly string _folderName = "MSBuildTestProject";

        [Fact]
        public void LoadProjectNotNullTest()
        {
            // Act
            var path = MSBuildTestHelper.CreateProject(_folderName);
            var project = MSBuildTestHelper.LoadProject(path);

            // Assert
            Assert.NotNull(project);

            // Cleanup
            MSBuildTestHelper.CleanupProject(_folderName);
        }

        [InlineData("Newtonsoft.Json", "9.0.1")]
        [Theory]
        public void PackageReferenceVersionTest(string packageName, string expectedVersion)
        {
            // Act
            var path = MSBuildTestHelper.CreateProject(_folderName);
            var project = MSBuildTestHelper.LoadProject(path);
            var packageReference = project.GetItems("PackageReference").FirstOrDefault();

            // Assert
            Assert.Equal(packageName, packageReference?.EvaluatedInclude);
            Assert.Equal(expectedVersion, packageReference?.GetMetadataValue("Version"));

            // Cleanup
            MSBuildTestHelper.CleanupProject(_folderName);
        }
    }
}
