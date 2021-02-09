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
using AspNetMigrator;
using AspNetMigrator.Solution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UpgradeAssistant.Extensions;
using Microsoft.UpgradeAssistant.Migrator;
using Microsoft.UpgradeAssistant.Steps;
using Microsoft.UpgradeAssistant.Steps.Backup;
using Microsoft.UpgradeAssistant.Steps.Packages;
using Microsoft.UpgradeAssistant.Steps.ProjectFormat;
using Serilog;

namespace Microsoft.UpgradeAssistant.Cli
{
    public class Program
    {
        private const string UpgradeAssistantExtensionPathsSettingName = "UpgradeAssistantExtensionPaths";
        private const string PackageUpdaterStepOptionsSection = "PackageUpdater";
        private const string TryConvertProjectConverterStepOptionsSection = "TryConvertProjectConverter";
        private const string LogFilePath = "log.txt";

        public static Task Main(string[] args)
        {
            ShowHeader();

            var migrateCmd = new Command("migrate")
            {
                Handler = CommandHandler.Create<MigrateOptions>(RunMigrationAsync),
            };

            migrateCmd.AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            migrateCmd.AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."));
            migrateCmd.AddOption(new Option<string[]>(new[] { "--extension", "-e" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));
            migrateCmd.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"));

            var analyzeCmd = new Command("analyze")
            {
                Handler = CommandHandler.Create<MigrateOptions>(RunAnalysisAsync),
            };

            analyzeCmd.AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            analyzeCmd.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"));
            analyzeCmd.AddOption(new Option<string[]>(new[] { "--extension", "-e" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));

            var root = new RootCommand
            {
                // Get name from process so that it will show correctly if run as a .NET CLI tool
                Name = GetProcessName(),
            };

            root.AddCommand(analyzeCmd);
            root.AddCommand(migrateCmd);

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

        public static Task RunCommandAsync(MigrateOptions options, Action<HostBuilderContext, IServiceCollection>? serviceConfiguration, AppCommand appCommand, CancellationToken token)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var logSettings = new LogSettings(options.Verbose);

            var host = Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
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

                    services.AddExtensions(context.Configuration[UpgradeAssistantExtensionPathsSettingName], options.Extension);

                    // Add command handlers
                    services.AddTransient<ICollectUserInput, ConsoleCollectUserInput>();
                    services.AddSingleton(new InputOutputStreams(Console.In, Console.Out));
                    services.AddSingleton<CommandProvider>();
                    services.AddSingleton(logSettings);

                    // Add steps
                    services.AddTryConvertProjectConverterStep().Bind(context.Configuration.GetSection(TryConvertProjectConverterStepOptionsSection));
                    services.AddPackageUpdaterStep().Bind(context.Configuration.GetSection(PackageUpdaterStepOptionsSection));
                    services.AddScoped<MigrationStep, SolutionMigrationStep>();
                    services.AddScoped<MigrationStep, BackupStep>();
                    services.AddTemplateInserterStep();
                    services.AddConfigUpdaterStep();
                    services.AddSourceUpdaterStep();

                    services.AddScoped<MigratorManager>();
                    services.AddTransient<IMigrationStepOrderer, MigrationStepOrderer>();

                    serviceConfiguration?.Invoke(context, services);
                })
                .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                    .MinimumLevel.ControlledBy(logSettings.LoggingLevelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.Conditional(evt => logSettings.IsConsoleEnabled, sink => sink.Console())
                    .WriteTo.Conditional(evt => logSettings.IsFileEnabled, sink => sink.File(LogFilePath)))
                .RunConsoleAsync(options =>
                {
                    options.SuppressStatusMessages = true;
                }, token);

            return host;
        }

        private static void ShowHeader()
        {
            var title = $"- ASP.NET Core Migrator, v{Constants.Version} -";
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
    }
}
