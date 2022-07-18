// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct AssemblyModel : IEquatable<AssemblyModel>
{
    internal AssemblyModel(ApiCatalogModel catalog, int offset)
    {
        Catalog = catalog;
        Id = offset;
    }

    public ApiCatalogModel Catalog { get; }

    public int Id { get; }

    public Guid UniqueId
    {
        get
        {
            return Catalog.AssemblyTable.ReadGuid(0);
        }
    }

    public string Name
    {
        get
        {
            var stringOffset = Catalog.AssemblyTable.ReadInt32(Id + 16);
            return Catalog.GetString(stringOffset);
        }
    }

    public string PublicKeyToken
    {
        get
        {
            var stringOffset = Catalog.AssemblyTable.ReadInt32(Id + 20);
            return Catalog.GetString(stringOffset);
        }
    }

    public string Version
    {
        get
        {
            var stringOffset = Catalog.AssemblyTable.ReadInt32(Id + 24);
            return Catalog.GetString(stringOffset);
        }
    }

    public RootApiEnumerator RootApis
    {
        get
        {
            var offset = Id + 28;
            return new RootApiEnumerator(Catalog, offset);
        }
    }

    public FrameworkEnumerator Frameworks
    {
        get
        {
            var rootApiCount = Catalog.AssemblyTable.ReadInt32(Id + 28);
            var frameworkTableOffset = Id + 32 + (rootApiCount * 4);
            return new FrameworkEnumerator(Catalog, frameworkTableOffset);
        }
    }

    public PackageEnumerator Packages
    {
        get
        {
            var rootApiCountOffset = Id + 28;
            var rootApiCount = Catalog.AssemblyTable.ReadInt32(rootApiCountOffset);
            var frameworkCountOffset = rootApiCountOffset + 4 + (rootApiCount * 4);
            var frameworkCount = Catalog.AssemblyTable.ReadInt32(frameworkCountOffset);

            var packageTableOffset = frameworkCountOffset + 4 + (frameworkCount * 4);
            return new PackageEnumerator(Catalog, packageTableOffset);
        }
    }

    public IEnumerable<PlatformSupportModel> PlatformSupport
    {
        get
        {
            return Catalog.GetPlatformSupport(-1, Id);
        }
    }

    public PreviewRequirementModel? PreviewRequirement
    {
        get
        {
            return Catalog.GetPreviewRequirement(-1, Id);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is AssemblyModel model && Equals(model);
    }

    public bool Equals(AssemblyModel other)
    {
        return ReferenceEquals(Catalog, other.Catalog) &&
               Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Catalog, Id);
    }

    public static bool operator ==(AssemblyModel left, AssemblyModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AssemblyModel left, AssemblyModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Name}, Version={Version}, PublicKeyToken={PublicKeyToken}";
    }

    public struct RootApiEnumerator : IEnumerable<ApiModel>, IEnumerator<ApiModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public RootApiEnumerator(ApiCatalogModel catalog, int offset)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _offset = offset;
            _count = catalog.AssemblyTable.ReadInt32(offset);
            _index = -1;
        }

        IEnumerator<ApiModel> IEnumerable<ApiModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public RootApiEnumerator GetEnumerator()
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
                var offset = _catalog.AssemblyTable.ReadInt32(_offset + 4 + (4 * _index));
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

    public struct FrameworkEnumerator : IEnumerable<FrameworkModel>, IEnumerator<FrameworkModel>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public FrameworkEnumerator(ApiCatalogModel catalog, int offset)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _offset = offset;
            _count = catalog.AssemblyTable.ReadInt32(offset);
            _index = -1;
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
                var offset = _catalog.AssemblyTable.ReadInt32(_offset + 4 + (4 * _index));
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

    public struct PackageEnumerator : IEnumerable<(PackageModel Package, FrameworkModel Framework)>, IEnumerator<(PackageModel Package, FrameworkModel Framework)>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public PackageEnumerator(ApiCatalogModel catalog, int offset)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _offset = offset;
            _count = catalog.AssemblyTable.ReadInt32(offset);
            _index = -1;
        }

        IEnumerator<(PackageModel Package, FrameworkModel Framework)> IEnumerable<(PackageModel Package, FrameworkModel Framework)>.GetEnumerator()
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

        public (PackageModel Package, FrameworkModel Framework) Current
        {
            get
            {
                var offset = _offset + 4 + (_index * 8);
                var packageOffset = _catalog.AssemblyTable.ReadInt32(offset);
                var frameworkOffset = _catalog.AssemblyTable.ReadInt32(offset + 4);
                var packageModel = new PackageModel(_catalog, packageOffset);
                var frameworkModel = new FrameworkModel(_catalog, frameworkOffset);
                return (packageModel, frameworkModel);
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
