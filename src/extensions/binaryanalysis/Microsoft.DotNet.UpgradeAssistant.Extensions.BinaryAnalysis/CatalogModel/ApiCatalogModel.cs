// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public sealed partial class ApiCatalogModel
{
    public static Uri Url => new("https://apicatalogblob.blob.core.windows.net/catalog/apicatalog.dat");

    private static IReadOnlyList<byte> MagicHeader { get; } = Encoding.ASCII.GetBytes("APICATFB");

    private const int FormatVersion = 4;

    private readonly int _sizeOnDisk;
    private readonly byte[] _buffer;
    private readonly int _stringTableLength;
    private readonly int _platformTableOffset;
    private readonly int _platformTableLength;
    private readonly int _frameworkTableOffset;
    private readonly int _frameworkTableLength;
    private readonly int _packageTableOffset;
    private readonly int _packageTableLength;
    private readonly int _assemblyTableOffset;
    private readonly int _assemblyTableLength;
    private readonly int _usageSourcesTableOffset;
    private readonly int _usageSourcesTableLength;
    private readonly int _apiTableOffset;
    private readonly int _apiTableLength;
    private readonly int _obsoletionTableOffset;
    private readonly int _obsoletionTableLength;
    private readonly int _platformSupportTableOffset;
    private readonly int _platformSupportTableLength;
    private readonly int _previewRequirementTableOffset;
    private readonly int _previewRequirementTableLength;

    private Dictionary<int, int>? _forwardedApis;

    private ApiCatalogModel(int sizeOnDisk, byte[] buffer, int[] tableSizes)
    {
        Debug.Assert(tableSizes.Length == 10, $"10 tables expected in catalog, found {tableSizes.Length}");

        _stringTableLength = tableSizes[0];

        _platformTableOffset = _stringTableLength;
        _platformTableLength = tableSizes[1];

        _frameworkTableOffset = _platformTableOffset + _platformTableLength;
        _frameworkTableLength = tableSizes[2];

        _packageTableOffset = _frameworkTableOffset + _frameworkTableLength;
        _packageTableLength = tableSizes[3];

        _assemblyTableOffset = _packageTableOffset + _packageTableLength;
        _assemblyTableLength = tableSizes[4];

        _usageSourcesTableOffset = _assemblyTableOffset + _assemblyTableLength;
        _usageSourcesTableLength = tableSizes[5];

        _apiTableOffset = _usageSourcesTableOffset + _usageSourcesTableLength;
        _apiTableLength = tableSizes[6];

        _obsoletionTableOffset = _apiTableOffset + _apiTableLength;
        _obsoletionTableLength = tableSizes[7];

        _platformSupportTableOffset = _obsoletionTableOffset + _obsoletionTableLength;
        _platformSupportTableLength = tableSizes[8];

        _previewRequirementTableOffset = _platformSupportTableOffset + _platformSupportTableLength;
        _previewRequirementTableLength = tableSizes[9];

        _buffer = buffer;
        _sizeOnDisk = sizeOnDisk;
    }

    internal ReadOnlySpan<byte> StringTable => new(_buffer, 0, _stringTableLength);

    internal ReadOnlySpan<byte> PlatformTable => new(_buffer, _platformTableOffset, _platformTableLength);

    internal ReadOnlySpan<byte> FrameworkTable => new(_buffer, _frameworkTableOffset, _frameworkTableLength);

    internal ReadOnlySpan<byte> PackageTable => new(_buffer, _packageTableOffset, _packageTableLength);

    internal ReadOnlySpan<byte> AssemblyTable => new(_buffer, _assemblyTableOffset, _assemblyTableLength);

    internal ReadOnlySpan<byte> UsageSourcesTable => new(_buffer, _usageSourcesTableOffset, _usageSourcesTableLength);

    internal ReadOnlySpan<byte> ApiTable => new(_buffer, _apiTableOffset, _apiTableLength);

    internal ReadOnlySpan<byte> ObsoletionTable => new(_buffer, _obsoletionTableOffset, _obsoletionTableLength);

    internal ReadOnlySpan<byte> PlatformSupportTable => new(_buffer, _platformSupportTableOffset, _platformSupportTableLength);

    internal ReadOnlySpan<byte> PreviewRequirementTable => new(_buffer, _previewRequirementTableOffset, _previewRequirementTableLength);

    public FrameworkEnumerator Frameworks
    {
        get
        {
            return new FrameworkEnumerator(this);
        }
    }

    public PlatformEnumerator Platforms
    {
        get
        {
            return new PlatformEnumerator(this);
        }
    }

    public PackageEnumerator Packages
    {
        get
        {
            return new PackageEnumerator(this);
        }
    }

    public AssemblyEnumerator Assemblies
    {
        get
        {
            return new AssemblyEnumerator(this);
        }
    }

    public IEnumerable<UsageSourceModel> UsageSources
    {
        get
        {
            var count = UsageSourcesTable.ReadInt32(0);
            var offset = 4;

            for (var i = 0; i < count; i++, offset += 8)
            {
                yield return new UsageSourceModel(this, offset);
            }
        }
    }

    public ApiEnumerator RootApis
    {
        get
        {
            return new ApiEnumerator(this, 0);
        }
    }

    public IEnumerable<ApiModel> GetAllApis()
    {
        return RootApis.SelectMany(r => r.DescendantsAndSelf());
    }

    public ApiModel GetApiById(int id)
    {
        return new ApiModel(this, id);
    }

    internal string GetString(int offset)
    {
        var stringLength = StringTable.ReadInt32(offset);
        var stringSpan = StringTable.Slice(offset + 4, stringLength);
        return Encoding.UTF8.GetString(stringSpan);
    }

    internal CatalogMarkup GetMarkup(int offset)
    {
        var span = StringTable[offset..];
        var partsCount = BinaryPrimitives.ReadInt32LittleEndian(span);
        span = span[4..];

        var parts = new List<MarkupPart>(partsCount);

        for (var i = 0; i < partsCount; i++)
        {
            var kind = (MarkupPartKind)span[0];
            span = span[1..];
            var textOffset = BinaryPrimitives.ReadInt32LittleEndian(span);
            var text = GetString(textOffset);
            span = span[4..];

            Guid? reference;

            if (kind == MarkupPartKind.Reference)
            {
                var apiOffset = BinaryPrimitives.ReadInt32LittleEndian(span);
                if (apiOffset < 0)
                {
                    reference = null;
                }
                else
                {
                    reference = new ApiModel(this, apiOffset).UniqueId;
                }

                span = span[4..];
            }
            else
            {
                reference = null;
            }

            var part = new MarkupPart(kind, text, reference);
            parts.Add(part);
        }

        return new CatalogMarkup(parts);
    }

    private static int GetDeclarationTableOffset(ReadOnlySpan<byte> table, int rowSize, int apiId, int assemblyId)
    {
#pragma warning disable SA1405 // Debug.Assert should provide message text
        Debug.Assert((table.Length - 4) % rowSize == 0);
#pragma warning restore SA1405 // Debug.Assert should provide message text

        var low = 0;
        var high = ((table.Length - 4) / rowSize) - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var rowStart = 4 + (middle * rowSize);

            var rowApiId = table.ReadInt32(rowStart);
            var rowAssemblyId = table.ReadInt32(rowStart + 4);

            var comparison = (apiId, assemblyId).CompareTo((rowApiId, rowAssemblyId));

            if (comparison == 0)
            {
                // The declaration table is allowed to contain multiple entries for a given apiId/assemblyId
                // combination. Our binary search may hav jumped in the middle of sequence of rows with the same
                // apiId/assemblyId. Just look backwards and adjust the row offset until we find a row with different
                // values.
                var previousRowStart = rowStart - rowSize;
                while (previousRowStart > 0)
                {
                    var previousRowApiId = table.ReadInt32(previousRowStart);
                    var previousRowAssemblyId = table.ReadInt32(previousRowStart + 4);
                    var same = (previousRowApiId, previousRowAssemblyId) == (apiId, assemblyId);
                    if (!same)
                    {
                        break;
                    }

                    rowStart = previousRowStart;
                    previousRowStart -= rowSize;
                }

                return rowStart;
            }

            if (comparison < 0)
            {
                high = middle - 1;
            }
            else
            {
                low = middle + 1;
            }
        }

        return -1;
    }

    internal ObsoletionModel? GetObsoletion(int apiId, int assemblyId)
    {
        var offset = GetDeclarationTableOffset(ObsoletionTable, 21, apiId, assemblyId);
        return offset < 0 ? null : new ObsoletionModel(this, offset);
    }

    internal IEnumerable<PlatformSupportModel> GetPlatformSupport(int apiId, int assemblyId)
    {
        const int rowSize = 13;

        var offset = GetDeclarationTableOffset(PlatformSupportTable, rowSize, apiId, assemblyId);
        if (offset < 0)
        {
            yield break;
        }

        while (offset < PlatformSupportTable.Length)
        {
            var rowApiId = PlatformSupportTable.ReadInt32(offset);
            var rowAssemblyId = PlatformSupportTable.ReadInt32(offset + 4);

            if (rowApiId != apiId ||
                rowAssemblyId != assemblyId)
            {
                yield break;
            }

            yield return new PlatformSupportModel(this, offset);
            offset += rowSize;
        }
    }

    internal PreviewRequirementModel? GetPreviewRequirement(int apiId, int assemblyId)
    {
        var offset = GetDeclarationTableOffset(PreviewRequirementTable, 16, apiId, assemblyId);
        return offset < 0 ? null : new PreviewRequirementModel(this, offset);
    }

    public ApiCatalogStatistics GetStatistics()
    {
        var allApis = RootApis.SelectMany(a => a.DescendantsAndSelf());
        return new ApiCatalogStatistics(
            sizeOnDisk: _sizeOnDisk,
            sizeInMemory: _buffer.Length,
            numberOfApis: allApis.Count(),
            numberOfDeclarations: allApis.SelectMany(a => a.Declarations).Count(),
            numberOfAssemblies: Assemblies.Count(),
            numberOfFrameworks: Frameworks.Count(),
            numberOfFrameworkAssemblies: Assemblies.SelectMany(a => a.Frameworks).Count(),
            numberOfPackages: Packages.Select(p => p.Name).Distinct().Count(),
            numberOfPackageVersions: Packages.Count(),
            numberOfPackageAssemblies: Assemblies.SelectMany(a => a.Packages).Count(),
            numberOfUsageSources: UsageSources.Count());
    }

    public ApiModel? GetForwardedApi(ApiModel api)
    {
        if (_forwardedApis is null)
        {
            var forwardedApis = ComputeForwardedApis();
            Interlocked.CompareExchange(ref _forwardedApis, forwardedApis, null);
        }

        if (_forwardedApis.TryGetValue(api.Id, out var forwardedId))
        {
            return GetApiById(forwardedId);
        }

        return null;
    }

    private Dictionary<int, int> ComputeForwardedApis()
    {
        var result = new Dictionary<int, int>();
        ForwardTypeMembers(result, this, "System.Reflection.TypeInfo", "System.Type");
        ForwardTypeMembers(result, this, "System.Type", "System.Reflection.MemberInfo");
        return result;

        static void ForwardTypeMembers(Dictionary<int, int> receiver, ApiCatalogModel catalog, string fromTypeFullName, string toTypeFullName)
        {
            var toApi = catalog.GetAllApis().Single(a => a.GetFullName() == toTypeFullName);
            var fromApi = catalog.GetAllApis().Single(a => a.GetFullName() == fromTypeFullName);

            var toMemberByRelativeName = toApi.Descendants()
                                              .Select(a => (Name: a.GetFullName()[(toTypeFullName.Length + 1)..], Api: a))
                                              .ToDictionary(t => t.Name, t => t.Api);

            var fromMembers = fromApi.Descendants()
                                     .Select(a => (Name: a.GetFullName()[(fromTypeFullName.Length + 1)..], Api: a));

            foreach (var (name, fromMember) in fromMembers)
            {
                if (toMemberByRelativeName.TryGetValue(name, out var toMember))
                {
                    receiver.TryAdd(fromMember.Id, toMember.Id);
                }
            }
        }
    }

    public static async Task<ApiCatalogModel> LoadFromWebAsync()
    {
        using var client = new HttpClient();
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var stream = await client.GetStreamAsync(Url).ConfigureAwait(false);
        await using var memoryStream = new MemoryStream();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
        memoryStream.Position = 0;
        return await LoadAsync(memoryStream).ConfigureAwait(false);
    }

    public static async Task<ApiCatalogModel> LoadAsync(string path)
    {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var stream = File.OpenRead(path);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        return await LoadAsync(stream).ConfigureAwait(false);
    }

    public static async Task<ApiCatalogModel> LoadAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var start = stream.Position;

        using var reader = new BinaryReader(stream);

        var magicHeader = reader.ReadBytes(8);
        if (!magicHeader.SequenceEqual(MagicHeader))
        {
            throw new InvalidDataException();
        }

        var formatVersion = reader.ReadInt32();
        if (formatVersion != FormatVersion)
        {
            throw new InvalidDataException();
        }

        var numberOfTables = reader.ReadInt32();
        var tableSizes = new int[numberOfTables];
        for (var i = 0; i < tableSizes.Length; i++)
        {
            tableSizes[i] = reader.ReadInt32();
        }

        var bufferSize = tableSizes.Sum();

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var decompressedStream = new DeflateStream(stream, CompressionMode.Decompress);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        var buffer = new byte[bufferSize];
        var offset = 0;

        while (offset < buffer.Length)
        {
            offset += await decompressedStream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset)).ConfigureAwait(false);
        }

        var sizeOnDisk = (int)(stream.Position - start);

        return new ApiCatalogModel(sizeOnDisk, buffer, tableSizes);
    }

    public struct FrameworkEnumerator : IEnumerable<FrameworkModel>, IEnumerator<FrameworkModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _count;
        private int _index;

        public FrameworkEnumerator(ApiCatalogModel catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _index = -1;
            _count = _catalog.FrameworkTable.ReadInt32(0);
        }

        IEnumerator<FrameworkModel> IEnumerable<FrameworkModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public FrameworkEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_index >= _count - 1)
            {
                return false;
            }

            _index++;
            return true;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public FrameworkModel Current
        {
            get
            {
                var offset = _catalog.FrameworkTable.ReadInt32(4 + (_index * 4));
                return new FrameworkModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct PlatformEnumerator : IEnumerable<PlatformModel>, IEnumerator<PlatformModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _count;
        private int _index;

        public PlatformEnumerator(ApiCatalogModel catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _index = -1;
            _count = _catalog.PlatformTable.ReadInt32(0);
        }

        IEnumerator<PlatformModel> IEnumerable<PlatformModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PlatformEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_index >= _count - 1)
            {
                return false;
            }

            _index++;
            return true;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public PlatformModel Current
        {
            get
            {
                var offset = 4 + (_index * 4);
                return new PlatformModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct PackageEnumerator : IEnumerable<PackageModel>, IEnumerator<PackageModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _count;
        private int _index;

        public PackageEnumerator(ApiCatalogModel catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _count = catalog.PackageTable.ReadInt32(0);
            _index = -1;
        }

        IEnumerator<PackageModel> IEnumerable<PackageModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public PackageEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_index >= _count - 1)
            {
                return false;
            }

            _index++;
            return true;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public PackageModel Current
        {
            get
            {
                var offset = _catalog.PackageTable.ReadInt32(4 + (_index * 4));
                return new PackageModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct AssemblyEnumerator : IEnumerable<AssemblyModel>, IEnumerator<AssemblyModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _count;
        private int _index;

        public AssemblyEnumerator(ApiCatalogModel catalog)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _count = catalog.AssemblyTable.ReadInt32(0);
            _index = -1;
        }

        IEnumerator<AssemblyModel> IEnumerable<AssemblyModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public AssemblyEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_index >= _count - 1)
            {
                return false;
            }

            _index++;
            return true;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public AssemblyModel Current
        {
            get
            {
                var offset = _catalog.AssemblyTable.ReadInt32(4 + (_index * 4));
                return new AssemblyModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public struct ApiEnumerator : IEnumerable<ApiModel>, IEnumerator<ApiModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public ApiEnumerator(ApiCatalogModel catalog, int offset)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _offset = offset;
            _count = catalog.ApiTable.ReadInt32(offset);
            _index = -1;
        }

        IEnumerator<ApiModel> IEnumerable<ApiModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ApiEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_index >= _count - 1)
            {
                return false;
            }

            _index++;
            return true;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public ApiModel Current
        {
            get
            {
                var offset = _catalog.ApiTable.ReadInt32(_offset + 4 + (4 * _index));
                return new ApiModel(_catalog, offset);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }
}
