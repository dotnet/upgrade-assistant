// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;
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
        private readonly ITelemetry _telemetry;

        public ConsoleRunner(
            IServiceProvider services,
            IHostApplicationLifetime lifetime,
            ErrorCodeAccessor errorCode,
            ITelemetry telemetry,
            ILogger<ConsoleRunner> logger)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _errorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken token)
        {
            try
            {
                _logger.LogDebug("Configuration loaded from context base directory: {BaseDirectory}", AppContext.BaseDirectory);

                using var scope = _services.GetAutofacRoot().BeginLifetimeScope(builder =>
                {
                    foreach (var extension in _services.GetRequiredService<IExtensionProvider>().Instances)
                    {
                        var services = new ServiceCollection();
                        extension.AddServices(services);
                        builder.Populate(services);
                    }
                });

                await RunStartupAsync(scope.Resolve<IEnumerable<IUpgradeStartup>>(), token);
                await scope.Resolve<IAppCommand>().RunAsync(token);

                _errorCode.ErrorCode = ErrorCodes.Success;
            }
            catch (UpgradeException e)
            {
                _logger.LogError("{Message}", e.Message);
                _errorCode.ErrorCode = ErrorCodes.UpgradeError;
                _telemetry.TrackException(e);
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("A cancellation occurred");
                _errorCode.ErrorCode = ErrorCodes.Canceled;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error");
                _errorCode.ErrorCode = ErrorCodes.UnexpectedError;
                _telemetry.TrackException(e);
            }
            finally
            {
                _telemetry.TrackEvent("exited", measurements: new Dictionary<string, double> { { "Exit Code", _errorCode.ErrorCode } });

                _lifetime.StopApplication();
            }
        }

        private static async Task RunStartupAsync(IEnumerable<IUpgradeStartup> startups, CancellationToken token)
        {
            foreach (var startup in startups)
            {
                if (!await startup.StartupAsync(token))
                {
                    throw new UpgradeException($"Failure running start up action {startup.GetType().FullName}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
