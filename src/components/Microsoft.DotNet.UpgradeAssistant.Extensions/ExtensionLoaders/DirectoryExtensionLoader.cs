// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class DirectoryExtensionLoader : IExtensionLoader
    {
        private readonly ExtensionInstanceFactory _factory;

        public DirectoryExtensionLoader(ExtensionInstanceFactory factory)
        {
            _factory = factory;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The file provider will be disposed when the extension instance is disposed.")]
        public ExtensionInstance? LoadExtension(string path)
        {
            if (Directory.Exists(path))
            {
                return _factory.Create(new PhysicalFileProvider(path), path);
            }

            return null;
        }
    }
}
