using System;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UpgradeAssistant.MSBuild;

namespace Microsoft.UpgradeAssistant
{
    public static class MigratorMsBuildExtensions
    {
        private static readonly string[] ItemTypesToDeduplicate = new[] { "Compile", "Content" };

        public static void AddMsBuild(this IServiceCollection services)
        {
            services.AddSingleton<IVisualStudioFinder, VisualStudioFinder>();
            services.AddTransient<IPackageRestorer, MSBuildPackageRestorer>();
            services.AddTransient<MSBuildRegistrationStartup>();
            services.AddSingleton<IMigrationContextFactory, MSBuildMigrationContextFactory>();

            // Instantiate the migration context with a func to avoid needing MSBuild types prior to MSBuild registration
            services.AddTransient<MSBuildWorkspaceMigrationContext>();
            services.AddTransient<IMigrationContext>(sp => sp.GetRequiredService<MSBuildWorkspaceMigrationContext>());
            services.AddTransient<Func<MSBuildWorkspaceMigrationContext>>(sp => () => sp.GetRequiredService<MSBuildWorkspaceMigrationContext>());
            services.AddTransient<ITargetTFMSelector, MSBuildTargetTFMSelector>();
            services.AddTransient<ITargetFrameworkMonikerFactory, NuGetTargetFrameworkMonikerFactory>();
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
                if (!i.EvaluatedInclude.Equals(i.UnevaluatedInclude))
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
                    foreach (var item in rootElement.Items.Where(i => i.Include.Equals(projectItem.EvaluatedInclude)))
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
