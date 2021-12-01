// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class NuGetExtensionPackageCreator : IExtensionCreator
    {
        private readonly Lazy<IPackageCreator> _packageCreator;

        public NuGetExtensionPackageCreator(Lazy<IPackageCreator> packageCreator)
        {
            _packageCreator = packageCreator ?? throw new ArgumentNullException(nameof(packageCreator));
        }

        public bool TryCreateExtensionFromDirectory(IExtensionInstance extension, Stream stream)
        {
            if (extension is null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!_packageCreator.Value.CreateArchive(extension, ExtensionManager.PackageType, stream))
            {
                return false;
            }

            stream.Position = 0;

            using var zip = new ZipArchive(stream, ZipArchiveMode.Update);

            foreach (var file in extension.FileProvider.GetFiles("*"))
            {
                var entry = zip.CreateEntry(file.Stem);

                using var entryStream = entry.Open();
                using var fileStream = extension.FileProvider.GetFileInfo(file.Stem).CreateReadStream();

                fileStream.CopyTo(entryStream);
            }

            return true;
        }
    }
}
