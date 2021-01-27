using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.Extensions
{
    public class DefaultExtensionProvider : IExtensionProvider
    {
        private readonly string _baseDirectory;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public virtual string Name => "Default extensions";

        public DefaultExtensionProvider(IConfiguration configuration, ILogger<DefaultExtensionProvider> logger)
            : this(configuration, logger, AppContext.BaseDirectory)
        { }

        public DefaultExtensionProvider(IConfiguration configuration, ILogger<DefaultExtensionProvider> logger, string baseDirectory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        }

        public Stream? GetFile(string path)
        {
            var filePath = GetAbsolutePath(path);
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Expected extension file does not exist at {Path}", filePath);
                return null;
            }

            return File.OpenRead(filePath);
        }

        public T? GetOptions<T>(string sectionName) => _configuration.GetSection(sectionName).Get<T>();

        public IEnumerable<string> GetFiles(string path) => GetFiles(path, "*");

        public IEnumerable<string> GetFiles(string path, string searchPattern)
        {
            var dirPath = GetAbsolutePath(path);
            if (!Directory.Exists(dirPath))
            {
                _logger.LogWarning("Expected extension directory does not exist at {Path}", dirPath);
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(dirPath, searchPattern, new EnumerationOptions { RecurseSubdirectories = true });
        }

        protected string GetAbsolutePath(string path) =>
            Path.IsPathFullyQualified(path)
            ? path
            : Path.Combine(_baseDirectory, path);
    }
}
