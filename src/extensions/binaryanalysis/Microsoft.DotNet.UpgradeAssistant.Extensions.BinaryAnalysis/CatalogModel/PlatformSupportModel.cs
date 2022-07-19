// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct PlatformSupportModel : IEquatable<PlatformSupportModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PlatformSupportModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public string PlatformName
    {
        get
        {
            var stringOffset = _catalog.PlatformSupportTable.ReadInt32(_offset + 8);
            return _catalog.GetString(stringOffset);
        }
    }

    public bool IsSupported
    {
        get
        {
            return _catalog.PlatformSupportTable.ReadByte(_offset + 12) == 1;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ApiUsageModel model && Equals(model);
    }

    public bool Equals(PlatformSupportModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(PlatformSupportModel left, PlatformSupportModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformSupportModel left, PlatformSupportModel right)
    {
        return !(left == right);
    }
}
