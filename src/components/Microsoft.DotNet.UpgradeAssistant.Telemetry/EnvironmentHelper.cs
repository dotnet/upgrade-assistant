// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    internal static class EnvironmentHelper
    {
        public static bool GetEnvironmentVariableAsBool(string name, bool defaultValue = false)
        {
            var str = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            return str.ToUpperInvariant() switch
            {
                "TRUE" or "1" or "YES" => true,
                "FALSE" or "0" or "NO" => false,
                _ => defaultValue,
            };
        }
    }
}
