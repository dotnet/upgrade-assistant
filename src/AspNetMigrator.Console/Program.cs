using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AspNetMigrator.ConsoleApp
{
    class Program
    {
        static IConfiguration Configuration { get; set; }

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
                var logger = new ConsoleLogger(options.Verbose);
                await MigrateAsync(options.ProjectPath, options.BackupPath, logger);
            }

            Console.WriteLine();
            Console.WriteLine("Done");
            Console.WriteLine();
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
            var pathDir = Path.GetDirectoryName(Path.GetFullPath(projectPath));
            
            var options = new MigrateOptions
            {
                ProjectPath = projectPath,
                BackupPath = $"{Path.TrimEndingDirectorySeparator(pathDir)}.backup"
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
                        options.BackupPath = null;
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

        public static async Task MigrateAsync(string path, string backupPath, Engine.ILogger logger)
        {

        }
    }
}
