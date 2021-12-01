// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class ExtensionLocator : IExtensionLocator
    {
        private readonly IOptions<ExtensionOptions> _options;

        public ExtensionLocator(IOptions<ExtensionOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string GetInstallPath(ExtensionSource extensionSource)
        {
            if (extensionSource is null)
            {
                throw new ArgumentNullException(nameof(extensionSource));
            }

            if (string.IsNullOrEmpty(extensionSource.Version))
            {
                throw new UpgradeException("Cannot get path of source without version");
            }

            var sourcePath = GetSourceForPath(extensionSource.Source);

            return Path.Combine(_options.Value.ExtensionCachePath, sourcePath, extensionSource.Name, extensionSource.Version);
        }

        /// <summary>
        /// Sources will usually be URL-style strings, which cannot be put into a filename. Instead, we take a hash of it and use that as the source parameter.
        /// </summary>
        /// <param name="source">Original source path</param>
        /// <returns>A hashed source suitable for insertion into a path</returns>
        private string GetSourceForPath(string source)
        {
            Span<byte> stringBytes = stackalloc byte[source.Length];
            Encoding.UTF8.GetBytes(source, stringBytes);

            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(stringBytes, hash);

            return Convert.ToBase64String(hash);
        }
    }
}
