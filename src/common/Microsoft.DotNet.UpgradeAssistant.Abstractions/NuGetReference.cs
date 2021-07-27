// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record NuGetReference(string Name, string Version)
    {
        // Hyphens in semantic versioning indicate a pre-release version (https://semver.org/spec/v2.0.0.html#spec-item-9)
        public bool IsPrerelease => Version.Contains("-");

        public bool HasWildcardVersion => Version.EndsWith("*", StringComparison.OrdinalIgnoreCase);

        public override string ToString()
        {
            return $"{Name}, Version={Version}";
        }

        public string? PrivateAssets { get; set; }

        public IEnumerable<string>? ActionDetails { get; set; }
    }
}
