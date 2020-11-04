using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetMigrator.Engine;
using AspNetMigrator.StartupUpdater;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetMigrator.ConsoleApp
{
    class Program
    {
        static IConfiguration Configuration { get; set; }
        static IServiceProvider Services { get; set; }

        static async Task Main(string[] args)
        {
            Configuration = BuildConfiguration();

            ShowHeader();

            var options = ParseArgs(args);

            if (options is null)
            {
                ShowUsage();
            }
            else
            {
                Services = BuildDIContainer(options);
                await RunMigrationReplAsync();
            }

            Console.WriteLine();
            Console.WriteLine("Done");
            Console.WriteLine();
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

        private static void ShowUsage()
        {
            //                 ---------1---------2---------3---------4---------5---------6---------7---------
            Console.WriteLine("Makes a best-effort attempt to migrate an ASP.NET MVC or Web API project to");
            Console.WriteLine("an ASP.NET Core project.");
            Console.WriteLine();
            Console.WriteLine("No tool can completely automate this process and the project *will*");
            Console.WriteLine("have build errors after the tool runs and will require significant manual");
            Console.WriteLine("changes to complete migration.");
            Console.WriteLine();
            Console.WriteLine("This tool's purpose is to automate some of the 'routine' migration tasks such");
            Console.WriteLine("as changing project file formats and updating APIs with near-equivalents in");
            Console.WriteLine(".NET Core. Analyzers added to the project will highlight the remaining");
            Console.WriteLine("changes needed after the tool runs.");
            Console.WriteLine();
            Console.WriteLine("NOTE: This tool depends on MSBuild, so it should be run from a developer");
            Console.WriteLine("      command prompt.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  AspNetMigrator [options] [Path to project file]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -b, --backup-path <path>       Specifies where the project should be backed");
            Console.WriteLine("                                 up. Defaults to a new directory next to the");
            Console.WriteLine("                                 target project's directory.");
            Console.WriteLine("  -n, --no-backup                Disables backing up the project. This is not");
            Console.WriteLine("                                 recommended unless the project is in source");
            Console.WriteLine("                                 control since this tool will make large");
            Console.WriteLine("                                 changes to both the project and source files");
            Console.WriteLine("  -d, --diag                     Enables verbose diagnostics.");
            Console.WriteLine("  -h, --help, -?                 Displays this help message.");
            Console.WriteLine();
        }

        private static MigrateOptions ParseArgs(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                return null;
            }

            var projectPath = args.Last();
            
            var options = new MigrateOptions
            {
                ProjectPath = projectPath
            };

            for (var i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "-d":
                    case "--diag":
                    case "/d":
                    case "/diag":
                        options.Verbose = true;
                        break;
                    case "-n":
                    case "--no-backup":
                    case "/n":
                    case "/no-backup":
                        options.SkipBackup = true;
                        break;
                    case "-b":
                    case "--backup-path":
                    case "/b":
                    case "/backup-path":
                        if (i >= args.Length - 2)
                        {
                            Console.WriteLine("ERROR: --backup-path must be followed by a path.");
                            return null;
                        }
                        options.BackupPath = args[++i];
                        break;
                    case "-h":
                    case "--help":
                    case "-?":
                    case "/h":
                    case "/help":
                    case "/?":
                        return null;
                    default:
                        Console.WriteLine($"ERROR: Unknown option: {args[i]}");
                        return null;
                }
            }

            return options;
        }

        public static async Task RunMigrationReplAsync()
        {
            var done = false;
            using var scope = Services.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<Migrator>();
            await migrator.InitializeAsync();

            while (!done)
            {
                ShowMigraitonSteps(migrator.Steps);

                var command = GetCommand(migrator.NextStep);

                switch (command)
                {
                    case ReplCommand.ApplyNext:
                        if (!await migrator.ApplyNextStepAsync())
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("No migration step applied");
                            Console.ResetColor();
                        }
                        break;
                    case ReplCommand.ConfigureLogging:
                        Console.WriteLine("Logging configuration not yet implemented.");
                        break;
                    case ReplCommand.SeeStepDetails:
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Current step details");
                        Console.ResetColor();
                        Console.WriteLine(WrapString(migrator.NextStep.Description, Console.WindowWidth));
                        Console.WriteLine();
                        Console.WriteLine(WrapString(migrator.NextStep.StatusDetails, Console.WindowWidth));
                        Console.WriteLine();
                        break;
                    case ReplCommand.Exit:
                        done = true;
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }

        private static ReplCommand GetCommand(MigrationStep step)
        {
            // TODO - Build this menu dynamically based on available commands
            Console.WriteLine("Choose action");
            Console.WriteLine($" 1. Apply next action{ (step is null ? string.Empty : $" ({step.Title})")}");
            Console.WriteLine(" 2. Configure logging");
            Console.WriteLine(" 3. See more step details");
            Console.WriteLine(" 4. Exit");
            Console.Write("> ");

            return Console.ReadLine().Trim(' ', '.', '\t') switch
            {
                "1" => ReplCommand.ApplyNext,
                "2" => ReplCommand.ConfigureLogging,
                "3" => ReplCommand.SeeStepDetails,
                "4" => ReplCommand.Exit,
                _ => ReplCommand.Unknown
            };            
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
                nextStepFound = step.Status != MigrationStepStatus.Complete;

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

        private static string WrapString(string input, int lineLength = 80)
        {
            var word = new StringBuilder();
            var ret = new StringBuilder();
            var index = 0;
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\n':
                    case '\r':
                        AddWordToRet();
                        ret.Append(c);
                        index = 0;
                        break;
                    case '\t':
                        AddWordToRet();
                        word.Append("    ");
                        AddWordToRet();
                        break;
                    case ' ':
                        word.Append(c);
                        AddWordToRet();
                        break;
                    default:
                        word.Append(c);
                        break;
                }
            }
            AddWordToRet();

            return ret.ToString();

            void AddWordToRet()
            {
                if (index + word.Length >= lineLength)
                {
                    ret.AppendLine();
                    index = 0;
                }
                ret.Append(word);
                index += word.Length;
                word = new StringBuilder();
            }
        }
    }
}
