// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class UpgradeVersion
    {
        public static UpgradeVersion Current { get; } = new();

        public UpgradeVersion()
        {
            FullVersion = typeof(UpgradeVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            Version = ConvertToVersion(FullVersion);
        }

        public string FullVersion { get; }

        public Version Version { get; }

        /// <summary>
        /// Need to grab just the version part of strings such as: <c>0.2.231602+ef31c22633e629fac65d9f9d8bf700119c888380</c> and <c>0.2.0-dev</c>.
        /// </summary>
        /// <param name="version">An informational version string.</param>
        /// <returns>A version.</returns>
        private static Version ConvertToVersion(ReadOnlySpan<char> version)
        {
            var idx = version.IndexOfAny("+-");
            var newVersionString = idx > 0 ? version[..idx] : version;

            return Version.Parse(newVersionString);
        }

        public bool IsDevelopment => FullVersion.EndsWith("dev", StringComparison.Ordinal);
    }
}
