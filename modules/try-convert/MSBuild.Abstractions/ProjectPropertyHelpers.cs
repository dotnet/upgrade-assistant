using System;
using System.Linq;

using Microsoft.Build.Construction;

using MSBuild.Conversion.Facts;

namespace MSBuild.Abstractions
{
    /// <summary>
    /// Helper functions for working with ProjectPropertyElements
    /// </summary>
    public static class ProjectPropertyHelpers
    {
        /// <summary>
        /// Checks if the given property is the 'Name' property, and if its value is the same as the project file name.
        /// </summary>
        public static bool IsNameDefault(ProjectPropertyElement prop, string projectName) =>
            // TODO use: prop.ContainingProject ?
            prop.ElementName.Equals(MSBuildFacts.NameNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(projectName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'DefineConstants', and if the values defined are the defaults brought in by a template.
        /// </summary>
        public static bool IsDefineConstantDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.DefineConstantsName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Split(';').All(constant => MSBuildFacts.DefaultDefineConstants.Contains(constant, StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if the given property is 'DebugType', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsDebugTypeDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.DebugTypeName, StringComparison.OrdinalIgnoreCase)
            && MSBuildFacts.DefaultDebugTypes.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'OutputPath', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsOutputPathDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputPathName, StringComparison.OrdinalIgnoreCase)
            && MSBuildFacts.DefaultOutputPaths.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'PlatformTarget', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsPlatformTargetDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.PlatformTargetName, StringComparison.OrdinalIgnoreCase)
            && MSBuildFacts.DefaultPlatformTargets.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'DocumentationFile', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsDocumentationFileDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.DocumentationFileNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(MSBuildFacts.DefaultDocumentationFileLocation, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a property is any of the unencessary test properties. This is only good to do <em>after</em> the loading phase where we discard inapplicable test projects.
        /// </summary>
        public static bool IsUnnecessaryTestProperty(ProjectPropertyElement prop) =>
            IsUITestExtensionsPackagesReferencePath(prop) ||
            prop.ElementName.Equals(MSTestFacts.IsCodedUITestNodeName, StringComparison.OrdinalIgnoreCase) ||
            prop.ElementName.Equals(MSTestFacts.TestProjectTypeNodeName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a property is the old NuGetPackageImportStamp property that used to be for some reason starting with VS 2013, but seems to no longer be required, but is still<em>stamped</em>(har har har...) into test project files and maybe others (lol).
        /// </summary>
        public static bool IsEmptyNuGetPackageImportStamp(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.NuGetPackageImportStampNodeName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a property is a default ReferencePath property that MSTest stamps into the project file.
        /// </summary>
        public static bool IsUITestExtensionsPackagesReferencePath(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.ReferencePathNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.ContainsIgnoreCase(MSTestFacts.UITestExtensionPackagesReferencePathFileName);

        public static bool IsOutputTypeNode(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputTypeNodeName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if the name and value of two properties are identical.
        /// </summary>
        public static bool ArePropertiesEqual(ProjectPropertyElement a, ProjectPropertyElement b) =>
            a.ElementName.Equals(b.ElementName, StringComparison.OrdinalIgnoreCase)
            && a.Value.Equals(b.Value, StringComparison.OrdinalIgnoreCase)
            && a.Condition.Equals(b.Condition, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a property is '<ProjectTypeGuids>'
        /// </summary>
        public static bool IsProjectTypeGuidsNode(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.ProjectTypeGuidsNodeName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if a property lists the default project type GUIds for legacy web projects.
        /// </summary>
        public static bool IsLegacyWebProjectTypeGuidsProperty(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').Any(guidString => WebFacts.LegacyWebProjectTypeGuids.Contains(Guid.Parse(guidString)));

        /// <summary>
        /// Checks if a given OutputType node is wither a library, exe, or WinExe.
        /// </summary>
        public static bool IsSupportedOutputType(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputTypeNodeName, StringComparison.OrdinalIgnoreCase)
            && (prop.Value.Equals(MSBuildFacts.LibraryOutputType, StringComparison.OrdinalIgnoreCase)
                || prop.Value.Equals(MSBuildFacts.ExeOutputType, StringComparison.OrdinalIgnoreCase)
                || prop.Value.Equals(MSBuildFacts.WinExeOutputType, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if a given property defines project type guids for C#, VB.NET, or F#.
        /// </summary>
        public static bool AllProjectTypeGuidsAreLanguageProjectTypeGuids(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').All(guidString => MSBuildFacts.LanguageProjectTypeGuids.Contains(Guid.Parse(guidString)));

        /// <summary>
        /// Checks if all projecttypeguids specified are known desktop project type guids.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static bool AllProjectTypeGuidsAreDesktopProjectTypeGuids(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').All(guidString => DesktopFacts.KnownSupportedDesktopProjectTypeGuids.Contains(Guid.Parse(guidString)));

        /// <summary>
        /// Checks if all projecttypeguids specified are known desktop project type guids.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static bool AllProjectTypeGuidsAreLegacyTestProjectTypeGuids(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').All(guidString => MSTestFacts.KnownOldMSTestProjectTypeGuids.Contains(Guid.Parse(guidString)));

        /// <summary>
        /// Checks if a given property specifies IsCodedUITest=True, which is not only unsupported for .NET Core but is also deprecated after VS 2019.
        /// </summary>
        public static bool IsCodedUITest(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSTestFacts.IsCodedUITestNodeName, StringComparison.OrdinalIgnoreCase) && prop.Value.Equals("True", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a test property is TestProjectType=UnitTest
        /// </summary>
        public static bool IsUnitTestType(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSTestFacts.TestProjectTypeNodeName, StringComparison.OrdinalIgnoreCase) && prop.Value.Equals(MSTestFacts.UnitTestTestProjectType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if an OutputType node is Library.
        /// </summary>
        public static bool IsLibraryOutputType(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputTypeNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(MSBuildFacts.LibraryOutputType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if an OutputType node is Exe.
        /// </summary>
        public static bool IsExeOutputType(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputTypeNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(MSBuildFacts.ExeOutputType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if an OutputType node is AppContainerExe.
        /// </summary>
        public static bool IsAppContainerExeOutputType(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputTypeNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(MSBuildFacts.AppContainerExeOutputType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if an OutputType node is Exe.
        /// </summary>
        public static bool IsWinExeOutputType(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputTypeNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(MSBuildFacts.WinExeOutputType, StringComparison.OrdinalIgnoreCase);

        public static bool IsVisualBasicProject(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').Any(guidString => Guid.Parse(guidString) == MSBuildFacts.LanguageProjectTypeVisualBasic);

        /// <summary>
        /// Determines if a property lists the default project type GUId for Xamarin.Android.
        /// </summary>
        public static bool IsXamarinDroidProjectTypeGuidsProperty(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').Any(guidString => XamarinFacts.XamarinDroidProjectTypeGuids.Contains(Guid.Parse(guidString)));

        /// <summary>
        /// Determines if a property lists the default project type GUId for Xamarin.iOS projects.
        /// </summary>
        public static bool IsXamariniOSProjectTypeGuidsProperty(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').Any(guidString => XamarinFacts.XamariniOSProjectTypeGuids.Contains(Guid.Parse(guidString)));


    }
}
