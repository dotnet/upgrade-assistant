// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct ObsoletionModel : IEquatable<ObsoletionModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _offset;

    internal ObsoletionModel(ApiCatalogModel catalog, int offset)
    {
        _catalog = catalog;
        _offset = offset;
    }

    public string Message
    {
        get
        {
            var stringOffset = _catalog.ObsoletionTable.ReadInt32(_offset + 8);
            return _catalog.GetString(stringOffset);
        }
    }

    public bool IsError
    {
        get
        {
            return _catalog.ObsoletionTable.ReadByte(_offset + 12) == 1;
        }
    }

    public string DiagnosticId
    {
        get
        {
            var stringOffset = _catalog.ObsoletionTable.ReadInt32(_offset + 13);
            return _catalog.GetString(stringOffset);
        }
    }

#pragma warning disable CA1056 // URI-like properties should not be strings
    public string UrlFormat
#pragma warning restore CA1056 // URI-like properties should not be strings
    {
        get
        {
            var stringOffset = _catalog.ObsoletionTable.ReadInt32(_offset + 17);
            return _catalog.GetString(stringOffset);
        }
    }

    public Uri Url
    {
        get
        {
            return new(UrlFormat.Length > 0 && DiagnosticId.Length > 0
                        ? string.Format(CultureInfo.InvariantCulture, UrlFormat, DiagnosticId)
                        : UrlFormat);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ObsoletionModel model && Equals(model);
    }

    public bool Equals(ObsoletionModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _offset);
    }

    public static bool operator ==(ObsoletionModel left, ObsoletionModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ObsoletionModel left, ObsoletionModel right)
    {
        return !(left == right);
    }
}
