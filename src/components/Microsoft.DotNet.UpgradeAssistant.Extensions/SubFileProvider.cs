// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal sealed class SubFileProvider : IFileProvider
    {
        private readonly IFileProvider _other;
        private readonly string _path;

        public SubFileProvider(IFileProvider other, string path)
        {
            _other = other;
            _path = path;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
            => _other.GetDirectoryContents(Path.Combine(_path, subpath));

        public IFileInfo GetFileInfo(string subpath)
            => _other.GetFileInfo(Path.Combine(_path, subpath));

        public IChangeToken Watch(string filter)
            => _other.Watch(Path.Combine(_path, filter));
    }
}
