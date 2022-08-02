using System;
using System.Linq;

using Microsoft.Build.Construction;

using MSBuild.Conversion.Facts;

namespace MSBuild.Abstractions
{
    /// <summary>
    /// Helper functions for working with ProjectItemElements
    /// </summary>
    public static class ProjectItemHelpers
    {
        /// <summary>
        /// Checks if a given item is a PackageReference node.
        /// </summary>
        public static bool IsPackageReference(ProjectItemElement element) =>
            element.ElementName.Equals(PackageFacts.PackageReferenceItemType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a given item is a specific PackageReference.
        /// </summary>
        public static bool IsSpecificPacakgeReference(ProjectItemElement element, string packageName) =>
            element.ElementName.Equals(PackageFacts.PackageReferenceItemType, StringComparison.OrdinalIgnoreCase)
            && element.Include.Equals(packageName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a given item needs to be removed because it either only runs on desktop .NET or is automatically pulled in as a reference and is thus unnecessary.
        /// </summary>
        public static bool DesktopReferencesNeedsRemoval(ProjectItemElement item) =>
            !(item.ElementName.Equals("import", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(item.Include))
            && (DesktopFacts.ReferencesThatNeedRemoval.Contains(item.Include, StringComparer.OrdinalIgnoreCase)
                || DesktopFacts.KnownWPFReferences.Contains(item.Include, StringComparer.OrdinalIgnoreCase)
                || DesktopFacts.KnownWinFormsReferences.Contains(item.Include, StringComparer.OrdinalIgnoreCase)
                || DesktopFacts.KnownDesktopReferences.Contains(item.Include, StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if a given item is a desktop item that is globbed, so long as the metadata is a form type.
        /// </summary>
        public static bool IsDesktopRemovableGlobbedItem(ProjectStyle style, ProjectItemElement item) =>
            style == ProjectStyle.WindowsDesktop
            && MSBuildFacts.GlobbedItemTypes.Contains(item.ElementName, StringComparer.OrdinalIgnoreCase)
            && (item.Metadata.Any(pme => pme.Name.Equals(MSBuildFacts.SubTypeNodeName, StringComparison.OrdinalIgnoreCase)
                                         && pme.Value.Equals(DesktopFacts.FormSubTypeValue, StringComparison.OrdinalIgnoreCase)) ||
                item.Metadata.Any(pme => pme.Name.Equals(MSBuildFacts.DependentUponName, StringComparison.OrdinalIgnoreCase) && pme.Value.EndsWith(DesktopFacts.XamlFileExtension)));

        /// <summary>
        /// Checks if a given item is a well-known reference that can be converted to PackageReference.
        /// </summary>
        public static bool IsReferenceConvertibleToPackageReference(ProjectItemElement item) =>
            !item.ElementName.Equals("import", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.Include)
            && MSBuildFacts.DefaultItemsThatHavePackageEquivalents.ContainsKey(item.Include);

        /// <summary>
        /// Checks if a reference is coming from an old-stlye NuGet package.
        /// </summary>
        public static bool IsReferenceComingFromOldNuGet(ProjectItemElement item) =>
            item.ElementName.Equals(MSBuildFacts.MSBuildReferenceName)
            && item.Metadata.Any(pme => pme.ElementName.Equals(MSBuildFacts.HintPathNodeName, StringComparison.OrdinalIgnoreCase)
                                        && pme.Value.ContainsIgnoreCase("packages")
                                        && pme.Value.ContainsIgnoreCase($"\\lib\\"));

        /// <summary>
        /// Checks if a given item is a well-known item that has unnecessary metadata.
        /// </summary>
        public static bool CanItemMetadataBeRemoved(ProjectItemElement item) =>
            MSBuildFacts.ItemsThatCanHaveMetadataRemoved.Contains(item.ElementName, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a given item is a XAML designer item (which will be globbed).
        /// </summary>
        public static bool IsLegacyXamlDesignerItem(ProjectItemElement item) =>
            item.Include.EndsWith(DesktopFacts.XamlFileExtension, StringComparison.OrdinalIgnoreCase)
            && item.Metadata.Any(pme => pme.Name.Equals(MSBuildFacts.SubTypeNodeName, StringComparison.OrdinalIgnoreCase)
                                       && pme.Value.Equals(MSBuildFacts.DesignerSubType, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if a given item has DependentUpon metadata for a globbed designer (and can thus be globbed).
        /// </summary>
        public static bool IsDependentUponXamlDesignerItem(ProjectItemElement item) =>
            item.Metadata.Any(pme => pme.Name.Equals(MSBuildFacts.SubTypeNodeName, StringComparison.OrdinalIgnoreCase)
                                     && pme.Value.Equals(MSBuildFacts.CodeSubTypeValue, StringComparison.OrdinalIgnoreCase))
            && item.Metadata.Any(pme => pme.Name.Equals(MSBuildFacts.DependentUponName, StringComparison.OrdinalIgnoreCase)
                                        && pme.Value.EndsWith(DesktopFacts.XamlFileExtension, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if a given item is a source file with needless metadata (example: source files in WPF templates).
        /// </summary>
        public static bool IsItemWithUnnecessaryMetadata(ProjectItemElement item) =>
            MSBuildFacts.GlobbedItemTypes.Contains(item.ElementName, StringComparer.OrdinalIgnoreCase)
            && item.Metadata.Any(pme => pme.Name.Equals(MSBuildFacts.SubTypeNodeName, StringComparison.OrdinalIgnoreCase)
                                        && pme.Value.Equals(MSBuildFacts.CodeSubTypeValue, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if the given item is a designer file.
        /// </summary>
        /// <param name="item">The ProjectItemElement that might be a designer file.</param>
        /// <returns>true if the given ProjectItemElement is a designer file.</returns>
        public static bool IsDesignerFile(ProjectItemElement item) =>
            item.Include.EndsWith(DesktopFacts.DesignerSuffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given item is a resx file.
        /// </summary>
        public static bool IsResxFile(ProjectItemElement item) =>
            item.Include.EndsWith(DesktopFacts.ResourcesFileSuffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given item is a settings file.
        /// </summary>
        public static bool IsSettingsFile(ProjectItemElement item) =>
            item.Include.EndsWith(DesktopFacts.SettingsFileSuffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if an item is an explicit System.ValueTuple and if the given TFM correspondes with an in-box System.ValueTuple type.
        /// </summary>
        public static bool IsExplicitValueTupleReferenceThatCanBeRemoved(ProjectItemElement item, string tfm) =>
            item.ElementName.Equals(MSBuildFacts.MSBuildReferenceName, StringComparison.OrdinalIgnoreCase)
            && item.Include.Equals(MSBuildFacts.SystemValueTupleName, StringComparison.OrdinalIgnoreCase)
            && MSBuildHelpers.FrameworkHasAValueTuple(tfm);

        /// <summary>
        /// Checks if a given item is a reference to System.Web, which is 100% incompatible with .NET Core.
        /// </summary>
        public static bool IsReferencingSystemWeb(ProjectItemElement item) =>
            item.ElementName.Equals(MSBuildFacts.MSBuildReferenceName, StringComparison.OrdinalIgnoreCase)
            && item.Include.Equals(MSBuildFacts.SystemWebReferenceName, StringComparison.OrdinalIgnoreCase);

        public static bool IsReferencingSettingsSingleFileGenerator(ProjectItemElement item)
        {
            var metadata = item.Children.FirstOrDefault(child => MSBuildFacts.EmbeddedResourceGeneratorProperty.Equals(child.ElementName, StringComparison.Ordinal) ) as ProjectMetadataElement;
            return metadata is null ? false : metadata.Value.Equals(MSBuildFacts.SettingsSingleFileGenerator, StringComparison.Ordinal);
        }

    }
}
