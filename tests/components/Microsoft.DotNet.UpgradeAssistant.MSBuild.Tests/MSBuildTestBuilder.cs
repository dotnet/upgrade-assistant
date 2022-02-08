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
    public sealed class MSBuildTestBuilder : IDisposable
    {
        private string TestFolderPath { get; }

        private ProjectCollection TestProjectCollection { get; }

        public MSBuildTestBuilder()
        {
            TestFolderPath = Path.Combine(Path.GetTempPath(), "MSBuildTestProject");
            if (!Directory.Exists(TestFolderPath))
            {
                Directory.CreateDirectory(TestFolderPath);
            }

            TestProjectCollection = new ProjectCollection();
        }

        public string Add(string fileName, string fileContents)
        {
            string file = Path.Combine(TestFolderPath, fileName);
            File.WriteAllText(file, fileContents);
            return file;
        }

        public Microsoft.Build.Evaluation.Project Build(string path)
        {
            return TestProjectCollection.LoadProject(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(TestFolderPath))
            {
                Directory.Delete(TestFolderPath, true);
            }
        }
    }
}