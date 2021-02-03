﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Construction;

namespace AspNetMigrator.MSBuild
{
    internal static class PackageReferenceExtensions
    {
        private const string PackageReferenceType = "PackageReference";
        private const string VersionElementName = "Version";

        public static NuGetReference AsNuGetReference(this ProjectItemElement item)
        {
            var packageName = item.Include;
            var packageVersion = (item.Children.FirstOrDefault(c => c.ElementName.Equals(VersionElementName, StringComparison.OrdinalIgnoreCase)) as ProjectMetadataElement)?.Value
                ?? "0.0.0"; // Package references without versions will resolve to the lowest stable version

            return new NuGetReference(packageName, packageVersion);
        }

        public static void RemovePackage(this ProjectRootElement projectRoot, NuGetReference package)
        {
            var element = projectRoot.GetAllPackageReferences()
                .FirstOrDefault(p => package.Equals(p.AsNuGetReference()));

            if (element is not null)
            {
                element.RemoveElement();
            }
        }

        public static void AddPackageReference(this ProjectRootElement projectRoot, ProjectItemGroupElement itemGroup, NuGetReference packageReference)
        {
            var newItemElement = projectRoot.CreateItemElement(PackageReferenceType, packageReference.Name);
            itemGroup.AppendChild(newItemElement);
            newItemElement.AddMetadata(VersionElementName, packageReference.Version, true);

            if (packageReference.PrivateAssets is not null)
            {
                var privateAssetsElement = projectRoot.CreateMetadataElement("PrivateAssets", packageReference.PrivateAssets);
                newItemElement.AppendChild(privateAssetsElement);
            }
        }

        public static IEnumerable<ProjectItemElement> GetAllPackageReferences(this ProjectRootElement projectRoot)
            => projectRoot.Items.Where(i => i.ItemType.Equals(PackageReferenceType, StringComparison.OrdinalIgnoreCase));

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
    }
}
