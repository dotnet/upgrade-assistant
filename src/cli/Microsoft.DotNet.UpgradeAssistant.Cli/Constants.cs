// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class Constants
    {
        public static string Version
        {
            get
            {
                var attribute = typeof(Constants).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return attribute?.InformationalVersion ?? "0.0.0-unspecified";
            }
        }
    }
}
