// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class NuGetExtensionDownloader : IExtensionDownloader
    {
        private readonly IOptions<ExtensionOptions> _options;
        private readonly Lazy<IPackageDownloader> _packageDownloader;
        private readonly ILogger<NuGetExtensionDownloader> _logger;

        public NuGetExtensionDownloader(
            IOptions<ExtensionOptions> options,
            Lazy<IPackageDownloader> packageDownloader,
            ILogger<NuGetExtensionDownloader> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _packageDownloader = packageDownloader ?? throw new ArgumentNullException(nameof(packageDownloader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string?> GetLatestVersionAsync(ExtensionSource n, CancellationToken token)
        {
            if (n is null)
            {
                throw new ArgumentNullException(nameof(n));
            }

            var result = await _packageDownloader.Value.GetNuGetReference(n.Name, n.Version, n.Source ?? _options.Value.DefaultSource, token).ConfigureAwait(false);

            return result?.Version;
        }

        public async Task<string?> RestoreAsync(ExtensionSource source, CancellationToken token)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(source.Version))
            {
                _logger.LogWarning("Cannot restore a source {Name} with no version", source.Name);
                return null;
            }

            var path = GetInstallPath(source);

            if (Directory.Exists(path))
            {
                _logger.LogDebug("{Name} is already restored at {Path}", source.Name, path);
                return path;
            }

            _logger.LogDebug("Creating directory {Path} for extension {Name}", path, source.Name);

            Directory.CreateDirectory(path);
            var package = new NuGetReference(source.Name, source.Version);

            if (await _packageDownloader.Value.DownloadPackageToDirectoryAsync(path, package, source.Source ?? _options.Value.DefaultSource, token).ConfigureAwait(false))
            {
                return path;
            }

            Directory.Delete(path, true);

            return null;
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
