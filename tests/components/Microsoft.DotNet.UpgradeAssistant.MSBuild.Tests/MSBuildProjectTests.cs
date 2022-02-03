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
        [Fact]
        public void LoadProjectNotNullTest()
        {
            // Arrange
            var msBuildTestBuild = new MSBuildTestBuild("MSBuildTestProject");
            var csprojPath = CreateProjectContent(msBuildTestBuild, "TestProject.csproj", @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework> net472 </TargetFramework>
  </PropertyGroup>

    <ItemGroup>
        <PackageReference Include = ""Newtonsoft.Json"" />
    </ItemGroup>
 </Project> ");

            CreateProjectContent(msBuildTestBuild, "Packages.props", @"<Project>
  <ItemGroup Label=""Test"">
    <PackageReference Update = ""Newtonsoft.Json"" Version = ""9.0.1"" />  
  </ItemGroup>
</Project> ");

            CreateProjectContent(msBuildTestBuild, "Directory.Build.targets", @"<Project>
  <Sdk Name=""Microsoft.Build.CentralPackageVersions"" Version=""2.0.1"" />
</Project> ");

            // Act
            var project = MSBuildTestBuild.Build(csprojPath);

            // Assert
            Assert.NotNull(project);

            // Cleanup
            msBuildTestBuild.Dispose();
        }

        [InlineData("Newtonsoft.Json", "9.0.1")]
        [Theory]
        public void PackageReferenceVersionTest(string packageName, string expectedVersion)
        {
            // Arrange
            var msBuildTestBuild = new MSBuildTestBuild("MSBuildTestProject");
            var csprojPath = CreateProjectContent(msBuildTestBuild, "TestProject.csproj", @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework> net472 </TargetFramework>
  </PropertyGroup>

    <ItemGroup>
        <PackageReference Include = ""Newtonsoft.Json"" />
    </ItemGroup>
 </Project> ");

            CreateProjectContent(msBuildTestBuild, "Packages.props", @"<Project>
  <ItemGroup Label=""Test"">
    <PackageReference Update = ""Newtonsoft.Json"" Version = ""9.0.1"" />  
  </ItemGroup>
</Project> ");

            CreateProjectContent(msBuildTestBuild, "Directory.Build.targets", @"<Project>
  <Sdk Name=""Microsoft.Build.CentralPackageVersions"" Version=""2.0.1"" />
</Project> ");

            // Act
            var project = MSBuildTestBuild.Build(csprojPath);
            var packageReference = project.GetItems("PackageReference").FirstOrDefault();

            // Assert
            Assert.Equal(packageName, packageReference?.EvaluatedInclude);
            Assert.Equal(expectedVersion, packageReference?.GetMetadataValue("Version"));

            // Cleanup
            msBuildTestBuild.Dispose();
        }

        public static string CreateProjectContent(MSBuildTestBuild build, string fileName, string fileContent)
        {
            return build.Add(fileName, fileContent);
        }
    }
}
