using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using AspNetMigrator.Solution;
using AspNetMigrator.StartupUpdater;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AspNetMigrator.ConsoleApp
{
    public class Program
    {
        private const string TryConvertProjectConverterStepOptionsSection = "TryConvertProjectConverterStepOptions";
        private const string LogFilePath = "log.txt";

        public static Task Main(string[] args)
        {
            ShowHeader();

            var root = new RootCommand
            {
                Handler = CommandHandler.Create<MigrateOptions>(RunMigrationAsync),

                // Get name from process so that it will show correctly if run as a .NET CLI tool
                Name = GetProcessName(),
            };

            return new CommandLineBuilder(root)
                .AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly())
                .AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."))
                .AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"))
                .AddOption(new Option<DirectoryInfo>(new[] { "--backup-path", "-b" }, "Specifies where the project should be backed up. Defaults to a new directory next to the project's directory."))
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

        private static Task RunMigrationAsync(MigrateOptions options)
        {
            var logSettings = new LogSettings(options.Verbose);

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<ConsoleRepl>();

                    services.AddMsBuild();

                    services.AddSingleton(options);
                    services.AddSingleton(new PackageUpdaterOptions(new[] { "PackageMap.json" }));
                    services.AddSingleton<IPackageLoader, PackageLoader>();

                    // Add command handlers
                    services.AddTransient<ICollectUserInput, ConsoleCollectUserInput>();
                    services.AddSingleton<CommandProvider>();
                    services.AddSingleton(logSettings);

                    // Add steps
                    services.AddScoped<MigrationStep, BackupStep>();
                    services.AddScoped<MigrationStep, SolutionMigrationStep>();
                    services.AddTryConvertProjectConverterStep().Bind(context.Configuration.GetSection(TryConvertProjectConverterStepOptionsSection));
                    services.AddScoped<MigrationStep, PackageUpdaterStep>();
                    services.AddScoped<MigrationStep, StartupUpdaterStep>();
                    services.AddScoped<MigrationStep, SourceUpdaterStep>();
                    services.AddScoped<Migrator>();
                })
                .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                    .MinimumLevel.ControlledBy(logSettings.LoggingLevelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.Conditional(evt => logSettings.IsConsoleEnabled, sink => sink.Console())
                    .WriteTo.Conditional(evt => logSettings.IsFileEnabled, sink => sink.File(LogFilePath)))
                .RunConsoleAsync(options =>
                {
                    options.SuppressStatusMessages = true;
                });

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
                WriteString("NOTE: This tool depends on MSBuild, so it should be run from a developer command prompt.");

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
