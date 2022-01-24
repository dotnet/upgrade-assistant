// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public class TransitiveClosureCollection
    {
        public static TransitiveClosureCollection Empty { get; } = new(Enumerable.Empty<NuGetReference>().ToLookup(static t => t));

        private readonly ILookup<NuGetReference, NuGetReference> _dependencies;

        public TransitiveClosureCollection(ILookup<NuGetReference, NuGetReference> dependencies)
        {
            _dependencies = dependencies;
        }

        public IEnumerable<NuGetReference> GetDependencies(NuGetReference package)
            => _dependencies[package];

        /// <summary>
        /// Gets a collection of all transitive references that were found.
        /// </summary>
        public IEnumerable<NuGetReference> References => _dependencies.Select(d => d.Key);
    }
}
