using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

using MSBuild.Conversion.Facts;

namespace MSBuild.Abstractions
{
    public class MSBuildConversionWorkspace
    {
        public ImmutableArray<MSBuildConversionWorkspaceItem> WorkspaceItems { get; }

        public MSBuildConversionWorkspace(ImmutableArray<string> paths, bool noBackup, string tfm, bool keepCurrentTFMs, bool forceWeb)
        {
            var items = ImmutableArray.CreateBuilder<MSBuildConversionWorkspaceItem>();

            var globalProperties = ImmutableDictionary<string, string>.Empty;
            using var collection = new ProjectCollection();

            foreach (var path in paths)
            {
                var fileExtension = Path.GetExtension(path);
                if ((StringComparer.OrdinalIgnoreCase.Compare(fileExtension, ".fsproj") != 0) &&
                    (StringComparer.OrdinalIgnoreCase.Compare(fileExtension, ".csproj") != 0) &&
                    (StringComparer.OrdinalIgnoreCase.Compare(fileExtension, ".vbproj") != 0))
                {
                    Console.WriteLine($"'{path}' is not a .NET project, skipping it.");
                    continue;
                }

                // This is a hack, but the only way to handle this is to re-architect try-convert along these lines:
                // 1. Launch a .NET Framework process using the VS-deployed MSBuild to evaluate a project
                // 2. Serialize the evaluation model
                // 3. Use the .NET Core process to load MSBuild, but this time from .NET SDK
                // 4. Deserialize the evaluation model
                // 5. Do conversions
                RemoveTargetsNotLoadableByNETSDKMSBuild(path);

                var root = new MSBuildProjectRootElement(ProjectRootElement.Open(path, collection, preserveFormatting: true));
                if (IsSupportedProjectType(root, forceWeb, keepCurrentTFMs))
                {

                    var configurations = DetermineConfigurations(root);

                    var unconfiguredProject = new UnconfiguredProject(configurations);
                    unconfiguredProject.LoadProjects(collection, globalProperties, path);


                    if (TryCreateSdkBaselineProject(path, unconfiguredProject.FirstConfiguredProject, root, configurations, tfm, keepCurrentTFMs, out var baseline))
                    {
                        if (!noBackup)
                        {
                            // Since git doesn't track the new '.old' addition in your changeset,
                            // failing to overwrite will crash the tool if you have one in your directory.
                            // This can be common if you're using the tool a few times and forget to delete the backup.
                            File.Copy(path, path + ".old", overwrite: true);
                        }

                        root.Reload(throwIfUnsavedChanges: false, preserveFormatting: true);
                        var item = new MSBuildConversionWorkspaceItem(root, unconfiguredProject, baseline.Value);
                        items.Add(item);
                    }
                }
            }

            WorkspaceItems = items.ToImmutable();
        }

        public ImmutableDictionary<string, ImmutableDictionary<string, string>> DetermineConfigurations(IProjectRootElement projectRootElement)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>>();
            foreach (var propertyGroup in projectRootElement.PropertyGroups)
            {
                if (MSBuildHelpers.ConditionToDimensionValues(propertyGroup.Condition, out var dimensionValues))
                {
                    var name = MSBuildHelpers.GetConfigurationName(dimensionValues);
                    if (!builder.ContainsKey(name))
                    {
                        builder.Add(name, dimensionValues.ToImmutableDictionary());
                        foreach (var dimensionValuePair in dimensionValues)
                        {
                            if (!builder.ContainsKey(dimensionValuePair.Value))
                            {
                                var dimensionValueDictionary = new Dictionary<string, string> { { dimensionValuePair.Key, dimensionValuePair.Value } };
                                builder.Add(dimensionValuePair.Value, dimensionValueDictionary.ToImmutableDictionary());
                            }
                        }
                    }
                }
            }
            return builder.ToImmutable();
        }

