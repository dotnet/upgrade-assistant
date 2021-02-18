using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Packages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class Program
    {
        private const string LogFilePath = "log.txt";

        public static Task Main(string[] args)
        {
            var root = new RootCommand
            {
                // Get name from process so that it will show correctly if run as a .NET CLI tool
                Name = GetProcessName(),
            };

#if ANALYZE_COMMAND
            var migrateCmd = new Command("migrate");
            ConfigureMigrateCommand(migrateCmd);
            root.AddCommand(migrateCmd);

            var analyzeCmd = new Command("analyze");
            ConfigureAnalyzeCommand(analyzeCmd);
            root.AddCommand(analyzeCmd);
#else
            ConfigureMigrateCommand(root);
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

        public static Task RunMigrationAsync(MigrateOptions options)
            => RunCommandAsync(options, null, AppCommand.Migrate, CancellationToken.None);

        public static Task RunAnalysisAsync(MigrateOptions options)
            => RunCommandAsync(options, null, AppCommand.Analyze, CancellationToken.None);

        public static Task RunCommandAsync(MigrateOptions options, Action<ContainerBuilder>? builderConfiguration, AppCommand appCommand, CancellationToken token)
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
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((context, services) =>
                {
                    if (appCommand == AppCommand.Migrate)
                    {
                        services.AddHostedService<ConsoleMigrate>();
                    }
                    else if (appCommand == AppCommand.Analyze)
                    {
                        services.AddHostedService<ConsoleAnalyze>();
                    }

                    services.AddPortabilityAnalysis()
                        .Bind(context.Configuration.GetSection("Portability"));

                    services.AddSingleton<IMigrationStateManager, FileMigrationStateFactory>();

                    services.AddMsBuild();
                    services.AddReports();

                    services.AddSingleton(options);
                    services.AddSingleton<IPackageLoader, PackageLoader>();

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
                    services.AddStepManagement();
                })
                .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                    .MinimumLevel.ControlledBy(logSettings.LoggingLevelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.Conditional(evt => logSettings.IsConsoleEnabled, sink => sink.Console())
                    .WriteTo.Conditional(evt => logSettings.IsFileEnabled, sink => sink.File(LogFilePath)))
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    builder.RegisterExtensions(context.Configuration, options.Extension);
                    builderConfiguration?.Invoke(builder);
                })
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

                WriteString("Makes a best-effort attempt to migrate an ASP.NET MVC or Web API project to an ASP.NET Core project.");
                WriteString("No tool can completely automate this process and the project *will* have build errors after the tool runs and will require significant manual changes to complete migration.");
                WriteString("This tool's purpose is to automate some of the 'routine' migration tasks such as changing project file formats and updating APIs with near-equivalents in NET Core. Analyzers added to the project will highlight the remaining changes needed after the tool runs.");

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

        private static void ConfigureMigrateCommand(Command command)
        {
            command.Handler = CommandHandler.Create<MigrateOptions>(RunMigrationAsync);

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
            command.Handler = CommandHandler.Create<MigrateOptions>(RunAnalysisAsync);

            command.AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            command.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"));
            command.AddOption(new Option<string[]>(new[] { "--extension", "-e" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));
        }
#endif
    }
}
