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
                return attribute?.InformationalVersion ?? "0.0.0-unspecified";
            }
        }

        public static Version Version
        {
            get
            {
                var version = FullVersion;
                var idx = version.IndexOf('-', StringComparison.Ordinal);

                if (idx > 0)
                {
                    version = version.Substring(0, idx);
                }

                return Version.Parse(version);
            }
        }
    }
}
