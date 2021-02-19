// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageUpdaterOptions
    {
        public string? MigrationAnalyzersPackageSource { get; set; }

        public string? MigrationAnalyzersPackageVersion { get; set; }

        public string? PackageMapPath { get; set; }
    }
}
