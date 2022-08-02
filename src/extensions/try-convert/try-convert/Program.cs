using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

using MSBuild.Abstractions;
using MSBuild.Conversion.Project;
using MSBuild.Conversion.SDK;

namespace MSBuild.Conversion
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                Name = "try-convert",
                Handler = CommandHandler.Create(typeof(Program).GetMethod(nameof(Run))!)
            };

            var parser =
                new CommandLineBuilder(rootCommand)
                .UseParseDirective()
                .UseHelp()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(new Option(new[] { "-p", "--project" }, "The path to a project to convert") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "-w", "--workspace" }, "The solution or project file to operate on. If a project is not specified, the command will search the current directory for one.") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "-m", "--msbuild-path" }, "The path to an MSBuild.exe, if you prefer to use that") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "-tfm", "--target-framework" }, "The name of the framework you would like to upgrade to. If unspecified, the default TFM for apps chosen will be the highest available one found on your machine, and the default TFM for libraries will be .NET Standard 2.0.") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "--force-web-conversion" }, "Attempt to convert MVC and WebAPI projects even though significant manual work is necessary after migrating such projects.") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--preview" }, "Use preview SDKs as part of conversion") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--diff-only" }, "Produces a diff of the project to convert; no conversion is done") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--no-backup" }, "Converts projects, does not create a backup of the originals and removes packages.config file.") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--keep-current-tfms" }, "Converts project files but does not change any TFMs. If unspecified, TFMs may change.") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--maui-conversion" }, "Attempt to convert Xamarin.Forms Projects to .NET MAUI projects. There may be additional manual work necessary after migrating such projects.") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--force-remove-custom-imports" }, "Force remove custom imports from the project file if set to true.") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "-u", "--update" }, "Updates the try-convert tool to the latest available version") { Argument = new Argument<bool>(() => false) })
                .Build();

            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static int Run(string? project, string? workspace, string? msbuildPath, string? tfm, bool forceWebConversion, bool preview, bool diffOnly, bool noBackup, bool keepCurrentTfms, bool update, bool mauiConversion, bool forceRemoveCustomImports)
        {
            if (update)
            {
                UpdateTryConvert.Update();
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(workspace))
            {
                Console.WriteLine("Cannot specify both a project and a workspace.");
                return -1;
            }

            if (!string.IsNullOrWhiteSpace(tfm) && keepCurrentTfms)
            {
                Console.WriteLine($"Both '{nameof(tfm)}' and '{nameof(keepCurrentTfms)}' cannot be specified. Please pick one.");
                return -1;
            }

            try
            {
                msbuildPath = MSBuildHelpers.HookAssemblyResolveForMSBuild(msbuildPath);

                //For Xamarin Projects, set MSBuild path to VSInstallation Dir via Environment Variable
                if (mauiConversion)
                {
                    // For Xamarin support we need to set the MSBuild Extensions path to VS install.
                    var vsinstalldir = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSINSTALLDIR"))
                        ? Environment.GetEnvironmentVariable("VSINSTALLDIR")
                        : new VisualStudioLocator().GetLatestVisualStudioPath();
                    if (!string.IsNullOrEmpty(vsinstalldir))
                    {
                        Environment.SetEnvironmentVariable("MSBuildExtensionsPath", Path.Combine(vsinstalldir, "MSBuild"));
                    }
                    else
                    {
                        Console.WriteLine("Error locating VS Install Directory. Try setting Environment Variable VSINSTALLDIR.");
                        return -1;
                    }
                }

                if (string.IsNullOrWhiteSpace(msbuildPath))
                {
                    Console.WriteLine("Could not find an MSBuild.");
                    return -1;
                }

                if (!string.IsNullOrWhiteSpace(tfm))
                {
                    tfm = tfm.Trim();
                    if (!TargetFrameworkHelper.IsValidTargetFramework(tfm))
                    {
                        Console.WriteLine($"Invalid framework specified for --target-framework: '{tfm}'");
                        return -1;
                    }
                }
                else
                {
                    tfm = TargetFrameworkHelper.FindHighestInstalledTargetFramework(preview, msbuildPath);
                }

                var workspacePath = string.Empty;
                MSBuildConversionWorkspaceType workspaceType;

                if (!string.IsNullOrWhiteSpace(project))
                {
                    workspacePath = Path.GetFullPath(project, Environment.CurrentDirectory);
                    workspaceType = MSBuildConversionWorkspaceType.Project;
                }
                else
                {
                    var (isSolution, workspaceFilePath) = MSBuildConversionWorkspaceFinder.FindWorkspace(Environment.CurrentDirectory, workspace);
                    workspaceType = isSolution ? MSBuildConversionWorkspaceType.Solution : MSBuildConversionWorkspaceType.Project;
                    workspacePath = workspaceFilePath;
                }

                var workspaceLoader = new MSBuildConversionWorkspaceLoader(workspacePath, workspaceType);
                // do not create backup if --diff-only specified
                noBackup = noBackup || diffOnly;
                var msbuildWorkspace = workspaceLoader.LoadWorkspace(workspacePath, noBackup, tfm, keepCurrentTfms, forceWebConversion);

                if (msbuildWorkspace.WorkspaceItems.Length is 0)
                {
                    Console.WriteLine("No projects converted.");
                    return 0;
                }

                foreach (var item in msbuildWorkspace.WorkspaceItems)
                {
                    if (diffOnly)
                    {
                        var differ = new Differ(item.UnconfiguredProject.FirstConfiguredProject, item.SdkBaselineProject.Project.FirstConfiguredProject);
                        var parent = Directory.GetParent(workspacePath);
                        if (parent is null)
                        {
                            differ.GenerateReport(workspacePath);
                        }
                        else
                        {
                            differ.GenerateReport(parent.FullName);
                        }
                    }
                    else
                    {
                        var converter = new Converter(item.UnconfiguredProject, item.SdkBaselineProject, item.ProjectRootElement, noBackup, forceRemoveCustomImports);
                        converter.Convert(item.ProjectRootElement.FullPath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }

            Console.WriteLine("Conversion complete!");
            return 0;
        }
    }
}
