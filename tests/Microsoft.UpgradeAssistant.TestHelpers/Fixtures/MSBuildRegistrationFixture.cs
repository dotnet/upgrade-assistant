using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UpgradeAssistant.MSBuild;

namespace Microsoft.UpgradeAssistant.Fixtures
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
