// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class ProjectExtensions
    {
        public static IProject Required(this IProject? project)
            => project ?? throw new InvalidOperationException("Project cannot be null");

        public static bool TryGetPackageByName(this INuGetReferences references, string packageName, [MaybeNullWhen(false)] out NuGetReference nugetReference)
        {
            if (references is null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            var matches = references.PackageReferences.Where(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase)).OrderByDescending(p => Version.Parse(p.Version));

            nugetReference = matches.FirstOrDefault();
            return nugetReference is not null;
        }

        public static async ValueTask<bool> AppliesToProject(this Type type, IProject project, CancellationToken token)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            // Check whether the type has an [ApplicableComponents] attribute.
            // If one exists, the type only applies to the project if the project has the indicated components.
            var applicableComponentsAttr = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(typeof(ApplicableComponentsAttribute).FullName, StringComparison.Ordinal));
            if (applicableComponentsAttr is not null)
            {
                var applicableComponents = applicableComponentsAttr.ConstructorArguments.Single().Value as int?;
                if (applicableComponents.HasValue)
                {
                    var projectComponents = (ProjectComponents)applicableComponents.Value;
                    var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

                    if (!components.HasFlag(projectComponents))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
