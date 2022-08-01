// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.Analysis
{
    public record FrameworkResult
    {
        public FrameworkResult(NuGetFramework framework,
            AvailabilityResult availability,
            ObsoletionResult? obsoletion,
            IReadOnlyList<PlatformResult?> platforms)
        {
            ArgumentNullException.ThrowIfNull(framework);
            ArgumentNullException.ThrowIfNull(platforms);

            FrameworkName = framework;
            Availability = availability;
            Obsoletion = obsoletion;
            Platforms = platforms;
        }

        public NuGetFramework FrameworkName { get; }

        public AvailabilityResult Availability { get; }

        public ObsoletionResult? Obsoletion { get; }

        public IReadOnlyList<PlatformResult?> Platforms { get; }

        public bool IsRelevant()
        {
            return !Availability.IsAvailable ||
                   Availability.Package is not null ||
                   Obsoletion is not null ||
                   Platforms.Any(p => p?.IsSupported == false);
        }
    }
}
