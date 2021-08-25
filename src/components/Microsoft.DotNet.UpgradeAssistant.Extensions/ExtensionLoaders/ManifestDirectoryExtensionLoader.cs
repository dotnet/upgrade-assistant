// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ManifestDirectoryExtensionLoader : IExtensionLoader
    {
        private readonly ExtensionInstanceFactory _factory;

        public ManifestDirectoryExtensionLoader(ExtensionInstanceFactory factory)
        {
            _factory = factory;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The file provider will be disposed when the extension instance is disposed.")]
        public ExtensionInstance? LoadExtension(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var filename = Path.GetFileName(path);
            if (ExtensionInstance.ManifestFileName.Equals(filename, StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(path) ?? string.Empty;
                return _factory.Create(new PhysicalFileProvider(dir), dir);
            }

            return null;
        }
    }
}
