// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Console apps don't have a synchronization context")]

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class Program
    {
        private const string LogFilePath = "log.txt";

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

#if ANALYZE_COMMAND
            var upgradeCmd = new Command("upgrade");
            ConfigureUpgradeCommand(upgradeCmd);
            root.AddCommand(upgradeCmd);

            var analyzeCmd = new Command("analyze");
            ConfigureAnalyzeCommand(analyzeCmd);
            root.AddCommand(analyzeCmd);
#else
            ConfigureUpgradeCommand(root);
#endif

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

        public static Task<int> RunUpgradeAsync(UpgradeOptions options, Func<IHostBuilder, IHostBuilder> configure, CancellationToken token)
            => RunCommandAsync(options, host => configure(host).ConfigureServices(services =>
            {
                services.AddScoped<IAppCommand, ConsoleUpgrade>();
            }), token);

        private static IHostBuilder EnableLogging(UpgradeOptions options, IHostBuilder host)
        {
            var logSettings = new LogSettings(options.Verbose);

            return host
                .ConfigureServices(services =>
                {
                    services.AddSingleton(logSettings);
                })
                .UseSerilog((_, __, loggerConfiguration) => loggerConfiguration
                    .Enrich.FromLogContext()
                    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                    .WriteTo.Console(levelSwitch: logSettings.Console)
                    .WriteTo.File(LogFilePath, levelSwitch: logSettings.File));
        }

        private static Task<int> RunCommandAsync(
            UpgradeOptions options,
            Func<IHostBuilder, IHostBuilder> configure,
            CancellationToken token)
        {
            ConsoleUtils.Clear();

            ShowHeader();

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var hostBuilder = Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<ConsoleRunner>();

                    services.AddSingleton<IUpgradeStateManager, FileUpgradeStateFactory>();

                    services.AddMsBuild();
                    services.AddSingleton(options);
                    services.AddExtensions(context.Configuration, options.Extension);

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

                    services.AddStepManagement();
                });

            var host = configure(hostBuilder).UseConsoleLifetime(options =>
            {
                options.SuppressStatusMessages = true;
            }).Build();

            return RunAsync(host, token);
        }

        private static async Task<int> RunAsync(this IHost host, CancellationToken token)
        {
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

        private static void ShowHeader()
        {
            var title = $"- Microsoft .NET Upgrade Assistant v{Constants.Version} -";
            Console.WriteLine(new string('-', title.Length));
            Console.WriteLine(title);
            Console.WriteLine(new string('-', title.Length));
            Console.WriteLine();
        }

        private class HelpWithHeader : HelpBuilder
        {
            public HelpWithHeader(IConsole console)
                : base(console, maxWidth: 90)
            {
            }

            public override void Write(ICommand command)
            {
                ShowHeader();

                WriteString("Makes a best-effort attempt to upgrade .NET Framework projects to .NET 5.");
                WriteString("This tool does not completely automate the upgrade process and it is expected that projects will have build errors after the tool runs. Manual changes will be required to complete the upgrade to .NET 5.");
                WriteString("This tool's purpose is to automate some of the 'routine' upgrade tasks such as changing project file formats and updating APIs with near-equivalents in .NET Core. Analyzers added to the project will highlight the remaining changes needed after the tool runs.");

                base.Write(command);
            }

            private void WriteString(string input)
            {
                foreach (var line in SplitText(input, MaxWidth))
                {
                    Console.Out.WriteLine(line);
                }

                Console.Out.WriteLine();
            }
        }

        private static void ConfigureUpgradeCommand(Command command)
        {
            command.Handler = CommandHandler.Create<UpgradeOptions, CancellationToken>((options, token) => RunUpgradeAsync(options, host => EnableLogging(options, host), token));

            command.AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            command.AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."));
            command.AddOption(new Option<string[]>(new[] { "--extension" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));
            command.AddOption(new Option<IReadOnlyCollection<string>>(new[] { "--entry-point", "-e" }, "Provides the entry-point project to start the upgrade process. This may include globbing patterns such as '*' for match."));
            command.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"));
            command.AddOption(new Option<bool>(new[] { "--non-interactive" }, "Automatically select each first option in non-interactive mode."));
            command.AddOption(new Option<int>(new[] { "--non-interactive-wait" }, "Wait the supplied seconds before moving on to the next option in non-interactive mode."));
        }

#if ANALYZE_COMMAND
        private static void ConfigureAnalyzeCommand(Command command)
        {
            command.Handler = CommandHandler.Create<UpgradeOptions>(RunAnalysisAsync);

            command.AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            command.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"));
            command.AddOption(new Option<string[]>(new[] { "--extension", "-e" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));
        }
#endif
    }
}
