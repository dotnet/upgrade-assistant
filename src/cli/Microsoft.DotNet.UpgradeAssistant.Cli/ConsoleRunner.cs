﻿// Licensed to the .NET Foundation under one or more agreements.
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
    /// <summary>
    /// Hosted service for running the upgrade pipeline.
    /// </summary>
    internal class ConsoleRunner : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ErrorCodeAccessor _errorCode;

        public ConsoleRunner(
            IServiceProvider services,
            IHostApplicationLifetime lifetime,
            ErrorCodeAccessor errorCode,
            ILogger<ConsoleRunner> logger)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _errorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This method should not throw any exceptions.")]
        public async Task StartAsync(CancellationToken token)
        {
            try
            {
                _logger.LogDebug("Configuration loaded from context base directory: {BaseDirectory}", AppContext.BaseDirectory);

                await RunStartupAsync(token);
                await RunCommandAsync(token);

                _errorCode.ErrorCode = ErrorCodes.Success;
            }
            catch (UpgradeException e)
            {
                _logger.LogError("{Message}", e.Message);
                _errorCode.ErrorCode = ErrorCodes.UpgradeError;
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("A cancellation occurred");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error during upgrade.");
                _errorCode.ErrorCode = ErrorCodes.UnexpectedError;
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        private async Task RunStartupAsync(CancellationToken token)
        {
            using var scope = _services.CreateScope();
            var startups = scope.ServiceProvider.GetRequiredService<IEnumerable<IUpgradeStartup>>();

            foreach (var startup in startups)
            {
                if (!await startup.StartupAsync(token))
                {
                    throw new UpgradeException($"Failure running start up action {startup.GetType().FullName}");
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
