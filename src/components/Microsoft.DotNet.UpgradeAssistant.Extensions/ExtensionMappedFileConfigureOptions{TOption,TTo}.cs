// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    internal class ExtensionMappedFileConfigureOptions<TOption, TTo> : IConfigureOptions<ICollection<TTo>>
    {
        private readonly Func<TOption, IEnumerable<string>> _factory;
        private readonly bool _isArray;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly IOptions<ICollection<FileOption<TOption>>> _options;
        private readonly ILogger<ExtensionMappedFileConfigureOptions<TOption, TTo>> _logger;

        public ExtensionMappedFileConfigureOptions(
            Func<TOption, IEnumerable<string>> factory,
            bool isArray,
            IOptions<JsonSerializerOptions> serializerOptions,
            IOptions<ICollection<FileOption<TOption>>> options,
            ILogger<ExtensionMappedFileConfigureOptions<TOption, TTo>> logger)
        {
            _factory = factory;
            _isArray = isArray;
            _serializerOptions = serializerOptions.Value;
            _options = options;
            _logger = logger;
        }

        public void Configure(ICollection<TTo> options)
        {
            foreach (var other in _options.Value)
            {
                foreach (var path in _factory(other.Value))
                {
                    foreach (var match in other.Files.GetFiles(path))
                    {
                        var fileInfo = other.Files.GetFileInfo(match.Path);
                        var directory = Path.GetDirectoryName(match.Path);
                        var newFileProvider = new SubFileProvider(other.Files, directory);

                        foreach (var obj in ReadAll(fileInfo))
                        {
                            if (obj is IFileOption fileOption)
                            {
                                fileOption.Files = newFileProvider;
                            }

                            options.Add(obj);
                        }
                    }
                }
            }
        }

        private class SubFileProvider : IFileProvider
        {
            private readonly IFileProvider _other;
            private readonly string _path;

            public SubFileProvider(IFileProvider other, string path)
            {
                _other = other;
                _path = path;
            }

            public IDirectoryContents GetDirectoryContents(string subpath)
                => _other.GetDirectoryContents(Path.Combine(_path, subpath));

            public IFileInfo GetFileInfo(string subpath)
                => _other.GetFileInfo(Path.Combine(_path, subpath));

            public IChangeToken Watch(string filter)
                => _other.Watch(Path.Combine(_path, filter));
        }

        private TTo[] ReadAll(IFileInfo file)
        {
            if (!file.Exists)
            {
                return Array.Empty<TTo>();
            }

            try
            {
                // We must read all the contents as JsonSerializer does not support synchronous reading from
                // a stream, and the options infrastructure requires synchronous calls.
                using var stream = file.CreateReadStream();
                using var reader = new StreamReader(stream);
                var contents = reader.ReadToEnd();

                if (_isArray)
                {
                    return JsonSerializer.Deserialize<TTo[]>(contents, _serializerOptions) ?? Array.Empty<TTo>();
                }
                else
                {
                    var result = JsonSerializer.Deserialize<TTo>(contents, _serializerOptions);

                    return result is null ? Array.Empty<TTo>() : new[] { result };
                }
            }
            catch (JsonException exc)
            {
                _logger.LogDebug(exc, "File {PackageMapPath} is not a valid package map file", file);
                return Array.Empty<TTo>();
            }
        }
    }
}
