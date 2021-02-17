using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild.Test
{
    /// <summary>
    /// Class (which is never instantiated) for collecting package step tests that
    /// need test fixtures to register MSBuild and make NuGet packages available.
    /// </summary>
    [CollectionDefinition(Name)]
    public class MSBuildStepTestCollection : ICollectionFixture<MSBuildRegistrationFixture>
    {
        public const string Name = "Package Step Tests";
    }
}
