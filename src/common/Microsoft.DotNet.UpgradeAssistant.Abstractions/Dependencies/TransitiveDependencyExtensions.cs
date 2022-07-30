// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Dependencies
{
    public static class TransitiveDependencyExtensions
    {
        public static Task<bool> IsTransitiveDependencyAsync(this ITransitiveDependencyIdentifier identifier, NuGetReference package, IProject project, CancellationToken token)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return identifier.IsTransitiveDependencyAsync(package, project.GetProjectTranstiveDependencies(), project.TargetFrameworks, token);
        }

        public static async Task<bool> IsTransitiveDependencyAsync(this ITransitiveDependencyIdentifier identifier, NuGetReference package, IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfms, CancellationToken token)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (packages is null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            if (tfms is null)
            {
                throw new ArgumentNullException(nameof(tfms));
            }

            var result = await identifier.GetTransitiveDependenciesAsync(packages, tfms, token).ConfigureAwait(false);

            foreach (var item in result.References)
            {
                if (item.Equals(package))
                {
                    return true;
                }
            }

            return false;
        }

        public static Task<bool> IsTransitiveDependencyAsync(this ITransitiveDependencyIdentifier identifier, string packageName, IProject project, CancellationToken token)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentException($"'{nameof(packageName)}' cannot be null or empty.", nameof(packageName));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return identifier.IsTransitiveDependencyAsync(packageName, project.GetProjectTranstiveDependencies(), project.TargetFrameworks, token);
        }

        public static async Task<bool> IsTransitiveDependencyAsync(this ITransitiveDependencyIdentifier identifier, string packageName, IEnumerable<NuGetReference> packages, IEnumerable<TargetFrameworkMoniker> tfms, CancellationToken token)
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentException($"'{nameof(packageName)}' cannot be null or empty.", nameof(packageName));
            }

            if (packages is null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            if (tfms is null)
            {
                throw new ArgumentNullException(nameof(tfms));
            }

            var result = await identifier.GetTransitiveDependenciesAsync(packages, tfms, token).ConfigureAwait(false);
            return result.References.Any(item => string.Equals(item.Name, packageName, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<NuGetReference> GetProjectTranstiveDependencies(this IProject project)
        {
            var set = new HashSet<NuGetReference>();
            var visited = new HashSet<IProject>();

            var projects = new Queue<IProject>();
            projects.Enqueue(project);

            do
            {
                var current = projects.Dequeue();
                set.UnionWith(current.PackageReferences);

                foreach (var child in current.ProjectReferences)
                {
                    if (visited.Add(child))
                    {
                        projects.Enqueue(child);
                    }
                }
            }
            while (projects.Count > 0);

            return set;
        }
    }
}
