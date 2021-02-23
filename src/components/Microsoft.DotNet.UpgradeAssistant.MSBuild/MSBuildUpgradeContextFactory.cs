// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal class MSBuildUpgradeContextFactory : IUpgradeContextFactory
    {
        private readonly Func<MSBuildWorkspaceUpgradeContext> _factory;
        private readonly ILogger<MSBuildUpgradeContextFactory> _logger;

        public MSBuildUpgradeContextFactory(
            Func<MSBuildWorkspaceUpgradeContext> factory,
            ILogger<MSBuildUpgradeContextFactory> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async ValueTask<IUpgradeContext> CreateContext(CancellationToken token)
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
