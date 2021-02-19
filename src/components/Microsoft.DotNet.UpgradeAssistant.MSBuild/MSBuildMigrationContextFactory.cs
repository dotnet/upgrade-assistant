// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal class MSBuildMigrationContextFactory : IMigrationContextFactory
    {
        private readonly Func<MSBuildWorkspaceMigrationContext> _factory;
        private readonly ILogger<MSBuildMigrationContextFactory> _logger;

        public MSBuildMigrationContextFactory(
            Func<MSBuildWorkspaceMigrationContext> factory,
            ILogger<MSBuildMigrationContextFactory> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async ValueTask<IMigrationContext> CreateContext(CancellationToken token)
        {
            _logger.LogDebug("Generating context");
            var context = _factory();

            _logger.LogDebug("Initializing context");
            await context.InitializeWorkspace(token).ConfigureAwait(false);

            _logger.LogDebug("Done initializing context");

            return context;
        }
    }
}
