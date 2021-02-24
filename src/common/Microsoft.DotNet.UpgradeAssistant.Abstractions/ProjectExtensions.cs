// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ProjectExtensions
    {
        public static IProject Required(this IProject? project)
            => project ?? throw new InvalidOperationException("Project cannot be null");

        public static bool TryGetPackageByName(this IProject project, string packageName, [MaybeNullWhen(false)] out NuGetReference nugetReference)
        {
            var matches = project.Required().PackageReferences.Where(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase)).OrderByDescending(p => Version.Parse(p.Version));

            if (!matches.Any())
            {
                nugetReference = null;
                return false;
            }

            nugetReference = matches.First();
            return true;
        }
    }
}
