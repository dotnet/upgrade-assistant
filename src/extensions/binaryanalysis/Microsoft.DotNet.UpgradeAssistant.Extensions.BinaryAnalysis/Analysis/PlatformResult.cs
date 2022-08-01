// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.Analysis
{
    public record PlatformResult
    {
        public static PlatformResult Supported(string platformName) => new(platformName, true);

        public static PlatformResult Unsupported(string platformName) => new(platformName, false);

        private PlatformResult(string name, bool supported)
        {
            this.PlatformName = name;
            this.IsSupported = supported;
        }

        public string PlatformName { get; }

        public bool IsSupported { get; }

        public override string ToString()
        {
            return IsSupported ? "Supported" : "Unsupported";
        }
    }
}
