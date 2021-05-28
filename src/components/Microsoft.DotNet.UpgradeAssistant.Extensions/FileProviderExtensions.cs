// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal static class FileProviderExtensions
    {
        public static IEnumerable<FilePatternMatch> GetFiles(this IFileProvider provider, string glob)
        {
            var matcher = new Matcher();
            matcher.AddInclude(glob);

            return matcher.Execute(new FileProviderGlobbingDirectory(provider)).Files;
        }

#nullable disable
        internal class FileProviderGlobbingDirectory : DirectoryInfoBase
        {
            private const char DirectorySeparatorChar = '/';
            private readonly IFileProvider _fileProvider;
            private readonly IFileInfo _fileInfo;
            private readonly FileProviderGlobbingDirectory _parent;
            private readonly bool _isRoot;

            public FileProviderGlobbingDirectory(
                IFileProvider fileProvider,
                IFileInfo fileInfo = null,
                FileProviderGlobbingDirectory parent = null)
            {
                _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
                _fileInfo = fileInfo;
                _parent = parent;

                if (_fileInfo == null)
                {
                    // We're the root of the directory tree
                    RelativePath = string.Empty;
                    _isRoot = true;
                }
                else if (!string.IsNullOrEmpty(parent?.RelativePath))
                {
                    // We have a parent and they have a relative path so concat that with my name
                    RelativePath = _parent.RelativePath + DirectorySeparatorChar + _fileInfo.Name;
                }
                else
                {
                    // We have a parent which is the root, so just use my name
                    RelativePath = _fileInfo.Name;
                }
            }

            public string RelativePath { get; }

            public override string FullName
            {
                get
                {
                    if (_isRoot)
                    {
                        // We're the root, so just use our name
                        return Name;
                    }

                    return _parent.FullName + DirectorySeparatorChar + Name;
                }
            }

            public override string Name => _fileInfo?.Name;

            public override DirectoryInfoBase ParentDirectory => _parent;

            public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
            {
                foreach (var fileInfo in _fileProvider.GetDirectoryContents(RelativePath))
                {
                    yield return BuildFileResult(fileInfo);
                }
            }

            public override DirectoryInfoBase GetDirectory(string path)
            {
                return new FileProviderGlobbingDirectory(_fileProvider, _fileProvider.GetFileInfo(path), this);
            }

            public override FileInfoBase GetFile(string path)
            {
                return new FileProviderGlobbingFile(_fileProvider.GetFileInfo(path), this);
            }

            private FileSystemInfoBase BuildFileResult(IFileInfo fileInfo)
            {
                if (fileInfo.IsDirectory)
                {
                    return new FileProviderGlobbingDirectory(_fileProvider, fileInfo, this);
                }

                return new FileProviderGlobbingFile(fileInfo, this);
            }

            internal class FileProviderGlobbingFile : FileInfoBase
            {
                private const char DirectorySeparatorChar = '/';

                public FileProviderGlobbingFile(IFileInfo fileInfo, DirectoryInfoBase parent)
                {
                    if (fileInfo == null)
                    {
                        throw new ArgumentNullException(nameof(fileInfo));
                    }

                    Name = fileInfo.Name;
                    ParentDirectory = parent ?? throw new ArgumentNullException(nameof(parent));
                    FullName = ParentDirectory.FullName + DirectorySeparatorChar + Name;
                }

                public override string FullName { get; }

                public override string Name { get; }

                public override DirectoryInfoBase ParentDirectory { get; }
            }
        }
    }
}
