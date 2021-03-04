// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal static class MSBuildConstants
    {
        // Common target imports
        public const string WebApplicationTargets = "Microsoft.WebApplication.targets";

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
        public const string DesktopSdk = "Microsoft.NET.Sdk.Desktop";
        public const string WebSdk = "Microsoft.NET.Sdk.Web";

        public static readonly string[] SDKsWithExeDefaultOutputType = new[]
        {
            WebSdk
        };

        public static readonly string[] WebFrameworkReferences = new[]
        {
            "Microsoft.AspNetCore.App"
        };

        public static readonly string[] WinFormsFrameworkReferences = new[]
        {
            "Microsoft.WindowsDesktop.App.WindowsForms",
        };

        public static readonly string[] WpfFrameworkReferences = new[]
        {
            "Microsoft.WindowsDesktop.App.WPF",
        };

        public static readonly string[] DesktopFrameworkReferences = new[]
        {
            "Microsoft.WindowsDesktop.App",
            "Microsoft.WindowsDesktop.App.WindowsForms",
            "Microsoft.WindowsDesktop.App.WPF"
        };

        public static readonly string[] WebReferences = new[]
        {
            "System.Web",
            "System.Web.Abstractions",
            "System.Web.Routing"
        };

        public static readonly string[] WinFormsReferences = new[]
        {
            "System.Windows.Forms"
        };

        public static readonly string[] WpfReferences = new[]
        {
            "System.Xaml",
            "PresentationCore",
            "PresentationFramework",
            "WindowsBase"
        };

        public static readonly string[] WinRTPackages = new[]
        {
            "Microsoft.Windows.SDK.Contracts"
        };
    }
}
