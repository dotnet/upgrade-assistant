// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct UsageSourceModel : IEquatable<UsageSourceModel>
{
    private readonly int _offset;

    internal UsageSourceModel(ApiCatalogModel catalog, int offset)
    {
        Catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog { get; }

    public string Name
    {
        get
        {
            var stringOffset = Catalog.UsageSourcesTable.ReadInt32(_offset);
            return Catalog.GetString(stringOffset);
        }
    }

    public DateOnly Date
    {
        get
        {
            var dayNumber = Catalog.UsageSourcesTable.ReadInt32(_offset + 4);
            return DateOnly.FromDayNumber(dayNumber);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is AssemblyModel model && Equals(model);
    }

    public bool Equals(UsageSourceModel other)
    {
        return ReferenceEquals(Catalog, other.Catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Catalog, _offset);
    }

    public static bool operator ==(UsageSourceModel left, UsageSourceModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UsageSourceModel left, UsageSourceModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Name} ({Date})";
    }
}
