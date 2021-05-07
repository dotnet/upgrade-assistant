// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.UpgradeAssistant.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class UpgraderMsBuildExtensions
    {
        private static readonly string[] ItemTypesToDeduplicate = new[] { "Compile", "Content" };

        public static void AddMsBuild(this IServiceCollection services)
        {
            services.AddSingleton<MSBuildPathLocator>();
            services.AddSingleton<IVisualStudioFinder, VisualStudioFinder>();
            services.AddTransient<IPackageRestorer, DotnetRestorePackageRestorer>();
            services.AddTransient<IUpgradeStartup, MSBuildRegistrationStartup>();
            services.AddSingleton<IUpgradeContextFactory, MSBuildUpgradeContextFactory>();
            services.AddSingleton<IComponentIdentifier, ComponentIdentifier>();

            // Instantiate the upgrade context with a func to avoid needing MSBuild types prior to MSBuild registration
            services.AddTransient<MSBuildWorkspaceUpgradeContext>();
            services.AddTransient<IUpgradeContext>(sp => sp.GetRequiredService<MSBuildWorkspaceUpgradeContext>());
            services.AddTransient<Func<MSBuildWorkspaceUpgradeContext>>(sp => () => sp.GetRequiredService<MSBuildWorkspaceUpgradeContext>());

            services.AddNuGet();
        }

        private static void AddNuGet(this IServiceCollection services)
        {
            services.AddSingleton<IPackageLoader, PackageLoader>();
            services.AddSingleton<IVersionComparer, NuGetVersionComparer>();
            services.AddTransient<ITargetFrameworkMonikerComparer, NuGetTargetFrameworkMonikerComparer>();
            services.AddSingleton<IUpgradeStartup, NuGetCredentialsStartup>();
            services.AddSingleton<INuGetPackageSourceFactory, NuGetPackageSourceFactory>();
            services.AddOptions<NuGetDownloaderOptions>()
                .Configure(options =>
                {
                    var settings = Settings.LoadDefaultSettings(null);
                    options.CachePath = SettingsUtility.GetGlobalPackagesFolder(settings);
                });

            // Register the extension locator via a factory because NuGet types (IExtensionLocator)
            // can't be referenced until the MSBuild locator has run.
            services.AddTransient<NuGetExtensionLocatorFactory>();
        }

        // TEMPORARY WORKAROUND
        // https://github.com/dotnet/roslyn/issues/36781
        // Adding documents to a project can result in extra "<include Compile=...>" items
        // since Roslyn always adds an explicit include regardless of whether the new file
        // would be picked up by a globbing pattern. Until that is fixed, this workaround
        // cleans up extra explicit include items that duplicate globbing patterns.
        internal static void WorkAroundRoslynIssue36781(this ProjectRootElement rootElement)
        {
            if (rootElement is null)
            {
                throw new ArgumentNullException(nameof(rootElement));
            }

            using var projectCollection = new ProjectCollection();
            var project = projectCollection.LoadProject(rootElement.FullPath);

            // Find duplicate items in the project
            var duplicateItems = project.Items.Where(i =>
            {
                // Only consider items that are added explicitly (not via a globbing pattern)
                if (!i.EvaluatedInclude.Equals(i.UnevaluatedInclude, StringComparison.Ordinal))
                {
                    return false;
                }

                // Only consider compile or content items
                if (!ItemTypesToDeduplicate.Contains(i.ItemType, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Skip items that are only included once
                if (project.Items.Count(i2 => i2.EvaluatedInclude.Equals(i.EvaluatedInclude, StringComparison.Ordinal)) <= 1)
                {
                    return false;
                }

                return true;
            });

            if (duplicateItems.Any())
            {
                foreach (var projectItem in duplicateItems)
                {
                    // For any duplcate items, find them in the project root element and remove them
                    foreach (var item in rootElement.Items.Where(i => i.Include.Equals(projectItem.EvaluatedInclude, StringComparison.Ordinal)))
                    {
                        var parent = item.Parent;
                        parent.RemoveChild(item);

                        // If that item was the only child of its parent itemGroup,
                        // remove the parent, too
                        if (!parent.Children.Any())
                        {
                            parent.Parent.RemoveChild(parent);
                        }
                    }
                }

                rootElement.Save();
            }
        }
    }
}
