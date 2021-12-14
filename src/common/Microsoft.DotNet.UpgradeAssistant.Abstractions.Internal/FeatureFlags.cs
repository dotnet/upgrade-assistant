// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class FeatureFlags
    {
        private static ICollection<string> _features = CreateFeatures();

        private static ICollection<string> CreateFeatures()
        {
            var features = Environment.GetEnvironmentVariable("UA_FEATURES");

            if (features is null)
            {
                return Array.Empty<string>();
            }

            return new HashSet<string>(features.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsRequested(string name) => _features.Contains(name);

        public static bool AnalyzeFormat() => _features.Contains("FORMAT");
    }
}
