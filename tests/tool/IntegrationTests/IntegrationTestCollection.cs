using Microsoft.DotNet.UpgradeAssistant.Fixtures;
using Xunit;

namespace IntegrationTests
{
    /// <summary>
    /// Class (which is never instantiated) for collecting integration
    /// tests that need integration test fixtures like the TryConvertFixture.
    /// </summary>
    [CollectionDefinition(Name)]
    public class IntegrationTestCollection : ICollectionFixture<TryConvertFixture>
    {
        public const string Name = "Integration Tests";
    }
}
