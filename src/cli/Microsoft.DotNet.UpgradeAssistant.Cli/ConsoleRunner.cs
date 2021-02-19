// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal class ConsoleRunner : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ConsoleRunner(
            IServiceProvider services,
            IHostApplicationLifetime lifetime,
            ILogger<ConsoleRunner> logger)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken token)
        {
            try
            {
                _logger.LogDebug("Configuration loaded from context base directory: {BaseDirectory}", AppContext.BaseDirectory);

                try
                {
                    await RunStartupAsync(token);
                    await RunCommandAsync(token);
                }
                finally
                {
                    _lifetime.StopApplication();
                }
            }
            catch (MigrationException e)
            {
                _logger.LogError("Unexpected error: {Message}", e.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("A cancellation occurred");
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        private async Task RunStartupAsync(CancellationToken token)
        {
            using var scope = _services.CreateScope();
            var startups = scope.ServiceProvider.GetRequiredService<IEnumerable<IMigrationStartup>>();

            foreach (var startup in startups)
            {
                if (!await startup.StartupAsync(token))
                {
                    throw new MigrationException($"Failure running start up action {startup.GetType().FullName}");
                }
            }
        }

        private async Task RunCommandAsync(CancellationToken token)
        {
            using var scope = _services.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<IAppCommand>();
            await command.RunAsync(token);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
