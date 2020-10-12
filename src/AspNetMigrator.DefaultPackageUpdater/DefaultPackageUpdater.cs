using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Exceptions;

namespace AspNetMigrator.Engine
{
    public class DefaultPackageUpdater : IPackageUpdater
    {
        const string PackageConfigFileName = "PackageMap.json";
        const string PackageReferenceType = "PackageReference";
        const string AnalyzerPackageName = "AspNetMigrator.Analyzers";
        const string AnalyzerPackageVersion = "1.0.0";
        const string VersionElementName = "Version";

        private ILogger Logger { get; }

        private IEnumerable<NuGetPackageMap> _packageMaps;

        public DefaultPackageUpdater(ILogger logger)
        {
            Logger = logger;
        }

        public async Task<bool> UpdatePackagesAsync(string projectFilePath)
        {
            if (!File.Exists(projectFilePath))
            {
                Logger.Fatal("Project file {ProjectPath} not found", projectFilePath);
                return false;
            }

            try
            {
                var project = ProjectRootElement.Open(projectFilePath);

                // Replace known NuGet packages with updated ones
                var maps = await GetPackageMapsAsync();
                Logger.Verbose("Loaded {PackageMapCount} package maps from configuration", maps.Count());
                var referencesToAdd = new List<NuGetReference>();

                // Check for NuGet packages that need replaced and remove them
                foreach (var reference in project.Items.Where(i => i.ItemType.Equals(PackageReferenceType, StringComparison.OrdinalIgnoreCase)))
                {
                    var packageName = reference.Include;
                    var packageVersion = (reference.Children.FirstOrDefault(c => c.ElementName.Equals(VersionElementName, StringComparison.OrdinalIgnoreCase)) as ProjectMetadataElement)?.Value;
                    var map = maps.FirstOrDefault(m => m.ContainsReference(packageName, packageVersion));

                    if (map != null)
                    {
                        Logger.Information("Removing outdated packaged reference (based on package map {PackageMapName}): {PackageReference}", map.PackageSetName, new NuGetReference(packageName, packageVersion));

                        // The reference should be replaced
                        var itemGroup = reference.Parent;
                        itemGroup.RemoveChild(reference);

                        if (!itemGroup.Children.Any())
                        {
                            // If no element remain in the item group, remove it
                            Logger.Verbose("Removing empty ItemGroup");
                            itemGroup.Parent.RemoveChild(itemGroup);
                        }

                        // Include the updated version of removed packages in the list of packages to add references to
                        referencesToAdd.AddRange(map.NetCorePackages);
                    }
                }

                // Determine where new package references should go
                var packageReferenceItemGroup = project.ItemGroups.FirstOrDefault(g => g.Items.Any(i => i.ItemType.Equals(PackageReferenceType, StringComparison.OrdinalIgnoreCase)));
                if (packageReferenceItemGroup is null)
                {
                    Logger.Verbose("Creating a new ItemGroup for package references");
                    packageReferenceItemGroup = project.CreateItemGroupElement();
                    project.AppendChild(packageReferenceItemGroup);
                }
                else
                {
                    Logger.Verbose("Found ItemGroup for package references");
                }

                // Add replacement packages
                foreach (var newReference in referencesToAdd.Distinct())
                {
                    Logger.Information("Adding package reference to: {PackageReference}", newReference);
                    var newItemElement = project.CreateItemElement(PackageReferenceType, newReference.Name);
                    packageReferenceItemGroup.AppendChild(newItemElement);
                    newItemElement.AddMetadata(VersionElementName, newReference.Version, true);
                }

                // Add reference to ASP.NET Core migration analyzers
                if (!project.Items.Any(i => i.ItemType.Equals(PackageReferenceType, StringComparison.OrdinalIgnoreCase) && AnalyzerPackageName.Equals(i.Include, StringComparison.OrdinalIgnoreCase)))
                {
                    Logger.Information("Adding package reference to: {PackageReference}", AnalyzerPackageName);
                    var analyzerReferenceElement = project.CreateItemElement(PackageReferenceType, AnalyzerPackageName);
                    packageReferenceItemGroup.AppendChild(analyzerReferenceElement);
                    analyzerReferenceElement.AddMetadata(VersionElementName, AnalyzerPackageVersion, true);
                }
                else
                {
                    Logger.Verbose("Analyzer reference already present");
                }

                project.Save();
            }
            catch (InvalidProjectFileException)
            {
                Logger.Fatal("Invalid project: {ProjectPath}", projectFilePath);
                return false;
            }

            return true;
        }

        private async Task<IEnumerable<NuGetPackageMap>> GetPackageMapsAsync()
        {
            if (_packageMaps is null)
            {
                var configFilePath = Path.Combine(Path.GetDirectoryName(typeof(DefaultPackageUpdater).Assembly.Location), PackageConfigFileName);
                Logger.Verbose("Loading package maps from {PackageMapFilePath}", configFilePath);
                using var config = File.OpenRead(configFilePath);
                _packageMaps = await JsonSerializer.DeserializeAsync<IEnumerable<NuGetPackageMap>>(config);
            }

            return _packageMaps;
        }
    }
}
