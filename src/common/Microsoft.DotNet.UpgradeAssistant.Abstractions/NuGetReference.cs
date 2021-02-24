// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record NuGetReference(string Name, string Version)
    {
        public bool HasWildcardVersion => Version.Equals("*", StringComparison.OrdinalIgnoreCase);

        public override string ToString()
        {
            return $"{Name}, Version={Version}";
        }

        public string? PrivateAssets { get; set; }
    }
}
