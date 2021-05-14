// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal sealed class ZipFileProvider : IFileProvider, IDisposable
    {
        private readonly ZipArchive _archive;
        private readonly StringComparison _comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        public ZipFileProvider(string path)
        {
            _archive = new ZipArchive(File.OpenRead(path), ZipArchiveMode.Read, leaveOpen: false);
        }

        public ZipFileProvider(ZipArchive archive)
        {
            _archive = archive;
        }

        public void Dispose()
        {
            _archive.Dispose();
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var list = new List<IFileInfo>();

            foreach (var entry in _archive.Entries)
            {
                var dir = Path.GetDirectoryName(entry.FullName);

                if (string.Equals(subpath, dir, _comparison))
                {
                    list.Add(new ZipFileInfo(entry));
                }
            }

            if (list.Count == 0)
            {
                return NotFoundDirectoryContents.Singleton;
            }

            return new ListDirectoryContents(list);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var entry = _archive.GetEntry(subpath);

            if (entry is null)
            {
                return new NotFoundFileInfo(subpath);
            }

            return new ZipFileInfo(entry);
        }

        public IChangeToken Watch(string filter)
            => NullChangeToken.Singleton;

        private class ZipFileInfo : IFileInfo
        {
            private readonly ZipArchiveEntry _entry;

            public ZipFileInfo(ZipArchiveEntry entry)
            {
                _entry = entry;
            }

            public bool Exists => true;

            public long Length => _entry.Length;

            // NOTE: Must return null, otherwise, it will load from a FileStream instead.
            public string PhysicalPath => null!;

            public string Name => _entry.Name;

            public DateTimeOffset LastModified => _entry.LastWriteTime;

            public bool IsDirectory => false;

            public Stream CreateReadStream()
                => _entry.Open();
        }

        private class ListDirectoryContents : IDirectoryContents
        {
            private readonly List<IFileInfo> _list;

            public ListDirectoryContents(List<IFileInfo> list)
            {
                _list = list;
            }

            public bool Exists => true;

            public IEnumerator<IFileInfo> GetEnumerator()
                => _list.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}
