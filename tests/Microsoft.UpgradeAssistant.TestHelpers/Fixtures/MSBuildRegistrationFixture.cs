using System;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UpgradeAssistant.MSBuild;

namespace Microsoft.UpgradeAssistant.Fixtures
{
    public class MSBuildRegistrationFixture
    {
        private static readonly string[] ProjectsToRestore = new[]
        {
            @"..\..\..\..\..\tests\TestAssets\TestProject\TestProject.csproj"
        };

        public MSBuildRegistrationFixture()
        {
            // Register MSBuild
            var msBuildRegistrar = new MSBuildRegistrationStartup(new NullLogger<MSBuildRegistrationStartup>());
            msBuildRegistrar.RegisterMSBuildInstance();

            foreach (var project in ProjectsToRestore)
            {
                EnsurePackagesRestored(Environment.ExpandEnvironmentVariables(project));
            }
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
