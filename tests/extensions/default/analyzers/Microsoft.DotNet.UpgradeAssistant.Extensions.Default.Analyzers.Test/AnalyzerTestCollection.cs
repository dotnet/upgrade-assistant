using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test
{
    /// <summary>
    /// Class (which is never instantiated) for collecting analyzer tests that
    /// need test fixtures to register MSBuild and restore test project packages.
    /// </summary>
    [CollectionDefinition(Name)]
    public class AnalyzerTestCollection : ICollectionFixture<RestoreTestProjectFixture>
    {
        public const string Name = "Analyzer Tests";
    }
}
