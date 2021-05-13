﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public class PackageUpdaterPreTFMStep : PackageUpdaterStep
    {
        public override string Description => "Update package references and remove transitive dependencies";

        public override string Title => "Clean up NuGet package references";

        public override string Id => WellKnownStepIds.PackageUpdaterPreTFMStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be SDK-style before changing package references
            WellKnownStepIds.TryConvertProjectConverterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId,

            WellKnownStepIds.NextProjectStepId,
        };

        public PackageUpdaterPreTFMStep(
            IPackageRestorer packageRestorer,
            IEnumerable<IDependencyAnalyzer> packageAnalyzers,
            IDependencyAnalyzerRunner dependencyAnalyzerRunner,
            ILogger<PackageUpdaterPreTFMStep> logger)
            : base(packageRestorer, packageAnalyzers, dependencyAnalyzerRunner, logger)
        {
        }
    }
}
