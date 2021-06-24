// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Console apps don't have a synchronization context")]

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("This tool is not supported on non-Windows platforms due to dependencies on Visual Studio.");
                return Task.FromResult(ErrorCodes.PlatformNotSupported);
            }

            var root = new RootCommand
            {
                // Get name from process so that it will show correctly if run as a .NET CLI tool
                Name = GetProcessName(),
            };

            root.AddCommand(new ConsoleAnalyzeCommand(CreateConsoleHost));
            root.AddCommand(new ConsoleUpgradeCommand(CreateConsoleHost));

            return new CommandLineBuilder(root)
                .UseDefaults()
                .UseHelpBuilder(b => new HelpWithHeader(b.Console))
                .Build()
                .InvokeAsync(args);

            static string GetProcessName()
            {
                using var current = System.Diagnostics.Process.GetCurrentProcess();
                return current.ProcessName;
            }
        }

        public static IHostBuilder CreateHost(UpgradeOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var hostBuilder = Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
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
                        .Configure(opts =>
                        {
                            opts.RetainedProperties = options.Option.ParseOptions();
                            opts.CurrentVersion = UpgradeVersion.Current.Version;

                            foreach (var path in options.Extension)
                            {
                                opts.ExtensionPaths.Add(path);
                            }
                        });

                    services.AddMsBuild();
                    services.AddSingleton(options);

                    // Add command handlers
                    if (options.NonInteractive)
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

                    services.AddStepManagement(context.Configuration.GetSection("DefaultTargetFrameworks").Bind);
                });

            hostBuilder.UseConsoleLifetime(options =>
            {
                options.SuppressStatusMessages = true;
            });

            return hostBuilder;
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

        private static IHostBuilder CreateConsoleHost(
            UpgradeOptions options,
            ParseResult parseResult)
        {
            ConsoleUtils.Clear();

            ShowHeader();

            const string LogFilePath = "log.txt";

            var logSettings = new LogSettings(options.Verbose);

            return CreateHost(options)
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

        private static void ShowHeader()
        {
            var title = $"- Microsoft .NET Upgrade Assistant v{UpgradeVersion.Current.FullVersion} -";
            Console.WriteLine(new string('-', title.Length));
            Console.WriteLine(title);
            Console.WriteLine(new string('-', title.Length));
            Console.WriteLine();
        }

        private class HelpWithHeader : HelpBuilder
        {
            public HelpWithHeader(IConsole console)
                : base(console, maxWidth: ConsoleUtils.Width)
            {
            }

            protected override void AddSynopsis(ICommand command)
            {
                ShowHeader();

                const string Title = "Makes a best-effort attempt to upgrade .NET Framework projects to current, preview or LTS versions of .NET.\n\n" +
                                   "This tool does not completely automate the upgrade process and it is expected that projects will have build errors after the tool runs. Manual changes will be required to complete the upgrade to .NET 5.\n\n" +
                                   "This tool's purpose is to automate some of the 'routine' upgrade tasks such as changing project file formats and updating APIs with near-equivalents in the selected target framework. Analyzers added to the project will highlight the remaining changes needed after the tool runs.\n";
                WriteHeading(Title, null);
            }
        }
    }
}
