// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class Constants
    {
        public static string FullVersion
        {
            get
            {
                var attribute = typeof(Constants).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return attribute!.InformationalVersion;
            }
        }

        public static Version Version => Version.Parse(GetVersionFromInformationalVersion(FullVersion));

        /// <summary>
        /// Need to grab just the version part of strings such as: <c>0.2.231602+ef31c22633e629fac65d9f9d8bf700119c888380</c> and <c>0.2.0-dev</c>.
        /// </summary>
        /// <param name="informational">An informational version string.</param>
        /// <returns>A truncated version.</returns>
        private static string GetVersionFromInformationalVersion(string informational)
        {
            var plusIdx = informational.IndexOf('+', StringComparison.Ordinal);
            var minusIdx = informational.IndexOf('-', StringComparison.Ordinal);

            var idx = Math.Min(plusIdx, minusIdx);

            return idx > 0 ? informational.Substring(0, idx) : informational;
        }
    }
}
