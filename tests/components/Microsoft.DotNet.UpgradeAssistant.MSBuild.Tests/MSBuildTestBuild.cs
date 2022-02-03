﻿// Licensed to the .NET Foundation under one or more agreements.
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
    public sealed class MSBuildTestBuild : IDisposable
    {
        public string TestFolderPath { get; }

        public MSBuildTestBuild(string path)
        {
            TestFolderPath = path;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public string Add(string fileName, string fileContents)
        {
            string file = Path.Combine(TestFolderPath, fileName);
            using (var sw = new StreamWriter(file))
            {
                sw.WriteLine(fileContents);
            }

            return file;
        }

        public static Microsoft.Build.Evaluation.Project Build(string path)
        {
            using var collection = new ProjectCollection();
            return collection.LoadProject(path);
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
