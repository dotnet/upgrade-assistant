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
        private const string Output_Format = "ANALYZE_OUTPUT_FORMAT";
        private const string SolutionWideSdkConversion = "SOLUTION_WIDE_SDK_CONVERSION";

        public static readonly ICollection<string> RegisteredFeatures = new[]
        {
            Output_Format,
            SolutionWideSdkConversion
        };

        private static readonly ICollection<string> _features = CreateFeatures();

        private static ICollection<string> CreateFeatures()
        {
            var features = Environment.GetEnvironmentVariable("UA_FEATURES");

            if (features is null)
            {
                return Array.Empty<string>();
            }

            return new HashSet<string>(features.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsRegistered(string name) => _features.Contains(name);

        public static bool IsAnalyzeFormatEnabled => _features.Contains(Output_Format);

        public static bool IsSolutionWideSdkConversionEnabled => _features.Contains(SolutionWideSdkConversion);
    }
}
