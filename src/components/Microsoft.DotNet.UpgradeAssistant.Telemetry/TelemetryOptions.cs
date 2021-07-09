// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public class TelemetryOptions
    {
        public bool IsEnabled { get; set; }

        public string ProductVersion { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string InstrumentationKey { get; set; } = string.Empty;

        public string DetailsLink { get; set; } = string.Empty;

        public string CurrentSessionId { get; set; } = string.Empty;

        internal string TelemetryOptout => $"DOTNET_{ToolName.ToUpperInvariant()}_TELEMTRY_OPTOUT";

        internal string ProducerNamespace => $"dotnet/{ToolName}";

        internal string SentinelSuffix => $"dotnet{ToolName}FirstUseSentinel";

        internal string UserLevelCache => $"dotnet{ToolName}UserLevelCache";

        internal string SkipFirstTime => $"DOTNET_{ToolName.ToUpperInvariant()}_SKIP_FIRST_TIME_EXPERIENCE";

        private string ToolName => DisplayName.Replace(" ", string.Empty);
    }
}
