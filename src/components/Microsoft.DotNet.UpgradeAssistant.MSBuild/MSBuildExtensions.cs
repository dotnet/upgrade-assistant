// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Construction;

using MBuild = Microsoft.Build.Evaluation;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    internal static class MSBuildExtensions
    {
        public static NuGetReference AsNuGetReference(this ProjectItemElement item)
        {
            var packageName = item.Include;
            var packageVersion = (item.Children.FirstOrDefault(c => c.ElementName.Equals(MSBuildConstants.VersionElementName, StringComparison.OrdinalIgnoreCase)) as ProjectMetadataElement)?.Value
                ?? "0.0.0"; // Package references without versions will resolve to the lowest stable version

            return new NuGetReference(packageName, packageVersion);
        }

        public static NuGetReference AsNuGetReference(this MBuild.ProjectItem item)
        {
            var packageName = item.EvaluatedInclude;
            var packageVersion = item.GetMetadataValue(MSBuildConstants.VersionElementName) ?? "0.0.0"; // Package references without versions will resolve to the lowest stable version

            return new NuGetReference(packageName, packageVersion);
        }

        public static string? GetHintPath(this ProjectItemElement item)
            => item.GetMetadata("HintPath");

        private static string? GetMetadata(this ProjectItemElement item, string name)
        {
            foreach (var metadata in item.Metadata)
            {
                if (string.Equals(metadata.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return metadata.Value;
                }
            }

            return null;
        }

        public static Reference AsReference(this ProjectItemElement item)
        {
            var name = item.Include;
            var comma = name.IndexOf(',', StringComparison.Ordinal);

            if (comma > 0)
            {
                name = name.Substring(0, comma);
            }

            return new(name)
            {
                HintPath = item.GetHintPath(),
            };
        }

        public static void RemovePackage(this ProjectRootElement projectRoot, NuGetReference package)
        {
            var element = projectRoot.GetAllPackageReferences()
                .FirstOrDefault(p => package.Equals(p.AsNuGetReference()));

            element?.RemoveElement();
        }

        public static void AddFrameworkReference(this ProjectRootElement projectRoot, ProjectItemGroupElement itemGroup, Reference frameworkReference) =>
            itemGroup.AppendChild(projectRoot.CreateItemElement(MSBuildConstants.FrameworkReferenceType, frameworkReference.Name));

        public static void AddPackageReference(this ProjectRootElement projectRoot, ProjectItemGroupElement itemGroup, NuGetReference packageReference)
        {
            var newItemElement = projectRoot.CreateItemElement(MSBuildConstants.PackageReferenceType, packageReference.Name);
            itemGroup.AppendChild(newItemElement);
            newItemElement.AddMetadata(MSBuildConstants.VersionElementName, packageReference.Version, true);

            if (packageReference.PrivateAssets is not null)
            {
                var privateAssetsElement = projectRoot.CreateMetadataElement("PrivateAssets", packageReference.PrivateAssets);
                newItemElement.AppendChild(privateAssetsElement);
            }
        }

        public static void RemoveReference(this ProjectRootElement projectRoot, Reference reference)
        {
            var element = projectRoot.GetAllReferences()
                .FirstOrDefault(r => reference.Equals(r.AsReference()));

            element?.RemoveElement();
        }

        public static void RemoveFrameworkReference(this ProjectRootElement projectRoot, Reference frameworkReference)
        {
            var element = projectRoot.GetAllFrameworkReferences()
                .FirstOrDefault(r => frameworkReference.Equals(r.AsReference()));

            element?.RemoveElement();
        }

        public static IEnumerable<ProjectItemElement> GetAllReferences(this ProjectRootElement projectRoot)
            => projectRoot.Items.Where(i => i.ItemType.Equals(MSBuildConstants.ReferenceType, StringComparison.OrdinalIgnoreCase));

        public static IEnumerable<ProjectItemElement> GetAllPackageReferences(this ProjectRootElement projectRoot)
            => projectRoot.Items.Where(i => i.ItemType.Equals(MSBuildConstants.PackageReferenceType, StringComparison.OrdinalIgnoreCase));

        public static IEnumerable<ProjectItemElement> GetAllFrameworkReferences(this ProjectRootElement projectRoot)
            => projectRoot.Items.Where(i => i.ItemType.Equals(MSBuildConstants.FrameworkReferenceType, StringComparison.OrdinalIgnoreCase));

        public static void RemoveElement(this ProjectElement element)
        {
            var itemGroup = element.Parent;
            itemGroup.RemoveChild(element);

            if (!itemGroup.Children.Any())
            {
                // If no element remain in the item group, remove it
                itemGroup.Parent.RemoveChild(itemGroup);
            }
        }

        public static void RemoveProjectProperty(this ProjectRootElement projectRoot, ProjectPropertyElement element)
        {
            var itemGroup = element.Parent;
            itemGroup.RemoveChild(element);

            if (!itemGroup.Children.Any())
            {
                // If no element remain in the item group, remove it
                itemGroup.Parent.RemoveChild(itemGroup);
            }
        }

        public static ProjectItemGroupElement GetOrCreateItemGroup(this ProjectRootElement projectRoot, string itemType)
        {
            var itemGroup = projectRoot.ItemGroups.FirstOrDefault(g => g.Items.Any(i => i.ItemType.Equals(itemType, StringComparison.OrdinalIgnoreCase)));
            if (itemGroup is null)
            {
                itemGroup = projectRoot.CreateItemGroupElement();
                projectRoot.AppendChild(itemGroup);
            }

            return itemGroup;
        }
    }
}
