// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                return Task.FromResult(1);
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

        public static Task RunUpgradeAsync(UpgradeOptions options, Action<HostBuilderContext, IServiceCollection> configure, CancellationToken token)
            => RunCommandAsync(options, (ctx, services) =>
            {
                services.AddScoped<IAppCommand, ConsoleUpgrade>();
                configure(ctx, services);
            }, token);

        public static Task RunAnalysisAsync(UpgradeOptions options)
            => RunCommandAsync(options, (ctx, services) =>
            {
                services.AddScoped<IAppCommand, ConsoleAnalyze>();
                services.AddReports();
                services.AddPortabilityAnalysis()
                    .Bind(ctx.Configuration.GetSection("Portability"));
            }, CancellationToken.None);

        private static Task RunCommandAsync(UpgradeOptions options, Action<HostBuilderContext, IServiceCollection> serviceConfiguration, CancellationToken token)
        {
            ConsoleUtils.Clear();

            ShowHeader();

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var logSettings = new LogSettings(options.Verbose);

            var host = Host.CreateDefaultBuilder()
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
                    services.AddSingleton(logSettings);

                    services.AddSingleton<IProcessRunner, ProcessRunner>();

                    services.AddStepManagement();

                    serviceConfiguration(context, services);
                })
                .UseSerilog((_, __, loggerConfiguration) => loggerConfiguration
                    .Enrich.FromLogContext()
                    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
                    .WriteTo.Console(levelSwitch: logSettings.Console)
                    .WriteTo.File(LogFilePath, levelSwitch: logSettings.File))
                .RunConsoleAsync(options =>
                {
                    options.SuppressStatusMessages = true;
                }, token);

            return host;
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
            command.Handler = CommandHandler.Create<UpgradeOptions, CancellationToken>((options, token) => RunUpgradeAsync(options, (ctx, services) => { }, token));

            command.AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            command.AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."));
            command.AddOption(new Option<string[]>(new[] { "--extension", "-e" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));
            command.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"));
            command.AddOption(new Option<bool>(new[] { "--non-interactive" }, "Automatically select each first option") { IsHidden = true });
            command.AddOption(new Option<int>(new[] { "--non-interactive-wait" }, "Wait the supplied seconds before moving on to the next option in non-interactive mode.") { IsHidden = true });
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
