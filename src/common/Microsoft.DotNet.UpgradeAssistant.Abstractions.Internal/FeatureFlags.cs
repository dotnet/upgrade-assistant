// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// A system to track experimental features. Please keep in sync with the README section 'Experimental features'.
    /// </summary>
    public static class FeatureFlags
    {
        private const string AnalyzeBinaries = "ANALYZE_BINARIES";
        private const string SolutionWideSdkConversion = "SOLUTION_WIDE_SDK_CONVERSION";
        private const string EnableCrossPlatform = "ENABLE_CROSS_PLATFORM";

        public static readonly IReadOnlyCollection<string> RegisteredFeatures = new[]
        {
            SolutionWideSdkConversion,
            EnableCrossPlatform,
            AnalyzeBinaries
        };

        private static readonly ICollection<string> _features = CreateFeatures();

        private static ICollection<string> CreateFeatures()
        {
            var features = Environment.GetEnvironmentVariable("UA_FEATURES");

            if (string.IsNullOrWhiteSpace(features))
            {
                return Array.Empty<string>();
            }

            return new HashSet<string>(features.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsRegistered(string name) => _features.Contains(name);

        public static bool IsWindowsCheckEnabled => !_features.Contains(EnableCrossPlatform);

        public static bool IsAnalyzeBinariesEnabled => _features.Contains(AnalyzeBinaries);

        public static bool IsSolutionWideSdkConversionEnabled => _features.Contains(SolutionWideSdkConversion);
    }
}
