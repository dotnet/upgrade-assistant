// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class TransitiveDependencyChecker : ITransitiveDependencyChecker
    {
        private readonly ITransitiveDependencyCollector _collector;
        private readonly ILogger<TransitiveDependencyChecker> _logger;

        public TransitiveDependencyChecker(ITransitiveDependencyCollector collector, ILogger<TransitiveDependencyChecker> logger)
        {
            _collector = collector;
            _logger = logger;
        }

        public virtual async Task<bool> IsTransitiveDependencyAsync(IEnumerable<NuGetReference> packages, NuGetReference package, TargetFrameworkMoniker tfm, CancellationToken token)
        {
            if (packages is null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            var packageVersion = VersionRange.Parse(package.Version);
            var dependencies = await _collector.GetTransitiveDependenciesAsync(packages, tfm, token).ConfigureAwait(false);

            if (dependencies.Count == 0)
            {
                return false;
            }

            var versions = dependencies
                .Where(d => string.Equals(d.Name, package.Name, StringComparison.OrdinalIgnoreCase))
                .Select(d => n.Parse(d.Version));

            global::NuGet.Versioning.VersionExtensions.FindBestMatch(versions, packageVersion, static v => v);

            if (latest is null)
            {
                return false;
            }

            return latest.CompareTo(packageVersion) >= 0;
        }
    }
}
