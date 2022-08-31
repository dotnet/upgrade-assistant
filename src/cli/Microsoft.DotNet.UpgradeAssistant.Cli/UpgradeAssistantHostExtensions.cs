// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Parsing;

using Autofac.Extensions.DependencyInjection;

using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class UpgradeAssistantHostExtensions
    {
        public static void AddNonInteractive(this IServiceCollection services, Action<NonInteractiveOptions> configure, bool isNonInteractive)
        {
            if (isNonInteractive)
            {
                services.AddTransient<IUserInput, NonInteractiveUserInput>();
                services
                    .AddOptions<NonInteractiveOptions>()
                    .Configure(configure);
            }
        }

        public static void AddKnownExtensionOptions(this IServiceCollection services, KnownExtensionOptionsBuilder options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.AddExtensionOption(new
            {
                Backup = new { Skip = options.SkipBackup },
                Solution = new { Entrypoints = options.Entrypoints }
            });
        }

        public static IHostBuilder UseUpgradeAssistant<TApp>(this IHostBuilder host, IUpgradeAssistantOptions upgradeOptions)
            where TApp : class, IAppCommand
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            return host
                .UseContentRoot(AppContext.BaseDirectory)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();

                    // Register this first so the first startup step is to check for telemetry opt-out
                    services.AddTransient<IUpgradeStartup, ConsoleFirstTimeUserNotifier>();
                    services.AddTelemetry(options =>
                    {
                        context.Configuration.GetSection("Telemetry").Bind(options);
                        options.ProductVersion = UpgradeVersion.Current.FullVersion;
                    });

                    services.AddHostedService<ConsoleRunner>();
                    services.AddStepManagement();
                    services.AddExtensions()
                        .AddDefaultExtensions(context.Configuration)
                        .AddFromEnvironmentVariables(context.Configuration);

                    services.TryAddTransient<IUserInput, ConsoleCollectUserInput>();

                    services.ConfigureOutputOptions(options =>
                    {
                        options.Format = upgradeOptions.Format ?? context.Configuration["Output:DefaultFormat"];
                    });
                    services.AddAnalysis();

                    services.AddSingleton(new InputOutputStreams(Console.In, Console.Out));
                    services.AddSingleton<CommandProvider>();
                    services.TryAddSingleton(new LogSettings(true));

                    services.AddSingleton<IProcessRunner, ProcessRunner>();
                    services.AddSingleton<ErrorCodeAccessor>();

                    services.AddTargetFrameworkSelectors(options =>
                    {
                        context.Configuration.GetSection("DefaultTargetFrameworks").Bind(options);
                        options.TargetTfmSupport = upgradeOptions.TargetTfmSupport;
                    });

                    services.AddScoped<IAppCommand, TApp>();
                })
                .UseConsoleLifetime(options =>
                {
                    options.SuppressStatusMessages = true;
                });
        }

        /// <summary>
        /// Configures common services for Upgrade Assistant CLI execution, including an IAppCommand that will run when the host starts.
        /// </summary>
        /// <typeparam name="TApp">The type of command to start running.</typeparam>
        /// <param name="host">The host builder to register services in.</param>
        /// <param name="options">Options provided by the user on the command line.</param>
        /// <param name="parseResult">The result of parsing command line arguments.</param>
        /// <returns>The host builder updated to include services necessary to run Upgrade Assistant as a command line app.</returns>
        public static IHostBuilder UseConsoleUpgradeAssistant<TApp>(
            this IHostBuilder host,
            IUpgradeAssistantOptions options,
            ParseResult parseResult)
            where TApp : class, IAppCommand
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Program.ShowHeader();

            const string LogFilePath = "upgrade-assistant.clef";

            var logSettings = new LogSettings(options.IsVerbose);

            return host
                .UseUpgradeAssistant<TApp>(options)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(logSettings);

                    services.AddSingleton(parseResult);
                    services.AddTransient<IUpgradeStartup, UsedCommandTelemetry>();
                })
                .UseSerilog((context, __, loggerConfiguration) => loggerConfiguration
                    .Enrich.FromLogContext()
                    .MinimumLevelFromConfiguration(context.Configuration.GetSection("Logging:Loglevel"))
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .MinimumLevel.Is(LogEventLevel.Verbose)
                    .WriteTo.Console(levelSwitch: logSettings.Console)
                    .WriteTo.File(new CompactJsonFormatter(), LogFilePath, levelSwitch: logSettings.File));
        }

        private static LoggerConfiguration MinimumLevelFromConfiguration(this LoggerConfiguration builder, IConfiguration config)
        {
            foreach (var (key, value) in config.AsEnumerable())
            {
                var idx = key.LastIndexOf(':');
                var eventName = key[(idx + 1)..];

                if (Enum.TryParse<LogEventLevel>(value, out var level))
                {
                    builder.MinimumLevel.Override(eventName, level);
                }
            }

            return builder;
        }

        public static async Task<int> RunUpgradeAssistantAsync(this IHostBuilder hostBuilder, CancellationToken token)
        {
            if (hostBuilder is null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            var host = hostBuilder.Build();

            var errorCode = host.Services.GetRequiredService<ErrorCodeAccessor>();

            try
            {
                await host.StartAsync(token).ConfigureAwait(false);

                await host.WaitForShutdownAsync(token).ConfigureAwait(false);
            }
            finally
            {
                if (host is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    host.Dispose();
                }
            }

            return errorCode.ErrorCode;
        }
    }
}
