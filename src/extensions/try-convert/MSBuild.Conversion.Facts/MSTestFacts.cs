using System;
using System.Collections.Immutable;

namespace MSBuild.Conversion.Facts
{
    public static class MSTestFacts
    {
        public static ImmutableArray<string> MSTestProps => ImmutableArray.Create(
            "MSTest.TestAdapter.props"
        );

        public static ImmutableArray<string> MSTestTargets => ImmutableArray.Create(
            "Microsoft.TestTools.targets",
            "MSTest.TestAdapter.targets"
        );

        public static ImmutableArray<Guid> KnownOldMSTestProjectTypeGuids => ImmutableArray.Create(
            MSBuildFacts.LanguageProjectTypeVisualBasic, // VB.NET
            Guid.Parse("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"), // C#
            Guid.Parse("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}") // Test
        );

        public static ImmutableArray<string> MSTestReferences = ImmutableArray.Create(
            "Microsoft.VisualStudio.TestPlatform.TestFramework",
            "Microsoft.VisualStudio.TestPlatform.TestFramework.Extensions"
        );

        public const string IsCodedUITestNodeName = "IsCodedUITest";
        public const string TestProjectTypeNodeName = "TestProjectType";
        public const string UnitTestTestProjectType = "UnitTest";
        public const string UITestExtensionPackagesReferencePathFileName = "UITestExtensionPackages";
        public const string MSTestSDKPackageName = "Microsoft.NET.Test.Sdk";
        public const string MSTestSDKDev16FloatingVersion = "16.*"; // This is probably fine
    }
}
