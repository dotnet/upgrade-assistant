// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Tests
{
    [Collection(MSBuildStepTestCollection.Name)]
    public class MSBuildProjectTests
    {
        [Fact]
        public void LoadProjectNotNullTest()
        {
            // Arrange
            using var msBuildTestBuild = new MSBuildTestBuilder();
            var csprojPath = msBuildTestBuild.Add("TestProject.csproj", @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework> net472 </TargetFramework>
  </PropertyGroup>

    <ItemGroup>
        <PackageReference Include = ""Newtonsoft.Json"" />
    </ItemGroup>
 </Project> ");

            msBuildTestBuild.Add("Packages.props", @"<Project>
  <ItemGroup Label=""Test"">
    <PackageReference Update = ""Newtonsoft.Json"" Version = ""9.0.1"" />  
  </ItemGroup>
</Project> ");

            msBuildTestBuild.Add("Directory.Build.targets", @"<Project>
  <Sdk Name=""Microsoft.Build.CentralPackageVersions"" Version=""2.0.1"" />
</Project> ");

            // Act
            var project = msBuildTestBuild.Build(csprojPath);

            // Assert
            Assert.NotNull(project);
        }

        [InlineData("Newtonsoft.Json", "9.0.1")]
        [Theory]
        public void PackageReferenceVersionTest(string packageName, string expectedVersion)
        {
            // Arrange
            using var msBuildTestBuild = new MSBuildTestBuilder();
            var csprojPath = msBuildTestBuild.Add("TestProject.csproj", @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework> net472 </TargetFramework>
  </PropertyGroup>

    <ItemGroup>
        <PackageReference Include = ""Newtonsoft.Json"" />
    </ItemGroup>
 </Project> ");

            msBuildTestBuild.Add("Packages.props", @"<Project>
  <ItemGroup Label=""Test"">
    <PackageReference Update = ""Newtonsoft.Json"" Version = ""9.0.1"" />  
  </ItemGroup>
</Project> ");

            msBuildTestBuild.Add("Directory.Build.targets", @"<Project>
  <Sdk Name=""Microsoft.Build.CentralPackageVersions"" Version=""2.0.1"" />
</Project> ");

            // Act
            var msbuildEvaluatedProject = msBuildTestBuild.Build(csprojPath);
            var packageReferences = msbuildEvaluatedProject.GetItems("PackageReference").Select(i => new NuGetReference(i.EvaluatedInclude, i.GetMetadataValue("Version")));

            var projectFile = new Mock<IProjectFile>();
            projectFile.Setup(f => f.FilePath).Returns(csprojPath);

            var project = new Mock<IProject>();
            project.Setup(p => p.GetFile()).Returns(projectFile.Object);
            project.Setup(p => p.FileInfo).Returns(new FileInfo(csprojPath));
            project.Setup(p => p.PackageReferences).Returns(packageReferences);

            var packageReference = project.Object.PackageReferences.FirstOrDefault();

            // Assert
            Assert.Equal(packageName, packageReference?.Name);
            Assert.Equal(expectedVersion, packageReference?.Version);
        }
    }
}
