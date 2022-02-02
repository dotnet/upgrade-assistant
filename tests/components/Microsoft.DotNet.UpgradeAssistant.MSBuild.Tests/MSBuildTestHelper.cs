// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Tests
{
    public static class MSBuildTestHelper
    {
        public static string GetTestFolder(string folderName)
        {
            var testFolderPath = Path.Combine(Path.GetTempPath(), folderName);
            if (!Directory.Exists(testFolderPath))
            {
                Directory.CreateDirectory(testFolderPath);
            }

            return testFolderPath;
        }

        public static string AddFileToTestFolder(string folderName, string fileName, string fileContents)
        {
            var testFolderPath = GetTestFolder(folderName);
            string file = Path.Combine(testFolderPath, fileName);
            using (var sw = new StreamWriter(file))
            {
                sw.WriteLine(fileContents);
            }

            return file;
        }

        public static Microsoft.Build.Evaluation.Project LoadProject(string path)
        {
            ProjectCollection collection = ProjectCollection.GlobalProjectCollection;
            return collection.LoadProject(path);
        }

        public static string CreateProject(string folderName)
        {
            var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework> net472 </TargetFramework>
  </PropertyGroup>

    <ItemGroup>
        <PackageReference Include = ""Newtonsoft.Json"" />
    </ItemGroup>
 </Project> ";
            var csprojFilePath = AddFileToTestFolder(folderName, "TestProject.csproj", csproj);
            var packagesProps = @"<Project>
  <ItemGroup Label=""Test"">
    <PackageReference Update = ""Newtonsoft.Json"" Version = ""9.0.1"" />  
  </ItemGroup>
</Project> ";
            AddFileToTestFolder(folderName, "Packages.props", packagesProps);
            var directoryBuildTargets = @"<Project>
  <Sdk Name=""Microsoft.Build.CentralPackageVersions"" Version=""2.0.1"" />
</Project> ";
            AddFileToTestFolder(folderName, "Directory.Build.targets", directoryBuildTargets);

            return csprojFilePath;
        }

        public static void CleanupProject(string folderName)
        {
            var foldderPath = GetTestFolder(folderName);
            if (Directory.Exists(foldderPath))
            {
                Directory.Delete(foldderPath, true);
            }
        }
    }
}
