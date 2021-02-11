using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UpgradeAssistant.Fixtures;
using Microsoft.UpgradeAssistant.MSBuild;

namespace Microsoft.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    public class RestoreTestProjectFixture : MSBuildRegistrationFixture
    {
        public RestoreTestProjectFixture()
            : base()
        {
            EnsurePackagesRestored(TestHelper.TestProjectPath);
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
