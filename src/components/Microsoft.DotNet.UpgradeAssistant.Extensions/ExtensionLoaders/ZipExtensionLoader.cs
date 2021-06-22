// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ZipExtensionLoader : IExtensionLoader
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The file provider will be disposed when the extension instance is disposed.")]
        public ExtensionInstance? LoadExtension(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            if (Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var provider = new ZipFileProvider(path);

                try
                {
                    return new ExtensionInstance(provider, path);
                }

                // If the manifest file couldn't be found, let's try looking at one layer deep with the name
                // of the file as the first folder. This is what happens when you create a zip file from a folder
                // with Windows or 7-zip
                catch (UpgradeException ex) when (ex.InnerException is FileNotFoundException)
                {
                    var subpath = Path.GetFileNameWithoutExtension(path);
                    var subprovider = new SubFileProvider(provider, subpath);
                    return new ExtensionInstance(subprovider, path);
                }
            }

            return null;
        }
    }
}
