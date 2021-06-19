// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Telemetry
{
    public sealed class UserLevelCacheWriter : IUserLevelCacheWriter
    {
        private readonly TelemetryOptions _options;
        private readonly string _dotnetUpgradeAssistantUserProfileFolderPath;
        private readonly IFileManager _fileManager;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Options are always provided")]
        public UserLevelCacheWriter(IOptions<TelemetryOptions> options, IFileManager fileManager)
        {
            _options = options.Value;
            _dotnetUpgradeAssistantUserProfileFolderPath = Paths.DotnetUserProfileFolderPath;
            _fileManager = fileManager;
        }

        public string RunWithCache(string cacheKey, Func<string> getValueToCache)
        {
            _ = getValueToCache ?? throw new ArgumentNullException(nameof(getValueToCache));

            var cacheFilepath = GetCacheFilePath(cacheKey);
            try
            {
                if (!_fileManager.FileExists(cacheFilepath))
                {
                    if (!_fileManager.DirectoryExists(_dotnetUpgradeAssistantUserProfileFolderPath))
                    {
                        _fileManager.CreateDirectory(_dotnetUpgradeAssistantUserProfileFolderPath);
                    }

                    var runResult = getValueToCache();

                    _fileManager.WriteAllText(cacheFilepath, runResult);
                    return runResult;
                }
                else
                {
                    return _fileManager.ReadAllText(cacheFilepath);
                }
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException
                    || ex is PathTooLongException
                    || ex is IOException)
                {
                    return getValueToCache();
                }

                throw;
            }
        }

        private string GetCacheFilePath(string cacheKey)
        {
            return Path.Combine(_dotnetUpgradeAssistantUserProfileFolderPath, $"{_options.ProductVersion}_{cacheKey}.{_options.UserLevelCache}");
        }
    }
}
