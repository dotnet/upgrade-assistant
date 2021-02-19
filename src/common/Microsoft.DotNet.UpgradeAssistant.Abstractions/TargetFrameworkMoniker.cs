// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record TargetFrameworkMoniker(string Name)
    {
        private const string NetStandardNamePrefix = "netstandard";
        private const string NetPrefix = "net";

        public override string ToString() => Name;

        public bool IsFramework => !IsNetStandard && !IsNetCore;

        public bool IsNetStandard => Name.StartsWith(NetStandardNamePrefix, StringComparison.OrdinalIgnoreCase);

        public bool IsNetCore => Name.StartsWith(NetPrefix, StringComparison.OrdinalIgnoreCase) && Name.Contains('.');

        public bool IsWindows => Name.Contains("windows", StringComparison.OrdinalIgnoreCase);
    }
}
