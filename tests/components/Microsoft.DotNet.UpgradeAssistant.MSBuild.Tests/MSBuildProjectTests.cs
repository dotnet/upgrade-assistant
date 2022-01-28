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
        [InlineData(@"TestProject\TestProject.csproj")]
        [Theory]
        public void LoadProjectNotNullTest(string path)
        {
            // Act
            var project = LoadProject(path);

            // Assert
            Assert.NotNull(project);
        }

        [InlineData(@"TestProject\TestProject.csproj", "Newtonsoft.Json", "9.0.1")]
        [Theory]
        public void PackageReferenceVersionTest(string path, string packageName, string expectedVersion)
        {
            // Act
            var project = LoadProject(path);
            var packageReference = project.GetItems("PackageReference").FirstOrDefault();

            // Assert
            Assert.Equal(packageName, packageReference?.EvaluatedInclude);
            Assert.Equal(expectedVersion, packageReference?.GetMetadataValue("Version"));
        }

        public static Microsoft.Build.Evaluation.Project LoadProject(string path)
        {
            ProjectCollection collection = ProjectCollection.GlobalProjectCollection;
            return collection.LoadProject(path);
        }
    }
}
