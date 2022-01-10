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

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class TransitiveDependencyCollector : ITransitiveDependencyCollector
    {
        private readonly IPackageLoader _packages;

        public TransitiveDependencyCollector(IPackageLoader packages)
        {
            _packages = packages;
        }

        public virtual async Task<IReadOnlyCollection<NuGetReference>> GetTransitiveDependenciesAsync(IEnumerable<NuGetReference> projectReferences, TargetFrameworkMoniker tfm, CancellationToken token)
        {
            if (projectReferences is null)
            {
                throw new ArgumentNullException(nameof(projectReferences));
            }

            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            var nugetTfm = NuGetFramework.Parse(tfm.ToString());
            var result = new HashSet<NuGetReference>();

            var additional = await ExpandAsync(result, projectReferences, nugetTfm, token).ConfigureAwait(false);

            while (additional.Count > 0)
            {
                additional = await ExpandAsync(result, projectReferences, nugetTfm, token).ConfigureAwait(false);
            }

            return result;
        }

        private async Task<IReadOnlyCollection<NuGetReference>> ExpandAsync(HashSet<NuGetReference> result, IEnumerable<NuGetReference> references, NuGetFramework tfm, CancellationToken token)
        {
            var metadatas = await Task.WhenAll(references.Select(r => _packages.GetPackageMetadata(r, token))).ConfigureAwait(false);

            if (metadatas is null)
            {
                return Array.Empty<NuGetReference>();
            }

            var additional = new HashSet<NuGetReference>();

            foreach (var metadata in metadatas)
            {
                if (metadata is null)
                {
                    continue;
                }

                var tfms = metadata.Dependencies.Select(d => NuGetFramework.Parse(d.ToString()));
                var match = NuGetFrameworkUtility.GetNearest(metadata.Dependencies, NuGetFramework.Parse(tfm.ToString()), e => NuGetFramework.Parse(e.Framework.ToString()));

                if (match is null)
                {
                    continue;
                }

                foreach (var r in match.Packages)
                {
                    if (result.Add(r))
                    {
                        additional.Add(r);
                    }
                }
            }

            return additional;
        }
    }
}
