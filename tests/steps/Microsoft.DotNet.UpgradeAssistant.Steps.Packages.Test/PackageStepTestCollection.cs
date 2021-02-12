using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages.Test
{
    /// <summary>
    /// Class (which is never instantiated) for collecting package step tests that
    /// need test fixtures to register MSBuild and make NuGet packages available.
    /// </summary>
    [CollectionDefinition(Name)]
    public class PackageStepTestCollection : ICollectionFixture<MSBuildRegistrationFixture>
    {
        public const string Name = "Package Step Tests";
    }
}
