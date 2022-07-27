// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

public readonly struct FrameworkModel : IEquatable<FrameworkModel>
{
    internal FrameworkModel(ApiCatalogModel catalog, int offset)
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
            var stringOffset = Catalog.FrameworkTable.ReadInt32(Id);
            return Catalog.GetString(stringOffset);
        }
    }

    public AssemblyEnumerator Assemblies
    {
        get
        {
            return new AssemblyEnumerator(Catalog, Id + 4);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is FrameworkModel model && Equals(model);
    }

    public bool Equals(FrameworkModel other)
    {
        return ReferenceEquals(Catalog, other.Catalog) &&
               Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Catalog, Id);
    }

    public static bool operator ==(FrameworkModel left, FrameworkModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FrameworkModel left, FrameworkModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }

    public struct AssemblyEnumerator : IEnumerable<AssemblyModel>, IEnumerator<AssemblyModel>
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
            _count = catalog.FrameworkTable.ReadInt32(offset);
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
                var offset = _catalog.FrameworkTable.ReadInt32(_offset + 4 + (4 * _index));
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
}
