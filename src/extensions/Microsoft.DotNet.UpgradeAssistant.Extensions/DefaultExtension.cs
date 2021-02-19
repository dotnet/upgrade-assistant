// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public class DefaultExtension : IExtension
    {
        private readonly string _baseDirectory;
        private readonly IConfiguration _configuration;

        public virtual string Name => "Default extension";

        public DefaultExtension(IConfiguration configuration)
            : this(configuration, AppContext.BaseDirectory)
        { }

        public DefaultExtension(IConfiguration configuration, string baseDirectory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        }

        public Stream? GetFile(string path)
        {
            var filePath = GetAbsolutePath(path);
            if (!File.Exists(filePath))
            {
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
