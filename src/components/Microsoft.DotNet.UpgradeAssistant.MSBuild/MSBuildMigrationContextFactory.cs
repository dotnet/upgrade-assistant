using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal class MSBuildMigrationContextFactory : IMigrationContextFactory
    {
        private readonly Func<MSBuildWorkspaceMigrationContext> _factory;
        private readonly MSBuildRegistrationStartup _registrar;
        private readonly ILogger<MSBuildMigrationContextFactory> _logger;

        public MSBuildMigrationContextFactory(
            Func<MSBuildWorkspaceMigrationContext> factory,
            MSBuildRegistrationStartup registrar,
            ILogger<MSBuildMigrationContextFactory> logger)
        {
            _factory = factory;
            _registrar = registrar;
            _logger = logger;
        }

        public async ValueTask<IMigrationContext> CreateContext(CancellationToken token)
        {
            _registrar.RegisterMSBuildInstance();

            _logger.LogDebug("Generating context");
            var context = _factory();

            _logger.LogDebug("Initializing context");
            await context.InitializeWorkspace(token).ConfigureAwait(false);

            _logger.LogDebug("Done initializing context");

            return context;
        }
    }
}
