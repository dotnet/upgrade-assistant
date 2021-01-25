using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    internal class MSBuildMigrationContextFactory : IMigrationContextFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MSBuildRegistrationStartup _registrar;
        private readonly ILogger<MSBuildMigrationContextFactory> _logger;

        public MSBuildMigrationContextFactory(
            IServiceProvider serviceProvider,
            MSBuildRegistrationStartup registrar,
            ILogger<MSBuildMigrationContextFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _registrar = registrar;
            _logger = logger;
        }

        public async ValueTask<IMigrationContext> CreateContext(CancellationToken token)
        {
            await _registrar.StartupAsync(token).ConfigureAwait(false);

            _logger.LogDebug("Generating context");
            var context = _serviceProvider.GetRequiredService<MSBuildWorkspaceMigrationContext>();

            _logger.LogDebug("Initializing context");
            await context.InitializeWorkspace(token).ConfigureAwait(false);

            _logger.LogDebug("Done initializing context");

            return context;
        }
    }
}
