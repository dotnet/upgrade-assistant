// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

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

        public static async ValueTask<bool> AppliesToProjectAsync(this Type type, IProject project, CancellationToken token)
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

            // Check whether the type has an [DiagnosticAnalyzer] attribute
            // If one exists, the type only applies to the project if the language matches
            if (!await DoesAnalyzerApplyToLanguageAsync(type, project).ConfigureAwait(false))
            {
                return false;
            }

            return true;
        }

        private static ValueTask<bool> DoesAnalyzerApplyToLanguageAsync(Type type, IProject project)
        {
            foreach (var analyzerType in new Type[] { typeof(DiagnosticAnalyzerAttribute), typeof(ExportCodeFixProviderAttribute) })
            {
                var analyzerAttr = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(analyzerType.FullName, StringComparison.Ordinal));
                if (analyzerAttr is not null)
                {
                    var applicableLanguage = analyzerAttr.ConstructorArguments.First().Value as string;
                    if (applicableLanguage is not null && !string.IsNullOrWhiteSpace(applicableLanguage))
                    {
                        if (project.Language == Language.CSharp
                            && !applicableLanguage.Equals(CodeAnalysis.LanguageNames.CSharp, StringComparison.Ordinal))
                        {
                            return new ValueTask<bool>(Task.FromResult(false));
                        }

                        if (project.Language == Language.VisualBasic
                            && !applicableLanguage.Equals(CodeAnalysis.LanguageNames.VisualBasic, StringComparison.Ordinal))
                        {
                            return new ValueTask<bool>(Task.FromResult(false));
                        }
                    }
                }
            }

            return new ValueTask<bool>(Task.FromResult(true));
        }
    }
}
