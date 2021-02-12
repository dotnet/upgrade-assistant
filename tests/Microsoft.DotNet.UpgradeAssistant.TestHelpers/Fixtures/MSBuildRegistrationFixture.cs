using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Fixtures
{
    public class MSBuildRegistrationFixture
    {
        public MSBuildRegistrationFixture()
        {
            // Register MSBuild
            var msBuildRegistrar = new MSBuildRegistrationStartup(new NullLogger<MSBuildRegistrationStartup>());
            msBuildRegistrar.RegisterMSBuildInstance();
        }
    }
}