        private void RemoveTargetsNotLoadableByNETSDKMSBuild(string path)
        {
            var projectFile = File.ReadAllText(path);
            if (projectFile is { Length: > 0 })
            {
                var replacement =
                    projectFile
                        // Legacy web project specify these two targets paths. They aren't loadable by the .NET SDK msbuild process, so we just remove them.
                        // They aren't actually useful as an end result when converting web projects. When people conver to .NET Core they will just use the Web SDK attribute.
                        .Replace("<Import Project=\"$(VSToolsPath)\\WebApplications\\Microsoft.WebApplication.targets\" Condition=\"'$(VSToolsPath)' != ''\" />", "")
                        .Replace("<Import Project=\"$(MSBuildExtensionsPath32)\\Microsoft\\VisualStudio\\v10.0\\WebApplications\\Microsoft.WebApplication.targets\" Condition=\"false\" />", "")

                        // Legacy F# projects specify this import. It's not loadable by the .NET SDK MSBuild, and .NET Core-based F# projects don't use it. So we just remove it.
                        .Replace("<Import Project=\"$(FSharpTargetsPath)\" />", "");

                File.WriteAllText(path, replacement);
            }
        }

        /// <summary>
        /// Clear out the project's construction model and add a simple SDK-based project to get a baseline.
        /// We need to use the same name as the original csproj and same path so that all the default that derive
        /// from name\path get the right values (there are a lot of them).
        /// </summary>
        private bool TryCreateSdkBaselineProject(string projectFilePath, IProject project, IProjectRootElement root, ImmutableDictionary<string, ImmutableDictionary<string, string>> configurations, string tfm, bool keepCurrentTFMs, [NotNullWhen(true)] out BaselineProject? baselineProject)
        {
            var projectStyle = GetProjectStyle(root);
            var outputType = GetProjectOutputType(root, projectStyle);
            var rootElement = ProjectRootElement.Open(projectFilePath);

            rootElement.RemoveAllChildren();
            switch (projectStyle)
            {
                case ProjectStyle.Default:
                case ProjectStyle.DefaultSubset:
                case ProjectStyle.MSTest:
                case ProjectStyle.XamarinDroid:
                case ProjectStyle.XamariniOS:
                    rootElement.Sdk = MSBuildFacts.DefaultSDKAttribute;
                    break;
                case ProjectStyle.WindowsDesktop:
                    rootElement.Sdk =
                        tfm.ContainsIgnoreCase(MSBuildFacts.Net5) || tfm.ContainsIgnoreCase(MSBuildFacts.Net6)
                            ? MSBuildFacts.DefaultSDKAttribute
                            : DesktopFacts.WinSDKAttribute; // pre-.NET 5 apps need a special SDK attribute.
                    break;
                case ProjectStyle.Web:
                    rootElement.Sdk =
                        // Web apps should use the web SDK (there's no good way to build a classic web apps with SDK-style projects)
                        // but libraries with web dependencies should only use the web SDK if the TFM is updating. Otherwise, they
                        // can work as classic ASP.NET libraries.
                        MSBuildHelpers.IsAspNetCore(root, keepCurrentTFMs ? project.GetTargetFramework() : tfm)
                            ? WebFacts.WebSDKAttribute
                            : MSBuildFacts.DefaultSDKAttribute;
                    break;
                default:
                    baselineProject = null;
                    return false;
            }

            var propGroup = rootElement.AddPropertyGroup();
            propGroup.AddProperty(MSBuildFacts.TargetFrameworkNodeName, project.GetTargetFramework());

            var outputTypeValue = outputType switch
            {
                ProjectOutputType.Exe => MSBuildFacts.ExeOutputType,
                ProjectOutputType.Library => MSBuildFacts.LibraryOutputType,
                ProjectOutputType.WinExe => MSBuildFacts.WinExeOutputType,
                ProjectOutputType.AppContainerExe => MSBuildFacts.WinExeOutputType,
                _ => project.GetPropertyValue(MSBuildFacts.OutputTypeNodeName)
            };
            propGroup.AddProperty(MSBuildFacts.OutputTypeNodeName, outputTypeValue ?? throw new InvalidOperationException($"OutputType is not set! '{projectFilePath}'"));

            if (MSBuildHelpers.IsWinForms(root))
            {
                MSBuildHelpers.AddUseWinForms(propGroup);
            }

            if (MSBuildHelpers.IsWPF(root))
            {
                MSBuildHelpers.AddUseWPF(propGroup);
            }

            // User is referencing WindowsBase only
            if (MSBuildHelpers.IsDesktop(root) && !MSBuildHelpers.HasWPFOrWinForms(propGroup))
            {
                MSBuildHelpers.AddUseWinForms(propGroup);
            }

            if (MSBuildHelpers.HasWPFOrWinForms(propGroup) && tfm.ContainsIgnoreCase(MSBuildFacts.Net5) || tfm.ContainsIgnoreCase(MSBuildFacts.Net6))
            {
                MSBuildHelpers.AddImportWindowsDesktopTargets(propGroup);
            }

            // Create a new collection because a project with this name has already been loaded into the global collection.
            using var pc = new ProjectCollection();
            var newProject = new UnconfiguredProject(configurations);
            newProject.LoadProjects(pc, rootElement);

            // If the original project had the TargetFramework property don't touch it during conversion.
            var propertiesInTheBaseline = ImmutableArray.Create(MSBuildFacts.OutputTypeNodeName);

            if (project.GetProperty(MSBuildFacts.TargetFrameworkNodeName) is { })
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(MSBuildFacts.TargetFrameworkNodeName);
            }

