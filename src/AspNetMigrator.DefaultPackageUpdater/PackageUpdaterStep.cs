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
    public class PackageUpdaterStep : MigrationStep
    {
        const string DefaultPackageConfigFileName = "PackageMap.json";
        const string PackageReferenceType = "PackageReference";
        const string AnalyzerPackageName = "AspNetMigrator.Analyzers";
        const string AnalyzerPackageVersion = "1.0.0";
        const string VersionElementName = "Version";

        public string PackageMapPath { get; }

        private IEnumerable<NuGetPackageMap> _packageMaps;

        public PackageUpdaterStep(MigrateOptions options, PackageUpdaterOptions updaterOptions, ILogger logger) : base(options, logger)
        {
            var mapPath = updaterOptions?.PackageMapPath ?? DefaultPackageConfigFileName;

            PackageMapPath = Path.IsPathFullyQualified(mapPath) ?
                mapPath :
                Path.Combine(Path.GetDirectoryName(typeof(PackageUpdaterStep).Assembly.Location), mapPath);

            Title = $"Update NuGet packages";
            Description = $"Update package references in {options.ProjectPath} to work with .NET based on mappings in {PackageMapPath}";
        }

        protected override Task<(MigrationStepStatus Status, string StatusDetails)> ApplyImplAsync()
        {
            if (!File.Exists(PackageMapPath))
            {
                throw new FileNotFoundException("Package map file not found", PackageMapPath);
            }

            try
            {
                var project = ProjectRootElement.Open(Options.ProjectPath);
                project.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

                var referencesToAdd = new List<NuGetReference>();

                // Check for NuGet packages that need replaced and remove them
                foreach (var reference in project.Items.Where(i => i.ItemType.Equals(PackageReferenceType, StringComparison.OrdinalIgnoreCase)))
                {
                    var packageName = reference.Include;
                    var packageVersion = (reference.Children.FirstOrDefault(c => c.ElementName.Equals(VersionElementName, StringComparison.OrdinalIgnoreCase)) as ProjectMetadataElement)?.Value;
                    var map = _packageMaps.FirstOrDefault(m => m.ContainsReference(packageName, packageVersion));

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

                return Task.FromResult((MigrationStepStatus.Complete, "Packages updated"));
            }
            catch (InvalidProjectFileException)
            {
                Logger.Fatal("Invalid project: {ProjectPath}", Options.ProjectPath);
                return Task.FromResult((MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}"));
            }
        }

        protected override async Task<(MigrationStepStatus Status, string StatusDetails)> InitializeImplAsync()
        {
            if (!File.Exists(PackageMapPath))
            {
                throw new FileNotFoundException("Package map file not found", PackageMapPath);
            }

            // Initialize package maps from config file
            Logger.Information("Loading package maps from {PackageMapPath}", PackageMapPath);
            using var config = File.OpenRead(PackageMapPath);
            _packageMaps = await JsonSerializer.DeserializeAsync<IEnumerable<NuGetPackageMap>>(config).ConfigureAwait(false);
            Logger.Verbose("Loaded {MapCount} package maps", _packageMaps.Count());

            if (!File.Exists(Options.ProjectPath))
            {
                Logger.Fatal("Project file {ProjectPath} not found", Options.ProjectPath);
                return (MigrationStepStatus.Failed, $"Project file {Options.ProjectPath} not found");
            }

            try
            {
                var project = ProjectRootElement.Open(Options.ProjectPath);
                project.Reload(false); // Reload to make sure we're not seeing an old cached version of the project

                // Query for the project's package references
                var packageReferences = project.Items
                    .Where(i => i.ItemType.Equals(PackageReferenceType, StringComparison.OrdinalIgnoreCase)) // All <PackageReferenceElements>
                    .Select(p => (Name: p.Include, Version: (p.Children.FirstOrDefault(c => c.ElementName.Equals(VersionElementName, StringComparison.OrdinalIgnoreCase)) as ProjectMetadataElement)?.Value)); // Select name/version
                
                // Identify any references that need updated
                var outdatedPackages = packageReferences.Where(p => _packageMaps.Any(m => m.ContainsReference(p.Name, p.Version)));

                if (outdatedPackages.Any())
                {
                    Logger.Information("Found {PackageCount} outdated package references", outdatedPackages.Count());
                    return (MigrationStepStatus.Incomplete, $"{outdatedPackages.Count()} packages need updated");
                }

                if (!packageReferences.Any(p => p.Name.Equals(AnalyzerPackageName)))
                {
                    Logger.Information("Reference to package {AnalyzerPackageName} needs added", AnalyzerPackageName);
                    return (MigrationStepStatus.Incomplete, $"Reference to package {AnalyzerPackageName} needed");
                }

                Logger.Information("No package updates needed");
                return (MigrationStepStatus.Complete, "No package updates needed");
            }
            catch (InvalidProjectFileException)
            {
                Logger.Fatal("Invalid project: {ProjectPath}", Options.ProjectPath);
                return (MigrationStepStatus.Failed, $"Invalid project: {Options.ProjectPath}");
            }
        }
    }
}
