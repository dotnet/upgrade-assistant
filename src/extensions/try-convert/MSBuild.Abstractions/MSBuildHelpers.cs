using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Build.Construction;
using Microsoft.Build.Locator;
using MSBuild.Conversion.Facts;

namespace MSBuild.Abstractions
{
    /// <summary>
    /// Static helper methods for working with general MSBuildisms.
    /// </summary>
    public static class MSBuildHelpers
    {
        /// <summary>
        /// matches $(name) pattern
        /// </summary>
        private static readonly Regex s_dimensionNameInConditionRegex = new Regex(@"^\$\(([^\$\(\)]*)\)$");

        /// <summary>
        /// Converts configuration dimensional value vector to a msbuild condition
        /// Use the standard format of
        /// '$(DimensionName1)|$(DimensionName2)|...|$(DimensionNameN)'=='DimensionValue1|...|DimensionValueN'
        /// </summary>
        /// <param name="dimensionalValues">vector of configuration dimensional properties</param>
        /// <returns>msbuild condition representation</returns>
        internal static string DimensionalValuePairsToCondition(ImmutableDictionary<string, string> dimensionalValues)
        {
            if (null == dimensionalValues || 0 == dimensionalValues.Count)
            {
                return string.Empty; // no condition. Returns empty string to match MSBuild.
            }

            var left = string.Empty;
            var right = string.Empty;

            foreach (var key in dimensionalValues.Keys)
            {
                if (!string.IsNullOrEmpty(left))
                {
                    left += "|";
                    right += "|";
                }

                left += "$(" + key + ")";
                right += dimensionalValues[key];
            }

            var condition = "'" + left + "'=='" + right + "'";
            return condition;
        }

        /// <summary>
        /// Returns a name of a configuration like Debug|AnyCPU
        /// </summary>
        public static string GetConfigurationName(ImmutableDictionary<string, string> dimensionValues) => dimensionValues.IsEmpty ? "" : dimensionValues.Values.Aggregate((x, y) => $"{x}|{y}");

        /// <summary>
        /// Returns a name of a configuration like Debug|AnyCPU
        /// </summary>
        public static string GetConfigurationName(string condition)
        {
            return ConditionToDimensionValues(condition, out var dimensionValues) ? GetConfigurationName(dimensionValues) : "";
        }

        /// <summary>
        /// Tries to parse an MSBuild condition to a dimensional vector
        /// only matches standard pattern:
        /// '$(DimensionName1)|$(DimensionName2)|...|$(DimensionNameN)'=='DimensionValue1|...|DimensionValueN'
        /// </summary>
        /// <param name="condition">msbuild condition string</param>
        /// <param name="dimensionalValues">configuration dimensions vector (output)</param>
        /// <returns>true on success</returns>
        public static bool ConditionToDimensionValues(string condition, out ImmutableDictionary<string, string> dimensionalValues)
        {
            string left;
            string right;
            dimensionalValues = ImmutableDictionary<string, string>.Empty;

            if (string.IsNullOrEmpty(condition))
            {
                // yes empty condition is recognized as a empty dimension vector
                return true;
            }

            var equalPos = condition.IndexOf("==", StringComparison.OrdinalIgnoreCase);
            if (equalPos <= 0)
            {
                return false;
            }

            left = condition.Substring(0, equalPos).Trim();
            right = condition.Substring(equalPos + 2).Trim();

            // left and right needs to ba a valid quoted strings
            if (!UnquoteString(ref left) || !UnquoteString(ref right))
            {
                return false;
            }

            var dimensionNamesInCondition = left.Split(new char[] { '|' });
            var dimensionValuesInCondition = right.Split(new char[] { '|' });

            // number of keys need to match number of values
            if (dimensionNamesInCondition.Length == 0 || dimensionNamesInCondition.Length != dimensionValuesInCondition.Length)
            {
                return false;
            }

            var parsedDimensionalValues = new Dictionary<string, string>(dimensionNamesInCondition.Length);

            for (var i = 0; i < dimensionNamesInCondition.Length; i++)
            {
                // matches "$(name)" patern.
                var match = s_dimensionNameInConditionRegex.Match(dimensionNamesInCondition[i]);
                if (!match.Success)
                {
                    return false;
                }

                var dimensionName = match.Groups[1].ToString();
                if (string.IsNullOrEmpty(dimensionName))
                {
                    return false;
                }

                parsedDimensionalValues[dimensionName] = dimensionValuesInCondition[i];
            }

            dimensionalValues = parsedDimensionalValues.ToImmutableDictionary();
            return true;
        }

