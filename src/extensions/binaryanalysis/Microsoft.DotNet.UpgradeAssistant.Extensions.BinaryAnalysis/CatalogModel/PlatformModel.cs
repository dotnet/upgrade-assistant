// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct PlatformModel : IEquatable<PlatformModel>
{
    private readonly int _offset;

    internal PlatformModel(ApiCatalogModel catalog, int offset)
    {
        Catalog = catalog;
        _offset = offset;
    }

    public ApiCatalogModel Catalog { get; }

    public string Name
    {
        get
        {
            var stringOffset = Catalog.PlatformTable.ReadInt32(_offset);
            return Catalog.GetString(stringOffset);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is PlatformModel model && Equals(model);
    }

    public bool Equals(PlatformModel other)
    {
        return ReferenceEquals(Catalog, other.Catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Catalog, _offset);
    }

    public static bool operator ==(PlatformModel left, PlatformModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformModel left, PlatformModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }
}
