using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;

using MSBuild.Abstractions;
using MSBuild.Conversion.Facts;
using MSBuild.Conversion.Package;

namespace MSBuild.Conversion.Project
{
    public static class ProjectRootElementExtensionsForConversion
    {
        public static IProjectRootElement ChangeImportsAndAddSdkAttribute(this IProjectRootElement projectRootElement, BaselineProject baselineProject, bool forceRemoveCustomImports)
        {
            foreach (var import in projectRootElement.Imports)
            {
                if (!forceRemoveCustomImports)
                {
                    var fileName = Path.GetFileName(import.Project);                
                    if (MSBuildFacts.PropsToRemove.Contains(fileName, StringComparer.OrdinalIgnoreCase) ||
                        MSBuildFacts.TargetsToRemove.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    {
                        projectRootElement.RemoveChild(import);
                    }
                    else if (!MSBuildFacts.ImportsToKeep.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"This project has an unrecognized custom import which may need reviewed after conversion: {fileName}");
                    }
                }
                else
                {
                    projectRootElement.RemoveChild(import);
                }
            }

            if (baselineProject.ProjectStyle is ProjectStyle.WindowsDesktop && baselineProject.TargetTFM is MSBuildFacts.NetCoreApp31)
            {
                projectRootElement.Sdk = DesktopFacts.WinSDKAttribute;
            }
            else if (MSBuildHelpers.IsAspNetCore(projectRootElement, baselineProject.TargetTFM))
            {
                // Libraries targeting .NET Framework can use the default SDK and still be used by NetFx callers.
                // However, web apps (as opposed to libraries) or libraries that are targeting .NET Core/.NET should use the web SDK.
                projectRootElement.Sdk = WebFacts.WebSDKAttribute;
            }
            else
            {
                projectRootElement.Sdk = MSBuildFacts.DefaultSDKAttribute;
            }

            // Xamarin projects contain the Import line, not needed for .NET MAUI
            if ((baselineProject.ProjectStyle is ProjectStyle.XamarinDroid) || (baselineProject.ProjectStyle is ProjectStyle.XamariniOS))
            {
                foreach (var import in projectRootElement.Imports)
                {
                    if (XamarinFacts.UnnecessaryXamarinImports.Contains(import.Project, StringComparer.OrdinalIgnoreCase))
                    {
                        projectRootElement.RemoveChild(import);
                    }
                }
            }

            return projectRootElement;
        }

        public static IProjectRootElement UpdateOutputTypeProperty(this IProjectRootElement projectRootElement, BaselineProject baselineProject)
        {
            var outputTypeNode = projectRootElement.GetOutputTypeNode();
            if (outputTypeNode != null)
            {
                outputTypeNode.Value = baselineProject.OutputType switch
                {
                    ProjectOutputType.Exe => MSBuildFacts.ExeOutputType,
                    ProjectOutputType.Library => MSBuildFacts.LibraryOutputType,
                    ProjectOutputType.WinExe => MSBuildFacts.WinExeOutputType,
                    ProjectOutputType.AppContainerExe => MSBuildFacts.WinExeOutputType,
                    _ => throw new InvalidOperationException("Unsupported output type: " + baselineProject.OutputType)
                };
            }
            return projectRootElement;
        }

