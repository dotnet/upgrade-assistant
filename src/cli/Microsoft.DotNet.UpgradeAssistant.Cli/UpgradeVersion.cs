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
            Version = Version.Parse(GetVersionFromInformationalVersion(FullVersion));
        }

        public string FullVersion { get; }

        public Version Version { get; }

        /// <summary>
        /// Need to grab just the version part of strings such as: <c>0.2.231602+ef31c22633e629fac65d9f9d8bf700119c888380</c> and <c>0.2.0-dev</c>.
        /// </summary>
        /// <param name="informational">An informational version string.</param>
        /// <returns>A truncated version.</returns>
        private static ReadOnlySpan<char> GetVersionFromInformationalVersion(ReadOnlySpan<char> informational)
        {
            var plusIdx = informational.IndexOf("+", StringComparison.Ordinal);

            if (plusIdx > 0)
            {
                informational = informational[..plusIdx];
            }

            var minusIdx = informational.IndexOf("-", StringComparison.Ordinal);

            if (minusIdx > 0)
            {
                informational = informational[..minusIdx];
            }

            return informational;
        }
    }
}
