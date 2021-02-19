// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class DirectoryExtension : DefaultExtension
    {
        public const string ManifestFileName = "ExtensionManifest.json";
        private const string ExtensionNamePropertyName = "ExtensionName";

        public override string Name { get; }

        public DirectoryExtension(string directory, ILogger<DirectoryExtension> logger)
            : base(GetConfiguration(directory), logger, directory)
        {
            var configuration = GetConfiguration(directory);
            Name = configuration[ExtensionNamePropertyName] ?? $"Extensions from {directory}";
        }

        public static IConfiguration GetConfiguration(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new ArgumentException($"Extension diretory does not exist: {directory}", nameof(directory));
            }

            var manifestPath = Path.Combine(directory, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException($"Extension manifest not found: {manifestPath}");
            }

            return new ConfigurationBuilder().AddJsonFile(manifestPath, false).Build();
        }
    }
}