        /// <summary>
        /// Given a TFM string, determines if that TFM has an explicit System.ValueTuple reference.
        /// </summary>
        public static bool FrameworkHasAValueTuple(string tfm) =>
            tfm.StartsWith(MSBuildFacts.NetstandardPrelude, StringComparison.OrdinalIgnoreCase)
            || tfm.StartsWith(MSBuildFacts.NetcoreappPrelude, StringComparison.OrdinalIgnoreCase)
            || tfm.StartsWith(MSBuildFacts.Net5, StringComparison.OrdinalIgnoreCase)
            || tfm.StartsWith(MSBuildFacts.Net6, StringComparison.OrdinalIgnoreCase)
            || tfm.StartsWith(MSBuildFacts.LowestFrameworkVersionWithSystemValueTuple, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets all Reference items from a given item group.
        /// </summary>
        private static IEnumerable<ProjectItemElement> GetReferences(ProjectItemGroupElement itemGroup) =>
            itemGroup.Items.Where(item => item.ElementName.Equals(MSBuildFacts.MSBuildReferenceName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets all PackageReference items from a given item group.
        /// </summary>
        private static IEnumerable<ProjectItemElement> GetPackageReferences(ProjectItemGroupElement itemGroup) =>
            itemGroup.Items.Where(item => item.ElementName.Equals(MSBuildFacts.MSBuildPackageReferenceName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if a root has a project type guids node.
        /// </summary>
        public static bool HasProjectTypeGuidsNode(IProjectRootElement root) =>
            root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsProjectTypeGuidsNode));

        /// <summary>
        /// Gets a flat list of all project type guids.
        /// </summary>
        public static IEnumerable<string> GetAllProjectTypeGuids(IProjectRootElement root) =>
            root.PropertyGroups.SelectMany(pg => pg.Properties.Where(prop => prop.ElementName.Equals(MSBuildFacts.ProjectTypeGuidsNodeName))
                                                              .SelectMany(prop => prop.Value.Split(';')));

        /// <summary>
        /// Determines if a given project is a WPF project by looking at its references.
        /// </summary>
        public static bool IsWPF(IProjectRootElement projectRoot)
        {
            var references = projectRoot.ItemGroups.SelectMany(GetReferences)?.Select(elem => elem.Include.Split(',').First());
            if (references is null)
            {
                return false;
            }
            else
            {
                return DesktopFacts.KnownWPFReferences.Any(reference => references.Contains(reference, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Determines if a given project is a WinForms project by looking at its references.
        /// </summary>
        public static bool IsWinForms(IProjectRootElement projectRoot)
        {
            var references = projectRoot.ItemGroups.SelectMany(GetReferences)?.Select(elem => elem.Include.Split(',').First());
            if (references is null)
            {
                return false;
            }
            else
            {
                return DesktopFacts.KnownWinFormsReferences.Any(reference => references.Contains(reference, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Determines if a given project references Desktop assemblies.
        /// </summary>
        public static bool IsDesktop(IProjectRootElement projectRoot)
        {
            var references = projectRoot.ItemGroups.SelectMany(GetReferences)?.Select(elem => elem.Include.Split(',').First());
            if (references is null)
            {
                return false;
            }
            else
            {
                return DesktopFacts.KnownDesktopReferences.Any(reference => references.Contains(reference, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Determines if a given project is UWP
        /// </summary>
        public static bool IsUwp(IProjectRootElement projectRoot)
        {
            if (projectRoot.PropertyGroups.Any(g => g.Properties.Any(p => p.Name == "TargetPlatformIdentifier" && p.Value == "UAP")))
            {
                return true;
            }
            var packageReferences = projectRoot.ItemGroups.SelectMany(GetPackageReferences)?.Select(elem => elem.Include.Split(',').First());
            if (packageReferences is null)
            {
                return false;
            }
            else
            {
                return DesktopFacts.KnownUwpReferences.Any(reference => packageReferences.Contains(reference, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Determines if a given project references ASP.NET assemblies.
        /// </summary>
        public static bool IsWeb(IProjectRootElement projectRoot)
        {
            var references = projectRoot.ItemGroups.SelectMany(GetReferences)?.Select(elem => elem.Include.Split(',').First());
            if (references is null)
            {
                return false;
            }
            else
            {
                return WebFacts.KnownWebReferences.All(reference => references.Contains(reference, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Determines if a given project uses ASP.NET web app project type guid
        /// </summary>
        public static bool IsWebApp(IProjectRootElement projectRoot) =>
            projectRoot.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsLegacyWebProjectTypeGuidsProperty));

        /// <summary>
        /// Determines if a project should be treated as an ASP.NET Core project (as opposed to classic ASP.NET). Returns
        /// true if the project is a web app (since there's no good way to build an ASP.NET app with an SDK-style project
        /// except to upgrade to ASP.NET Core) or the project has web dependencies and will target .NET/.NET Core.
        /// </summary>
        public static bool IsAspNetCore(IProjectRootElement projectRoot, string tfm) =>
            IsWebApp(projectRoot) || projectRoot.Sdk.Equals(WebFacts.WebSDKAttribute)
            || (IsWeb(projectRoot) && new[] { MSBuildFacts.Net6, MSBuildFacts.Net5, MSBuildFacts.NetcoreappPrelude }.Any(s => tfm.StartsWith(s, StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// Determines if a project is a .NET Framework MSTest project by looking at its references.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static bool IsNETFrameworkMSTestProject(IProjectRootElement projectRoot)
        {
            var references = projectRoot.ItemGroups.SelectMany(GetReferences)?.Select(elem => elem.Include.Split(',').First());
            if (references is null)
            {
                return false;
            }
            else
            {
                return MSTestFacts.MSTestReferences.All(reference => references.Contains(reference, StringComparer.OrdinalIgnoreCase));
            }
        }

        public static bool HasWPFOrWinForms(ProjectPropertyGroupElement propGroup)
        {
            return (propGroup.Properties.Any(p => StringComparer.OrdinalIgnoreCase.Compare(p.Name, DesktopFacts.UseWPFPropertyName) == 0) ||
                    propGroup.Properties.Any(p => StringComparer.OrdinalIgnoreCase.Compare(p.Name, DesktopFacts.UseWinFormsPropertyName) == 0));
        }

        /// <summary>
        /// Checks if a given TFM is not .NET Framework.
        /// </summary>
        public static bool IsNotNetFramework(string tfm) =>
            !tfm.ContainsIgnoreCase(MSBuildFacts.NetcoreappPrelude)
            && !tfm.ContainsIgnoreCase(MSBuildFacts.NetstandardPrelude);

        /// <summary>
        /// Checks if a given TFM include -windows
        /// </summary>
        public static bool IsWindows(string tfm) =>
            tfm.ContainsIgnoreCase(MSBuildFacts.WindowsSuffix);

        /// <summary>
        /// Finds the item group where a packages.config is included. Assumes only one.
        /// </summary>
        public static ProjectItemGroupElement? GetPackagesConfigItemGroup(IProjectRootElement root) =>
            root.ItemGroups.FirstOrDefault(pige => pige.Items.Any(pe => pe.Include.Equals(PackageFacts.PackagesConfigIncludeName, StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// Finds the packages.config item in its containing item group.
        /// </summary>
        public static ProjectItemElement GetPackagesConfigItem(ProjectItemGroupElement packagesConfigItemGroup) =>
            packagesConfigItemGroup.Items.Single(pe => pe.Include.Equals(PackageFacts.PackagesConfigIncludeName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Adds the UseWindowsForms=True property to the top-level project property group.
        /// </summary>
        public static void AddUseWinForms(ProjectPropertyGroupElement propGroup) => propGroup.AddProperty(DesktopFacts.UseWinFormsPropertyName, "true");

        /// <summary>
        /// Adds the UseWPF=true property to the top-level project property group.
        /// </summary>
        public static void AddUseWPF(ProjectPropertyGroupElement propGroup) => propGroup.AddProperty(DesktopFacts.UseWPFPropertyName, "true");

        /// <summary>
        /// Adds the UseWinUI=true property to the top-level project property group.
        /// </summary>
        public static void AddUseWinUI(ProjectPropertyGroupElement propGroup) => propGroup.AddProperty(DesktopFacts.UseWinUIPropertyName, "true");

        /// <summary>
        /// Adds the ImportWindowsDesktopTargets=true property to ensure builds targeting .NET Framework will succeed.
        /// </summary>
        /// <param name="propGroup"></param>
        public static void AddImportWindowsDesktopTargets(ProjectPropertyGroupElement propGroup) => propGroup.AddProperty(DesktopFacts.ImportWindowsDesktopTargetsName, "true");

        /// <summary>
        /// Finds the property group with the TFM specified, which is normally the top-level property group.
        /// </summary>
        public static ProjectPropertyGroupElement GetOrCreateTopLevelPropertyGroupWithTFM(IProjectRootElement rootElement) =>
            rootElement.PropertyGroups.Single(pg => pg.Properties.Any(p => p.ElementName.Equals(MSBuildFacts.TargetFrameworkNodeName, StringComparison.OrdinalIgnoreCase)))
            ?? rootElement.AddPropertyGroup();

        /// <summary>
        /// Finds the item group where PackageReferences are specified. Usually there is only one.
        /// </summary>
        public static ProjectItemGroupElement GetOrCreatePackageReferencesItemGroup(IProjectRootElement rootElement) =>
            rootElement.ItemGroups.FirstOrDefault(ig => ig.Items.All(i => i.ElementName.Equals(PackageFacts.PackageReferenceItemType, StringComparison.OrdinalIgnoreCase)))
            ?? rootElement.AddItemGroup();

        /// <summary>
        /// Checks if a metadata item can stay or if it needs to be converted.
        /// </summary>
        /// <returns>True if the metadata item is fine. False if it needs to be removed or converted.</returns>
        public static bool IsValidMetadataForConversionPurposes(IProjectMetadata projectMetadata) =>
            !projectMetadata.Name.Equals(MSBuildFacts.RequiredTargetFrameworkNodeName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if an import is a target that comes from the packages directory (which would mean it's probably brought in via a NuGet package).
        /// </summary>
        public static bool IsTargetFromNuGetPackage(ProjectImportElement import) =>
            import.Project.ContainsIgnoreCase(MSBuildFacts.PackagesSubstring)
            && (import.Project.EndsWith(MSBuildFacts.TargetsSuffix, StringComparison.OrdinalIgnoreCase)
                 || import.Project.EndsWith(MSBuildFacts.PropsSuffix, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the top-level property group, and if it doesn't exist, creates it.
        /// </summary>
        public static ProjectPropertyGroupElement GetOrCreateTopLevelPropertyGroup(BaselineProject baselineProject, IProjectRootElement projectRootElement)
        {
            bool IsAfterFirstImport(ProjectPropertyGroupElement propertyGroup)
            {
                if (baselineProject.ProjectStyle == ProjectStyle.Default
                    || baselineProject.ProjectStyle == ProjectStyle.WindowsDesktop
                    || baselineProject.ProjectStyle == ProjectStyle.DefaultSubset)
                {
                    return true;
                }

                var firstImport = projectRootElement.Imports.Where(i => i.Label != MSBuildFacts.SharedProjectsImportLabel).FirstOrDefault();
                return firstImport is { } && propertyGroup.Location.Line > firstImport.Location.Line;
            }

            return projectRootElement.PropertyGroups.FirstOrDefault(pg => pg.Condition == "" && IsAfterFirstImport(pg))
                   ?? projectRootElement.AddPropertyGroup();
        }

        /// <summary>
        /// Determines if all the properties in two property groups are identical.
        /// </summary>
        public static bool ArePropertyGroupElementsIdentical(ProjectPropertyGroupElement groupA, ProjectPropertyGroupElement groupB)
        {
            return groupA.Properties.Count == groupB.Properties.Count
                   && groupA.Properties.All(propA => groupB.Properties.Any(propB => ProjectPropertyHelpers.ArePropertiesEqual(propA, propB)));
        }

        /// <summary>
        /// Unquote string. It simply removes the starting and ending "'", and checks they are present before.
        /// </summary>
        /// <param name="s">string to unquote </param>
        /// <returns>true if string is successfuly unquoted</returns>
        private static bool UnquoteString(ref string s)
        {
            if (s.Length < 2 || s[0] != '\'' || s[^1] != '\'' || s[1..^1].Contains('\''))
            {
                return false;
            }

            s = s[1..^1];
            return true;
        }

        /// <summary>
        /// Determines if a given project is of type Xamarin.Android.
        /// </summary>
        public static bool IsXamarinDroid(IProjectRootElement projectRoot) =>
           projectRoot.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsXamarinDroidProjectTypeGuidsProperty));

        /// <summary>
        /// Determines if a given project is of type Xamarin.iOS.
        /// </summary>
        public static bool IsXamariniOS(IProjectRootElement projectRoot) =>
           projectRoot.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsXamariniOSProjectTypeGuidsProperty));


        /// <summary>
        /// Given an optional path to MSBuild, registers an MSBuild.exe to be used for assembly resolution with this tool.
        /// </summary>
        public static string? HookAssemblyResolveForMSBuild(string? msbuildPath = null)
        {
            msbuildPath = GetMSBuildPathIfNotSpecified(msbuildPath);
            if (string.IsNullOrWhiteSpace(msbuildPath))
            {
                Console.WriteLine("Cannot find MSBuild. Please pass in a path to msbuild using -m or run from a developer command prompt.");
                return null;
            }

            MSBuildLocator.RegisterMSBuildPath(msbuildPath);

            return msbuildPath;
        }

        /// <summary>
        /// Given an optional path to MSBuild, finds an MSBuild path. Will query Visual Studio instances and ask for user input if there are multitple ones.
        /// </summary>
        private static string? GetMSBuildPathIfNotSpecified(string? msbuildPath = null)
        {
            // If the user specified a msbuild path use that.
            if (!string.IsNullOrEmpty(msbuildPath))
            {
                return msbuildPath;
            }

            // If the user is running from a developer command prompt use the MSBuild of that VS
            var vsinstalldir = Environment.GetEnvironmentVariable("VSINSTALLDIR");
            if (!string.IsNullOrEmpty(vsinstalldir))
            {
                return Path.Combine(vsinstalldir, "MSBuild", "Current", "Bin");
            }

            //Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            return instance?.MSBuildPath;
        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (var i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out var instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine("Input not accepted, try again.");
            }
        }
    }
}
