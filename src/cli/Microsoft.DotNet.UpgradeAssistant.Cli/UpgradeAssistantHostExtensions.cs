// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class UpgradeAssistantHostExtensions
    {
        public static IHostBuilder UseUpgradeAssistant<TApp>(this IHostBuilder host, UpgradeOptions upgradeOptions)
            where TApp : class, IAppCommand
        {
            if (host is null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (upgradeOptions is null)
            {
                throw new ArgumentNullException(nameof(upgradeOptions));
            }

            return host
                .UseContentRoot(AppContext.BaseDirectory)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((context, services) =>
                {
                    // Register this first so the first startup step is to check for telemetry opt-out
                    services.AddTransient<IUpgradeStartup, ConsoleFirstTimeUserNotifier>();
                    services.AddTelemetry(options =>
                    {
                        context.Configuration.GetSection("Telemetry").Bind(options);
                        options.ProductVersion = UpgradeVersion.Current.FullVersion;
                    });

                    services.AddHostedService<ConsoleRunner>();

                    services.AddSingleton<IUpgradeStateManager, FileUpgradeStateFactory>();

                    services.AddExtensions()
                        .AddDefaultExtensions(context.Configuration)
                        .AddFromEnvironmentVariables(context.Configuration)
                        .Configure(options =>
                        {
                            options.AdditionalOptions = upgradeOptions.Option.ParseOptions();
                            options.CurrentVersion = UpgradeVersion.Current.Version;

                            foreach (var path in upgradeOptions.Extension)
                            {
                                options.ExtensionPaths.Add(path);
                            }
                        })
                        .AddExtensionOption(new
                        {
                            Backup = new { Skip = upgradeOptions.SkipBackup },
                            Solution = new { EntryPoints = upgradeOptions.EntryPoint }
                        });

                    services.AddMsBuild(optionss =>
                    {
                        optionss.InputPath = upgradeOptions.ProjectPath;
                    });

                    services.AddNuGet(optionss =>
                    {
                        optionss.PackageSourcePath = Path.GetDirectoryName(upgradeOptions.ProjectPath);
                    });

                    services.AddSingleton(upgradeOptions);

                    // Add command handlers
                    if (upgradeOptions.NonInteractive)
                    {
                        services.AddTransient<IUserInput, NonInteractiveUserInput>();
                    }
                    else
                    {
                        services.AddTransient<IUserInput, ConsoleCollectUserInput>();
                    }

                    services.AddSingleton(new InputOutputStreams(Console.In, Console.Out));
                    services.AddSingleton<CommandProvider>();
                    services.TryAddSingleton(new LogSettings(true));

                    services.AddSingleton<IProcessRunner, ProcessRunner>();
                    services.AddSingleton<ErrorCodeAccessor>();

                    services.AddStepManagement();
                    services.AddTargetFrameworkSelectors(options =>
                    {
                        context.Configuration.GetSection("DefaultTargetFrameworks").Bind(options);
                        options.TargetTfmSupport = upgradeOptions.TargetTfmSupport;
                    });

                    services.AddReadinessChecks(options =>
                    {
                        options.IgnoreUnsupportedFeatures = upgradeOptions.IgnoreUnsupportedFeatures;
                    });

                    services.AddScoped<IAppCommand, TApp>();
                })
                .UseConsoleLifetime(options =>
                {
                    options.SuppressStatusMessages = true;
                });
        }

        public static IHostBuilder UseConsoleUpgradeAssistant<TApp>(
            this IHostBuilder host,
            UpgradeOptions options,
            ParseResult parseResult)
            where TApp : class, IAppCommand
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ConsoleUtils.Clear();
            Program.ShowHeader();

            const string LogFilePath = "log.txt";

            var logSettings = new LogSettings(options.Verbose);

            return host
                .UseUpgradeAssistant<TApp>(options)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(logSettings);

                    services.AddSingleton(parseResult);
                    services.AddTransient<IUpgradeStartup, UsedCommandTelemetry>();
                })
                .UseSerilog((_, __, loggerConfiguration) => loggerConfiguration
                    .Enrich.FromLogContext()
                    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                    .WriteTo.Console(levelSwitch: logSettings.Console)
                    .WriteTo.File(LogFilePath, levelSwitch: logSettings.File));
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
