﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageUpdaterPreTFMStep : PackageUpdaterStep
    {
        public override string Description => "Update package references and remove transitive dependencies";

        public override string Title => "Clean up NuGet package references";

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing package references
            "Microsoft.DotNet.UpgradeAssistant.Steps.Backup.BackupStep",

            // Project should be SDK-style before changing package references
            "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.TryConvertProjectConverterStep",
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            // Project should have correct TFM
            "Microsoft.DotNet.UpgradeAssistant.Steps.ProjectFormat.SetTFMStep",

            "Microsoft.DotNet.UpgradeAssistant.Steps.Solution.NextProjectStep",
        };

        public PackageUpdaterPreTFMStep(
            IOptions<PackageUpdaterOptions> updaterOptions,
            IPackageRestorer packageRestorer,
            IEnumerable<IPackageReferencesAnalyzer> packageAnalyzers,
            ILogger<PackageUpdaterPreTFMStep> logger)
            : base(updaterOptions, packageRestorer, packageAnalyzers, logger)
        {
        }
    }
}
