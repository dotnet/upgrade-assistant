// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    internal static class Paths
    {
        private const string DotnetHomeVariableName = "DOTNET_CLI_HOME";
        private const string DotnetProfileDirectoryName = ".dotnet";
        private const string ToolsShimFolderName = "tools";

        static Paths()
        {
            UserProfile = Environment.GetEnvironmentVariable(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "USERPROFILE"
                    : "HOME");

            DotnetToolsPath = Path.Combine(UserProfile, DotnetProfileDirectoryName, ToolsShimFolderName);

            var nugetPackagesEnvironmentVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            NugetCache = string.IsNullOrWhiteSpace(nugetPackagesEnvironmentVariable)
                             ? Path.Combine(UserProfile, ".nuget", "packages")
                             : nugetPackagesEnvironmentVariable;
        }

        public static string DotnetUserProfileFolderPath =>
            Path.Combine(DotnetHomePath, DotnetProfileDirectoryName);

        public static string DotnetHomePath
        {
            get
            {
                var home = Environment.GetEnvironmentVariable(DotnetHomeVariableName);
                if (string.IsNullOrEmpty(home))
                {
                    home = UserProfile;
                    if (string.IsNullOrEmpty(home))
                    {
                        throw new DirectoryNotFoundException();
                    }
                }

                return home;
            }
        }

        public static string DotnetToolsPath { get; }

        public static string UserProfile { get; }

        public static string NugetCache { get; }

        public static readonly string InstallDirectory = Path.GetDirectoryName(typeof(Paths).Assembly.Location);
    }
}
