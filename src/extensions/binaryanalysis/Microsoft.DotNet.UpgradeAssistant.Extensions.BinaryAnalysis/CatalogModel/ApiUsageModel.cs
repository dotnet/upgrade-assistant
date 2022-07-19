// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct ApiUsageModel : IEquatable<ApiUsageModel>
{
    private readonly int _offset;

    internal ApiUsageModel(ApiModel api, int offset)
    {
        Api = api;
        _offset = offset;
    }

    public ApiCatalogModel Catalog => Api.Catalog;

    public ApiModel Api { get; }

    public UsageSourceModel Source
    {
        get
        {
            var usageSourceOffset = Api.Catalog.ApiTable.ReadInt32(_offset);
            return new UsageSourceModel(Api.Catalog, usageSourceOffset);
        }
    }

    public float Percentage
    {
        get
        {
            var offset = _offset + 4;
            return Api.Catalog.ApiTable.ReadSingle(offset);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ApiUsageModel model && Equals(model);
    }

    public bool Equals(ApiUsageModel other)
    {
        return Api == other.Api &&
               _offset == other._offset;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Api, _offset);
    }

    public static bool operator ==(ApiUsageModel left, ApiUsageModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ApiUsageModel left, ApiUsageModel right)
    {
        return !(left == right);
    }
}
