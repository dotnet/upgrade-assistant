// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
            if (!AppliesToLanguage(type, project))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks to see if the type should be filtered out of the user's view.
        /// </summary>
        /// <param name="type">a type that represents an upgrade-assistant feature.</param>
        /// <param name="project">the project currently being upgraded.</param>
        /// <returns>true if the object is described as matching the project's language. Objects not defning the language they support will be true as default.</returns>
        private static bool AppliesToLanguage(Type type, IProject project)
        {
            // check to see if this is a code fixer
            if (DoesCodeFixerFilterThisFeature(type, project))
            {
                return false;
            }

            // check to see if this is a configuration step or other feature using the ApplicableLanguageAttribute
            if (DoesApplicableLanguageFilterThisFeature(type, project))
            {
                return false;
            }

            // if the attribute is not applied then this type defaults to opt into language support
            return true;
        }

        /// <summary>
        /// Checks to see if the type is a codefixer that should be filtered out of the user's view.
        /// </summary>
        /// <param name="type">a type that represents an upgrade-assistant feature decorated with <see cref="ExportCodeFixProviderAttribute"/>.</param>
        /// <param name="project">the project currently being upgraded.</param>
        /// <returns>true if the codefixer should be hidden from the user. Will return false for non-codefixer objects.</returns>
        private static bool DoesCodeFixerFilterThisFeature(Type type, IProject project)
        {
            var analyzerAttr = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(typeof(ExportCodeFixProviderAttribute).FullName, StringComparison.Ordinal));
            if (analyzerAttr is not null)
            {
                var applicableLanguage = analyzerAttr.ConstructorArguments.First().Value as string;
                if (applicableLanguage is not null && !string.IsNullOrWhiteSpace(applicableLanguage))
                {
                    if (project.Language == Language.CSharp
                        && !applicableLanguage.Equals(CodeAnalysis.LanguageNames.CSharp, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    if (project.Language == Language.VisualBasic
                        && !applicableLanguage.Equals(CodeAnalysis.LanguageNames.VisualBasic, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    if (project.Language == Language.FSharp
                        && !applicableLanguage.Equals(CodeAnalysis.LanguageNames.FSharp, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks to see if the type has an ApplicableLanguageAttribute that should filter the feature out of the user's view.
        /// </summary>
        /// <param name="type">a type that represents an upgrade-assistant feature decorated with <see cref="ApplicableLanguageAttribute"/>.</param>
        /// <param name="project">the project currently being upgraded.</param>
        /// <returns>true if the object should be hidden from the user. Will return false for objects not defning the language they support.</returns>
        private static bool DoesApplicableLanguageFilterThisFeature(Type type, IProject project)
        {
            var applicableLangAttr = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName.Equals(typeof(ApplicableLanguageAttribute).FullName, StringComparison.Ordinal));
            if (applicableLangAttr is not null)
            {
                var applicableLangInt = applicableLangAttr.ConstructorArguments.First().Value as int?;
                if (applicableLangInt.HasValue)
                {
                    var applicableLanguage = (Language)applicableLangInt.Value;
                    if (project.Language == Language.CSharp && applicableLanguage != Language.CSharp)
                    {
                        return true;
                    }

                    if (project.Language == Language.VisualBasic && applicableLanguage != Language.VisualBasic)
                    {
                        return true;
                    }

                    if (project.Language == Language.FSharp && applicableLanguage != Language.FSharp)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
