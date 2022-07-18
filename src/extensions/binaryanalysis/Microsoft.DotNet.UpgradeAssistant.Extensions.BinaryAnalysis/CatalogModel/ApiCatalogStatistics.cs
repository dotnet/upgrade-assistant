// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed class ApiCatalogStatistics
{
    public ApiCatalogStatistics(int sizeOnDisk,
                                int sizeInMemory,
                                int numberOfApis,
                                int numberOfDeclarations,
                                int numberOfAssemblies,
                                int numberOfFrameworks,
                                int numberOfFrameworkAssemblies,
                                int numberOfPackages,
                                int numberOfPackageVersions,
                                int numberOfPackageAssemblies,
                                int numberOfUsageSources)
    {
        SizeOnDisk = sizeOnDisk;
        SizeInMemory = sizeInMemory;
        NumberOfApis = numberOfApis;
        NumberOfDeclarations = numberOfDeclarations;
        NumberOfAssemblies = numberOfAssemblies;
        NumberOfFrameworks = numberOfFrameworks;
        NumberOfFrameworkAssemblies = numberOfFrameworkAssemblies;
        NumberOfPackages = numberOfPackages;
        NumberOfPackageVersions = numberOfPackageVersions;
        NumberOfPackageAssemblies = numberOfPackageAssemblies;
        NumberOfUsageSources = numberOfUsageSources;
    }

    public int SizeOnDisk { get; }

    public int SizeInMemory { get; }

    public int NumberOfApis { get; }

    public int NumberOfDeclarations { get; }

    public int NumberOfAssemblies { get; }

    public int NumberOfFrameworks { get; }

    public int NumberOfFrameworkAssemblies { get; }

    public int NumberOfPackages { get; }

    public int NumberOfPackageVersions { get; }

    public int NumberOfPackageAssemblies { get; }

    public int NumberOfUsageSources { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Size on disk         : {SizeOnDisk,12:N0} bytes");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Size in memory       : {SizeInMemory,12:N0} bytes");
        sb.AppendLine(CultureInfo.InvariantCulture, $"APIs                 : {NumberOfApis,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Declarations         : {NumberOfDeclarations,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Assemblies           : {NumberOfAssemblies,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Frameworks           : {NumberOfFrameworks,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Framework assemblies : {NumberOfFrameworkAssemblies,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Packages             : {NumberOfPackages,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Package versions     : {NumberOfPackageVersions,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Package assemblies   : {NumberOfPackageAssemblies,12:N0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Usage sources        : {NumberOfUsageSources,12:N0}");
        return sb.ToString();
    }
}
