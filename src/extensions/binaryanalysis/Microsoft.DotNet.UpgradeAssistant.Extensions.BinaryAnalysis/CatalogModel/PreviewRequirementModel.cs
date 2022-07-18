// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public record PreviewRequirementModel
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal PreviewRequirementModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public string Message
    {
        get
        {
            var stringOffset = _catalog.PreviewRequirementTable.ReadInt32(_offset + 8);
            return _catalog.GetString(stringOffset);
        }
    }

    public Uri Url
    {
        get
        {
            var stringOffset = _catalog.PreviewRequirementTable.ReadInt32(_offset + 12);
            return new(_catalog.GetString(stringOffset));
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }
}