            if (project.GetProperty(DesktopFacts.UseWinFormsPropertyName) is { })
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(DesktopFacts.UseWinFormsPropertyName);
            }

            if (project.GetProperty(DesktopFacts.UseWPFPropertyName) is { })
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(DesktopFacts.UseWPFPropertyName);
            }

            tfm = GetTFMString(projectStyle, tfm);

            baselineProject = new BaselineProject(newProject, propertiesInTheBaseline, projectStyle, outputType, tfm, keepCurrentTFMs);
            return true;
        }
        static string GetTFMString(ProjectStyle projectStyle, string tfm) {
            if (projectStyle == ProjectStyle.WindowsDesktop)
            {
                if (tfm.ContainsIgnoreCase(MSBuildFacts.Net5))
                    return MSBuildFacts.Net5Windows;
                if (tfm.ContainsIgnoreCase(MSBuildFacts.Net6))
                    return MSBuildFacts.Net6Windows;
            }
            return tfm;
        }

        private bool IsSupportedOutputType(ProjectOutputType type) =>
            type switch
            {
                ProjectOutputType.Exe => true,
                ProjectOutputType.Library => true,
                ProjectOutputType.WinExe => true,
                ProjectOutputType.AppContainerExe => true,
                _ => false
            };

        private ProjectOutputType GetProjectOutputType(IProjectRootElement root) =>
            GetProjectOutputType(root, GetProjectStyle(root));

        private ProjectOutputType GetProjectOutputType(IProjectRootElement root, ProjectStyle projectStyle)
        {
            if (MSBuildHelpers.IsWebApp(root))
            {
                // ASP.NET Core apps use an EXE output type even though legacy ASP.NET apps use Library
                // Note that this specifically checks the project guid type only (rather than a System.Web reference) since
                // ASP.NET libraries may reference System.Web and should still use a Library output types. Only ASP.NET
                // apps should convert with Exe output type.
                return ProjectOutputType.Exe;
            }

            if (MSBuildHelpers.IsXamarinDroid(root) || MSBuildHelpers.IsXamariniOS(root))
            {
                // Xamarin.iOS and Xamarin.Android projects use Library but migrating to .NET MAUI output changes to Exe
                //so force conversion here to Exe as part of Migration journey
                return ProjectOutputType.Exe;
            }

            var outputTypeNode = root.GetOutputTypeNode();
            if (outputTypeNode is null)
            {
                Console.WriteLine($"No OutputType found in the project file '{root.FullPath}'. Are you sure your project builds today?");
                return ProjectOutputType.None;
            }
            else if (ProjectPropertyHelpers.IsLibraryOutputType(outputTypeNode))
            {
                return ProjectOutputType.Library;
            }
            else if (ProjectPropertyHelpers.IsExeOutputType(outputTypeNode))
            {
                return ProjectOutputType.Exe;
            }
            else if (ProjectPropertyHelpers.IsWinExeOutputType(outputTypeNode))
            {
                return ProjectOutputType.WinExe;
            }
            else if (ProjectPropertyHelpers.IsAppContainerExeOutputType(outputTypeNode))
            {
                return ProjectOutputType.AppContainerExe;
            }
            else
            {
                return ProjectOutputType.Other;
            }
        }

        private ProjectStyle GetProjectStyle(IProjectRootElement projectRootElement)
        {
            if (projectRootElement.ImportGroups.Any())
            {
                return ProjectStyle.Custom;
            }

            // Exclude shared project references since they show up as imports.
            // Also exclude any imports that a Nuget package could have brought alone.
            var imports = projectRootElement.Imports.Where(i => i.Label != MSBuildFacts.SharedProjectsImportLabel && !MSBuildHelpers.IsTargetFromNuGetPackage(i));
            var importsCount = imports.Count();

            if (importsCount <= 0)
            {
                return ProjectStyle.Custom;
            }

            if (MSBuildHelpers.IsNETFrameworkMSTestProject(projectRootElement))
            {
                return ProjectStyle.MSTest;
            }
            else if (MSBuildHelpers.IsWPF(projectRootElement) || MSBuildHelpers.IsWinForms(projectRootElement) || MSBuildHelpers.IsDesktop(projectRootElement) || MSBuildHelpers.IsUwp(projectRootElement))
            {
                return ProjectStyle.WindowsDesktop;
            }
            else if (MSBuildHelpers.IsWeb(projectRootElement))
            {
                return ProjectStyle.Web;
            }
            else if(MSBuildHelpers.IsXamarinDroid(projectRootElement))
            {
                return ProjectStyle.XamarinDroid;
            }
            else if (MSBuildHelpers.IsXamariniOS(projectRootElement))
            {
                return ProjectStyle.XamariniOS;
            }
            else
            {
                return ProjectStyle.Default;
            }
        }

        private bool IsSupportedProjectType(IProjectRootElement root, bool forceWeb, bool keepCurrentTFMs)
        {
            if (root.Sdk.ContainsIgnoreCase(MSBuildFacts.DefaultSDKAttribute))
            {
                Console.WriteLine($"'{root.FullPath}' is already a .NET SDK-style project, so it won't be converted.");
                return false;
            }

            if (!IsSupportedOutputType(GetProjectOutputType(root)))
            {
                Console.WriteLine($"{root.FullPath} does not have a supported OutputType.");
                return false;
            }

            if (root.ItemGroups.Any(ig => ig.Items.Any(ProjectItemHelpers.IsReferencingSystemWeb)))
            {
                Console.WriteLine($"{root.FullPath} contains a reference to System.Web, which is not supported on .NET Core. You may have significant work ahead of you to fully port this project.");
            }

            if (root.ItemGroups.Any(ig => ig.Items.Any(i => string.Equals(i.Include, MSBuildFacts.AppConfig))))
            {
                Console.WriteLine($"{root.FullPath} contains an App.config file. App.config is replaced by appsettings.json in .NET Core. You will need to delete App.config and migrate to appsettings.json if it's applicable to your project.");
            }

            if (!keepCurrentTFMs
                && root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsVisualBasicProject))
                && root.ItemGroups.Any(ig => ig.Items.Any(ProjectItemHelpers.IsReferencingSettingsSingleFileGenerator)))
            {
                Console.WriteLine($"{root.FullPath} uses code generators which will not be handled by try-convert. You can edit your vbproj to add support or remove these dependencies.");
            }

            // Lots of wild old project types have project type guids that the old project system uses to light things up!
            // Also some references that are incompatible.
            var projectType = GetProjectSupportType(root);
            switch (projectType)
            {
                case ProjectSupportType.LegacyWeb:
                    Console.WriteLine($"'{root.FullPath}' is a legacy web project and/or references System.Web. Legacy Web projects and System.Web are unsupported on .NET Core. You will need to rewrite your application or find a way to not depend on System.Web to convert this project.");

                    // Proceed only if migrating web scenarios is explicitly enabled
                    return forceWeb;
                case ProjectSupportType.CodedUITest:
                    Console.WriteLine($"'{root.FullPath}' is a coded UI test. Coded UI tests are deprecated and not convertable to .NET Core.");
                    return false;
                case ProjectSupportType.UnknownTestProject:
                    Console.WriteLine($"'{root.FullPath}' has invalid Project Type Guids for test projects and is not supported.");
                    return false;
                case ProjectSupportType.UnsupportedTestType:
                    Console.WriteLine($"'{root.FullPath}' is an unsupported MSTest test type. Only Unit Tests are supported.");
                    return false;
                case ProjectSupportType.Desktop:
                case ProjectSupportType.MSTest:
                    return true;
                case ProjectSupportType.Unsupported:
                default:
                    if (MSBuildHelpers.HasProjectTypeGuidsNode(root))
                    {
                        var allSupportedProjectTypeGuids = DesktopFacts.KnownSupportedDesktopProjectTypeGuids.Select(ptg => ptg.ToString());
                        var allReadProjectTypeGuids = MSBuildHelpers.GetAllProjectTypeGuids(root);
                        Console.WriteLine($"{root.FullPath} is an unsupported project type. Not all project type guids are supported.");
                        PrintGuidMessage(allSupportedProjectTypeGuids, allReadProjectTypeGuids);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
            }

            static void PrintGuidMessage(IEnumerable<string> allSupportedProjectTypeGuids, IEnumerable<string> allReadProjectTypeGuids)
            {
                Console.WriteLine("All supported project type guids:");
                foreach (var guid in allSupportedProjectTypeGuids)
                {
                    Console.WriteLine($"\t{guid}");
                }

                Console.WriteLine("All given project type guids:");
                foreach (var guid in allReadProjectTypeGuids)
                {
                    Console.WriteLine($"\t{guid}");
                }
            }

            static ProjectSupportType GetProjectSupportType(IProjectRootElement root)
            {
                if (root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsLegacyWebProjectTypeGuidsProperty)))
                {
                    return ProjectSupportType.LegacyWeb;
                }
                else if (MSBuildHelpers.IsNETFrameworkMSTestProject(root))
                {
                    if (!root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.AllProjectTypeGuidsAreLegacyTestProjectTypeGuids)))
                    {
                        return ProjectSupportType.UnknownTestProject;
                    }

                    if (root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsCodedUITest)))
                    {
                        return ProjectSupportType.CodedUITest;
                    }

                    if (!root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsUnitTestType)))
                    {
                        return ProjectSupportType.UnsupportedTestType;
                    }

                    return ProjectSupportType.MSTest;
                }
                if (MSBuildHelpers.IsDesktop(root) &&
                    !root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.AllProjectTypeGuidsAreDesktopProjectTypeGuids)))
                {
                    return ProjectSupportType.Unsupported;
                }

                return ProjectSupportType.Desktop;
            }
        }

        private enum ProjectSupportType
        {
            Desktop,
            LegacyWeb,
            MSTest,
            Unsupported,
            CodedUITest,
            UnsupportedTestType,
            UnknownTestProject,
        }
    }
}
