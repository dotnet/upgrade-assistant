// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct PackageModel : IEquatable<PackageModel>
{
    internal PackageModel(ApiCatalogModel catalog, int offset)
    {
        Catalog = catalog;
        Id = offset;
    }

    public ApiCatalogModel Catalog { get; }

    public int Id { get; }

    public string Name
    {
        get
        {
            var stringOffset = Catalog.PackageTable.ReadInt32(Id);
            return Catalog.GetString(stringOffset);
        }
    }

    public string Version
    {
        get
        {
            var stringOffset = Catalog.PackageTable.ReadInt32(Id + 4);
            return Catalog.GetString(stringOffset);
        }
    }

    public AssemblyEnumerator Assemblies
    {
        get
        {
            return new AssemblyEnumerator(Catalog, Id + 8);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is FrameworkModel model && Equals(model);
    }

    public bool Equals(PackageModel other)
    {
        return ReferenceEquals(Catalog, other.Catalog) &&
               Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Catalog, Id);
    }

    public static bool operator ==(PackageModel left, PackageModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PackageModel left, PackageModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }

    public struct AssemblyEnumerator : IEnumerable<(FrameworkModel Framework, AssemblyModel Assembly)>, IEnumerator<(FrameworkModel Framework, AssemblyModel Assembly)>
    {
        private readonly ApiCatalogModel _catalog;
        private readonly int _offset;
        private readonly int _count;
        private int _index;

        public AssemblyEnumerator(ApiCatalogModel catalog, int offset)
        {
            ArgumentNullException.ThrowIfNull(catalog);

            _catalog = catalog;
            _offset = offset;
            _count = catalog.PackageTable.ReadInt32(offset);
            _index = -1;
        }

        IEnumerator<(FrameworkModel Framework, AssemblyModel Assembly)> IEnumerable<(FrameworkModel Framework, AssemblyModel Assembly)>.GetEnumerator()
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

        public (FrameworkModel Framework, AssemblyModel Assembly) Current
        {
            get
            {
                var offset = _offset + 4 + (_index * 8);
                var frameworkOffset = _catalog.PackageTable.ReadInt32(offset);
                var assemblyOffset = _catalog.PackageTable.ReadInt32(offset + 4);
                var frameworkModel = new FrameworkModel(_catalog, frameworkOffset);
                var assemblyModel = new AssemblyModel(_catalog, assemblyOffset);
                return (frameworkModel, assemblyModel);
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
