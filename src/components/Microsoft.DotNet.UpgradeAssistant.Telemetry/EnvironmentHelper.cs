// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
