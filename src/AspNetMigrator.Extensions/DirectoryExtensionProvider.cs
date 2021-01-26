using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Extensions
{
    public class DirectoryExtensionProvider : DefaultExtensionProvider
    {
        public const string ManifestFileName = "ExtensionManifest.json";

        public DirectoryExtensionProvider(string directory, ILogger<DirectoryExtensionProvider> logger)
            : base(GetConfiguration(directory), logger, directory)
        {
            Name = $"Extensions from {directory}";
        }

        private static IConfiguration GetConfiguration(string directory)
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
