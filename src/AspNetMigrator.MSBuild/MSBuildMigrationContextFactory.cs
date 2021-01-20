using System;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetMigrator.MSBuild
{
    internal class MSBuildMigrationContextFactory : IMigrationContextFactory
    {
        private readonly Func<MSBuildWorkspaceMigrationContext> _factory;

        public MSBuildMigrationContextFactory(Func<MSBuildWorkspaceMigrationContext> factory)
        {
            _factory = factory;
        }

        public async ValueTask<IMigrationContext> CreateContext(CancellationToken token)
        {
            var context = _factory();

            await context.InitializeWorkspace(token).ConfigureAwait(false);

            return context;
        }
    }
}