        public static IProjectRootElement RemoveDefaultedProperties(this IProjectRootElement projectRootElement, BaselineProject baselineProject, ImmutableDictionary<string, Differ> differs)
        {
            foreach (var propGroup in projectRootElement.PropertyGroups)
            {
                var configurationName = MSBuildHelpers.GetConfigurationName(propGroup.Condition);
                var propDiff = differs[configurationName].GetPropertiesDiff();

                foreach (var prop in propGroup.Properties)
                {
                    // These properties were added to the baseline - so don't treat them as defaulted properties.
                    if (baselineProject.GlobalProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (propDiff.DefaultedProperties.Select(p => p.Name).Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        propGroup.RemoveChild(prop);
                    }
                }

                if (propGroup.Properties.Count == 0)
                {
                    projectRootElement.RemoveChild(propGroup);
                }
            }

            return projectRootElement;
        }

        public static IProjectRootElement RemoveUnnecessaryPropertiesNotInSDKByDefault(this IProjectRootElement projectRootElement, ProjectStyle projectStyle)
        {
            foreach (var propGroup in projectRootElement.PropertyGroups)
            {
                foreach (var prop in propGroup.Properties)
                {
                    if (MSBuildFacts.UnnecessaryProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsDefineConstantDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsDebugTypeDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsOutputPathDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsPlatformTargetDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsNameDefault(prop, GetProjectName(projectRootElement.FullPath)))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsDocumentationFileDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsUnnecessaryTestProperty(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsEmptyNuGetPackageImportStamp(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (projectStyle == ProjectStyle.MSTest && ProjectPropertyHelpers.IsOutputTypeNode(prop))
                    {
                        // Old MSTest projects specify library, but this is not valid since tests on .NET Core are netcoreapp projects.
                        propGroup.RemoveChild(prop);
                    }
                    else if (projectStyle == ProjectStyle.XamarinDroid && (XamarinFacts.UnnecessaryXamProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (projectStyle == ProjectStyle.XamariniOS && (XamarinFacts.UnnecessaryXamProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase)))
                    {
                        propGroup.RemoveChild(prop);
                    }
                }

                if (propGroup.Properties.Count == 0)
                {
                    projectRootElement.RemoveChild(propGroup);
                }
            }

            return projectRootElement;

            static string GetProjectName(string projectPath)
            {
                var projName = projectPath.Split('\\').Last();
                return projName.Substring(0, projName.LastIndexOf('.'));
            }
        }

        public static IProjectRootElement RemoveOrUpdateItems(this IProjectRootElement projectRootElement, ImmutableDictionary<string, Differ> differs, BaselineProject baselineProject, string tfm)
        {
            foreach (var itemGroup in projectRootElement.ItemGroups)
            {
                var configurationName = MSBuildHelpers.GetConfigurationName(itemGroup.Condition);

                foreach (var item in itemGroup.Items.Where(item => !ProjectItemHelpers.IsPackageReference(item)))
                {
                    if (item.HasMetadata && ProjectItemHelpers.CanItemMetadataBeRemoved(item))
                    {
                        foreach (var metadataElement in item.Metadata.Where(x => x.ElementName != "Aliases"))
                        {
                            item.RemoveChild(metadataElement);
                        }
                    }

                    if (MSBuildFacts.UnnecessaryItemIncludes.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (MSBuildFacts.UnnecessaryWebIncludes.Contains(item.Include, StringComparer.OrdinalIgnoreCase) && MSBuildHelpers.IsAspNetCore(projectRootElement, tfm))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsExplicitValueTupleReferenceThatCanBeRemoved(item, tfm))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsReferenceConvertibleToPackageReference(item))
                    {
                        var packageName = NugetHelpers.FindPackageNameFromReferenceName(item.Include);
                        string? version = null;
                        try
                        {
                            version = NugetHelpers.GetLatestVersionForPackageNameAsync(packageName).GetAwaiter().GetResult();
                        }
                        catch (Exception)
                        {
                            // Network failure of come kind
                        }

                        if (version is null)
                        {
                            // fall back to hard-coded version in the event of a network failure
                            version = MSBuildFacts.DefaultItemsThatHavePackageEquivalents[packageName];
                        }

                        projectRootElement.AddPackage(packageName, version);
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsReferenceComingFromOldNuGet(item))
                    {
                        // We've already converted it to PackageReference.
                        // Yes, this might mean their references are broken, since this might not have been convertable to PackageReference.
                        // That's probably fine!
                        itemGroup.RemoveChild(item);
                    }
                    else if (IsDesktopRemovableItem(baselineProject, itemGroup, item))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsItemWithUnnecessaryMetadata(item))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (baselineProject.ProjectStyle is ProjectStyle.XamarinDroid || baselineProject.ProjectStyle is ProjectStyle.XamariniOS)
                    {
                        if (XamarinFacts.UnnecessaryXamItemIncludes.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                            itemGroup.RemoveChild(item);

                        if (XamarinFacts.UnnecessaryXamItemTypes.Contains(item.ItemType, StringComparer.OrdinalIgnoreCase))
                            itemGroup.RemoveChild(item);
                    }
                    else
                    {
                        var itemsDiff = differs[configurationName].GetItemsDiff();
                        UpdateBasedOnDiff(itemsDiff, itemGroup, item);
                    }
                }

                if (itemGroup.Items.Count == 0)
                {
                    projectRootElement.RemoveChild(itemGroup);
                }
            }

            return projectRootElement;

            static void UpdateBasedOnDiff(ImmutableArray<ItemsDiff> itemsDiff, ProjectItemGroupElement itemGroup, ProjectItemElement item)
            {
                var itemTypeDiff = itemsDiff.FirstOrDefault(id => id.ItemType.Equals(item.ItemType, StringComparison.OrdinalIgnoreCase));
                if (!itemTypeDiff.DefaultedItems.IsDefault)
                {
                    var defaultedItems = itemTypeDiff.DefaultedItems.Select(i => i.EvaluatedInclude);
                    if (defaultedItems.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                    {
                        itemGroup.RemoveChild(item);
                    }
                }

                if (!itemTypeDiff.ChangedItems.IsDefault)
                {
                    var changedItems = itemTypeDiff.ChangedItems.Select(i => i.EvaluatedInclude);
                    if (changedItems.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                    {
                        var path = item.Include;
                        item.Include = null;
                        item.Update = path;
                    }
                }
            }

            static bool IsDesktopRemovableItem(BaselineProject sdkBaselineProject, ProjectItemGroupElement itemGroup, ProjectItemElement item)
            {
                return sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop
                       && (ProjectItemHelpers.IsLegacyXamlDesignerItem(item)
                           || ProjectItemHelpers.IsDependentUponXamlDesignerItem(item)
                           || ProjectItemHelpers.IsDesignerFile(item)
                           || ProjectItemHelpers.IsSettingsFile(item)
                           || ProjectItemHelpers.IsResxFile(item)
                           || ProjectItemHelpers.DesktopReferencesNeedsRemoval(item)
                           || ProjectItemHelpers.IsDesktopRemovableGlobbedItem(sdkBaselineProject.ProjectStyle, item));
            }
        }

        public static IProjectRootElement AddItemRemovesForIntroducedItems(this IProjectRootElement projectRootElement, ImmutableDictionary<string, Differ> differs)
        {
            var introducedItems = differs.Values
                                          .SelectMany(
                                            differ => differ.GetItemsDiff()
                                                            .Where(diff => MSBuildFacts.GlobbedItemTypes.Contains(diff.ItemType, StringComparer.OrdinalIgnoreCase))
                                                            .SelectMany(diff => diff.IntroducedItems))
                                          .Distinct(ProjectItemComparer.IncludeComparer)
                                          .Where(x => StringComparer.OrdinalIgnoreCase.Compare(x.ItemType, "None") != 0);

            if (introducedItems.Any())
            {
                var itemGroup = projectRootElement.AddItemGroup();
                foreach (var introducedItem in introducedItems)
                {
                    var item = itemGroup.AddItem(introducedItem.ItemType, introducedItem.EvaluatedInclude);
                    item.Include = null;
                    item.Remove = introducedItem.EvaluatedInclude;
                }
            }

            return projectRootElement;
        }

        public static IProjectRootElement RemoveUnnecessaryTargetsIfTheyExist(this IProjectRootElement projectRootElement)
        {
            var targets = projectRootElement.Targets;
            if (targets.Count == 0)
            {
                return projectRootElement;
            }

            foreach (var target in targets)
            {
                // Old target dropped into project files that checked if a .dll was in a hint path specified as a reference above.
                // This is from the old days when NuGet and MSBuild pretended the other didn't exist.
                // It's not necessary anymore.
                if (target.Name.Equals(PackageFacts.EnsureNuGetPackageBuildImportsName, StringComparison.OrdinalIgnoreCase))
                {
                    projectRootElement.RemoveChild(target);
                }

                // ASP.NET target for building views which is no longer needed in ASP.NET Core.
                if (target.Name.Equals(WebFacts.MvcBuildViewsName, StringComparison.OrdinalIgnoreCase))
                {
                    projectRootElement.RemoveChild(target);
                }
            }

            return projectRootElement;
        }

        private static IProjectRootElement AddPackage(this IProjectRootElement projectRootElement, string packageName, string packageVersion)
        {
            var packageReferencesItemGroup = MSBuildHelpers.GetOrCreatePackageReferencesItemGroup(projectRootElement);
            AddPackageReferenceElement(packageReferencesItemGroup, packageName, packageVersion);
            return projectRootElement;
        }

        public static IProjectRootElement ConvertAndAddPackages(this IProjectRootElement projectRootElement,
            ProjectStyle projectStyle, string tfm, bool removePackagesConfig)
        {
            var packagesConfigItemGroup = MSBuildHelpers.GetPackagesConfigItemGroup(projectRootElement);
            if (packagesConfigItemGroup is null)
            {
                return projectRootElement;
            }

            var packagesConfigItem = MSBuildHelpers.GetPackagesConfigItem(packagesConfigItemGroup);
            var path = Path.Combine(projectRootElement.DirectoryPath, packagesConfigItem.Include);
            if (!File.Exists(path))
            {
                // packages.config element is listed in the project file
                // but it does not exist on disk
                return projectRootElement;
            }

            var packageReferences = PackagesConfigConverter.Convert(path);
            if (packageReferences is { } && packageReferences.Any())
            {
                var groupForPackageRefs = projectRootElement.AddItemGroup();
                foreach (var pkgref in packageReferences)
                {
                    if (pkgref.ID == null)
                    {
                        continue;
                    }

                    if (pkgref.ID.Equals(MSBuildFacts.SystemValueTupleName, StringComparison.OrdinalIgnoreCase) && MSBuildHelpers.FrameworkHasAValueTuple(tfm))
                    {
                        continue;
                    }

                    if (MSBuildFacts.UnnecessaryItemIncludes.Contains(pkgref.ID, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (MSBuildFacts.UnnecessaryWebIncludes.Contains(pkgref.ID, StringComparer.OrdinalIgnoreCase)
                        && MSBuildHelpers.IsAspNetCore(projectRootElement, tfm))
                    {
                        continue;
                    }

                    AddPackageReferenceElement(groupForPackageRefs, pkgref.ID, pkgref.Version);
                }

                if (projectStyle == ProjectStyle.MSTest
                    && !projectRootElement.ItemGroups.Any(ig => ig.Items.Any(item => ProjectItemHelpers.IsSpecificPacakgeReference(item, MSTestFacts.MSTestSDKPackageName))))
                {
                    AddPackageReferenceElement(groupForPackageRefs, MSTestFacts.MSTestSDKPackageName, MSTestFacts.MSTestSDKDev16FloatingVersion);
                }

                // If the only references we had are already in the SDK, we're done.
                if (!groupForPackageRefs.Items.Any())
                {
                    projectRootElement.RemoveChild(groupForPackageRefs);
                }
            }

            packagesConfigItemGroup.RemoveChild(packagesConfigItem);
            if (removePackagesConfig)
            {
                File.Delete(path);
            }

            return projectRootElement;
        }

        private static void AddPackageReferenceElement(ProjectItemGroupElement packageReferencesItemGroup, string packageName, string? packageVersion)
        {
            var packageReference = packageReferencesItemGroup.AddItem(PackageFacts.PackageReferenceItemType, packageName);
            packageReference.GetXml().SetAttribute(PackageFacts.VersionAttribute, packageVersion);
        }

        public static IProjectRootElement AddDesktopProperties(this IProjectRootElement projectRootElement, BaselineProject baselineProject)
        {
            // Don't create a new prop group; put the desktop properties in the same group as where TFM is located
            var propGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroupWithTFM(projectRootElement);

            if (!baselineProject.GlobalProperties.Contains(DesktopFacts.UseWinFormsPropertyName, StringComparer.OrdinalIgnoreCase)
                && MSBuildHelpers.IsWinForms(projectRootElement))
            {
                MSBuildHelpers.AddUseWinForms(propGroup);
            }

            if (!baselineProject.GlobalProperties.Contains(DesktopFacts.UseWPFPropertyName, StringComparer.OrdinalIgnoreCase)
                && MSBuildHelpers.IsWPF(projectRootElement))
            {
                MSBuildHelpers.AddUseWPF(propGroup);
            }

            if (!baselineProject.GlobalProperties.Contains(DesktopFacts.UseWinUIPropertyName, StringComparer.OrdinalIgnoreCase)
                && MSBuildHelpers.IsUwp(projectRootElement))
            {
                MSBuildHelpers.AddUseWinUI(propGroup);
            }

            if (!baselineProject.GlobalProperties.Contains(DesktopFacts.ImportWindowsDesktopTargetsName, StringComparer.OrdinalIgnoreCase)
             && !projectRootElement.Sdk.Equals(DesktopFacts.WinSDKAttribute)
             && (MSBuildHelpers.IsWPF(projectRootElement) || MSBuildHelpers.IsWinForms(projectRootElement)))
            {
                MSBuildHelpers.AddImportWindowsDesktopTargets(propGroup);
            }

            return projectRootElement;
        }

        public static IProjectRootElement AddCommonPropertiesToTopLevelPropertyGroup(this IProjectRootElement projectRootElement)
        {
            var propGroups = projectRootElement.PropertyGroups;

            // If there is only 1, it's the top-level group.
            // If there are only 2, then the remaining group has unqiue properties in it that may be configuration-specific.
            if (propGroups.Count <= 2)
            {
                return projectRootElement;
            }

            var pairs = propGroups.Zip(propGroups.Skip(1), (pgA, pgB) => (pgA, pgB))
                                  .Where(pair => MSBuildHelpers.ArePropertyGroupElementsIdentical(pair.pgA, pair.pgB));

            var topLevelPropGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroupWithTFM(projectRootElement);

            foreach (var (a, b) in pairs)
            {
                foreach (var prop in a.Properties)
                {
                    if (prop.Parent is { })
                    {
                        a.RemoveChild(prop);
                    }

                    if (!topLevelPropGroup.Properties.Any(p => ProjectPropertyHelpers.ArePropertiesEqual(p, prop)))
                    {
                        topLevelPropGroup.AppendChild(prop);
                    }
                }

                foreach (var prop in b.Properties)
                {
                    if (prop.Parent is { })
                    {
                        b.RemoveChild(prop);
                    }
                }

                if (a.Parent is { })
                {
                    projectRootElement.RemoveChild(a);
                }

                if (b.Parent is { })
                {
                    projectRootElement.RemoveChild(b);
                }
            }

            return projectRootElement;
        }

        public static IProjectRootElement AddGenerateAssemblyInfoAsFalse(this IProjectRootElement projectRootElement, ProjectStyle projectStyle)
        {
            //Skip adding this for .NET MAUI conversion
            if ((projectStyle is ProjectStyle.XamarinDroid) || (projectStyle is ProjectStyle.XamariniOS))
                return projectRootElement;

            // Don't create a new prop group; put the desktop properties in the same group as where TFM is located
            var propGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroupWithTFM(projectRootElement);
            var generateAssemblyInfo = projectRootElement.CreatePropertyElement(MSBuildFacts.GenerateAssemblyInfoNodeName);
            generateAssemblyInfo.Value = "false";
            propGroup.AppendChild(generateAssemblyInfo);
            return projectRootElement;
        }

        public static IProjectRootElement ModifyProjectElement(this IProjectRootElement projectRootElement)
        {
            projectRootElement.ToolsVersion = null;
            projectRootElement.DefaultTargets = null;
            return projectRootElement;
        }

        public static IProjectRootElement AddTargetFrameworkProperty(this IProjectRootElement projectRootElement, BaselineProject baselineProject, string tfm)
        {
            var propGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroup(baselineProject, projectRootElement);
            var targetFrameworkElement = projectRootElement.CreatePropertyElement(MSBuildFacts.TargetFrameworkNodeName);
            targetFrameworkElement.Value = tfm;
            propGroup.PrependChild(targetFrameworkElement);
            return projectRootElement;
        }

        public static IProjectRootElement RemoveWebExtensions(this IProjectRootElement projectRootElement, ProjectStyle projectStyle)
        {
            // ASP.NET apps often contain project extensions that aren't used in ASP.NET Core
            if (projectStyle == ProjectStyle.Web)
            {
                var extensions = projectRootElement.ProjectExtensions;
                if (extensions != null)
                {
                    projectRootElement.RemoveChild(extensions);
                }
            }

            return projectRootElement;
        }
    }
}
