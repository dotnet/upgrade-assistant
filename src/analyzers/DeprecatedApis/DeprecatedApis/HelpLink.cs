// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.DeprecatedApisAnalyzer
{
    internal static class HelpLink
    {
        private const string Link = "https://github.com/twsouthwick/upgrade-assistant/blob/hackathon2021/docs/analyzers/DeprecatedApisAnalyzer/DeprecatedApiAnalyzer.md";

        public static string Create(string id) => $"{Link}#{id}";
    }
}
