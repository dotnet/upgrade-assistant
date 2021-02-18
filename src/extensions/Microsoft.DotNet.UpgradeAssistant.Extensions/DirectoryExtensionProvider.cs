using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class DirectoryExtensionProvider : DefaultExtensionProvider
    {
        public const string ManifestFileName = "ExtensionManifest.json";
        private const string ExtensionNamePropertyName = "ExtensionName";

        public override string Name { get; }

        public DirectoryExtensionProvider(string directory, ILogger<DirectoryExtensionProvider> logger)
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
