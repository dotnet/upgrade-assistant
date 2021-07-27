// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal static class MSBuildConstants
    {
        // Common target imports

        // Property and element names
        public const string OutputTypePropertyName = "OutputType";
        public const string FrameworkReferenceType = "FrameworkReference";
        public const string PackageReferenceType = "PackageReference";
        public const string ReferenceType = "Reference";
        public const string VersionElementName = "Version";

        // Property values
        public const string LibraryPropertyValue = "Library";
        public const string ExePropertyValue = "Exe";
        public const string WinExePropertyValue = "WinExe";

        // SDKs
        public const string DefaultSDK = "Microsoft.NET.Sdk";

        private const string WebSdk = "Microsoft.NET.Sdk.Web";

        public static readonly string[] SDKsWithExeDefaultOutputType = new[]
        {
            WebSdk
        };
    }
}
