// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Build.Execution;
using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source.Tests
{
    public class RestoreTestProjectFixture : MSBuildRegistrationFixture
    {
        public RestoreTestProjectFixture()
            : base()
        {
            EnsurePackagesRestored(DiagnosticAnalysisRunnerTests.TestProjectPath);
        }

        private static void EnsurePackagesRestored(string? projectPath)
        {
            if (projectPath is not null)
            {
                var project = new ProjectInstance(projectPath);
                var restorer = new MSBuildPackageRestorer(new NullLogger<MSBuildPackageRestorer>());
                restorer.RestorePackages(project);
            }
        }
    }
}
