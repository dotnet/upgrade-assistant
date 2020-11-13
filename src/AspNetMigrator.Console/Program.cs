using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using AspNetMigrator.Engine.GlobalCommands;
using AspNetMigrator.StartupUpdater;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator.ConsoleApp
{
    // TODO : Eventually, this may need localized and pull strings from resources, etc.
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "The prototype is not yet localized")]
    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "No sync context in console apps")]
    public class Program
    {
        private static IConfiguration Configuration { get; set; }

        private static IServiceProvider Services { get; set; }

        // the REPL will loop while !_done
        private static bool _done;

        public static Task Main(string[] args)
        {
            Configuration = BuildConfiguration();

            ShowHeader();

            return new CommandLineBuilder(new RootCommand { Handler = CommandHandler.Create<MigrateOptions>(RunMigrationAsync) })
                .AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly())
                .AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."))
                .AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"))
                .AddOption(new Option<DirectoryInfo>(new[] { "--backup-path", "-b" }, "Specifies where the project should be backed up. Defaults to a new directory next to the project's directory."))
                .UseDefaults()
                .UseHelpBuilder(b => new HelpWithHeader(b.Console))
                .Build()
                .InvokeAsync(args);
        }

        private static async Task RunMigrationAsync(MigrateOptions options)
        {
            Services = BuildDIContainer(options);
            await RunMigrationReplAsync();
        }

        private static IServiceProvider BuildDIContainer(MigrateOptions options)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger>(_ => new ConsoleLogger(options.Verbose));
            services.AddSingleton(options);
            services.AddSingleton(new PackageUpdaterOptions { PackageMapPath = "PackageMap.json" });
            services.AddScoped(sp => new MigrationStep[]
            {
                ActivatorUtilities.CreateInstance<BackupStep>(sp),
                ActivatorUtilities.CreateInstance<TryConvertProjectConverterStep>(sp),
                ActivatorUtilities.CreateInstance<PackageUpdaterStep>(sp),
                ActivatorUtilities.CreateInstance<StartupUpdaterStep>(sp),
                ActivatorUtilities.CreateInstance<SourceUpdaterStep>(sp)
            });
            services.AddScoped<Migrator>();
            return services.BuildServiceProvider();
        }

        private static IConfiguration BuildConfiguration() =>
            new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

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

        public static async Task RunMigrationReplAsync()
        {
            using var scope = Services.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<Migrator>();
            await migrator.InitializeAsync();

            while (!_done)
            {
                ShowMigraitonSteps(migrator.Steps);

                var command = GetCommand(migrator);
                var commandResultHandler = CommandResultHandlerFactory.GetCommandResult(command?.GetType());
                var commandResult = await command.ExecuteAsync();
                commandResultHandler.HandleResult(commandResult);
            }
        }

        private static void SetProgramIsDone()
        {
            _done = true;
        }

        private static MigrationCommand GetCommand(Migrator migrator)
        {
            var listOfCommands = GlobalCommands.GetCommands(migrator, SeeMoreDetailsCommandResultHandler.SendMessageToUserAsync, SetProgramIsDone);
            if (migrator?.NextStep?.Commands != null)
            {
                listOfCommands.InsertRange(0, migrator.NextStep.Commands);
            }

            Console.WriteLine("Choose command");
            for (var i = 0; i < listOfCommands.Count; i++)
            {
                Console.WriteLine($" {i + 1}. {listOfCommands[i].CommandText}");
            }

            Console.Write("> ");

            var selectedCommandText = Console.ReadLine().Trim(' ', '.', '\t');
            if (int.TryParse(selectedCommandText, out int selectedCommandIndex))
            {
                selectedCommandIndex--;
                if (selectedCommandIndex >= 0 && selectedCommandIndex < listOfCommands.Count)
                {
                    return listOfCommands[selectedCommandIndex];
                }
            }

            return new UnknownCommand();
        }

        private static void ShowMigraitonSteps(IEnumerable<MigrationStep> steps, int offset = 0)
        {
            if (steps is null || !steps.Any())
            {
                return;
            }

            Console.ResetColor();
            var nextStepFound = false;
            var count = 1;

            if (offset == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Migration Steps");
            }

            foreach (var step in steps)
            {
                // Write indent (if any) and item number
                Console.Write($"{new string(' ', offset * 2)}{count++}. ");

                // Write the step title and make a note of whether the step is incomplete
                // (since that would mean future steps shouldn't show "[Current step]")
                WriteStepStatus(step, !nextStepFound);
                Console.WriteLine(step.Title);
                nextStepFound = nextStepFound || (step.Status != MigrationStepStatus.Complete);

                ShowMigraitonSteps(step.SubSteps, offset + 1);
            }

            Console.WriteLine();
        }

        private static void WriteStepStatus(MigrationStep step, bool isNextStep)
        {
            switch (step.Status)
            {
                case MigrationStepStatus.Complete:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[Complete] ");
                    break;
                case MigrationStepStatus.Failed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[Failed] ");
                    break;
                case MigrationStepStatus.Skipped:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("[Skipped] ");
                    break;
                case MigrationStepStatus.Incomplete:
                    if (isNextStep)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("[Current step] ");
                    }

                    break;
            }

            Console.ResetColor();
        }
    }
}
