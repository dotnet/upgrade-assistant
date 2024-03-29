﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MSBuild.Abstractions
{
    public static class NugetHelpers
    {
        private const string SearchUrl = "https://api-v2v3search-0.nuget.org/query?q={0}&prerelease=false&semVerLevel=2.0.0&take=1";

        private static readonly Dictionary<string, string?> _packageToVersionCache = new Dictionary<string, string?>();
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async ValueTask<string?> GetLatestVersionForPackageNameAsync(string packageName)
        {
            if (_packageToVersionCache.TryGetValue(packageName, out var version))
            {
                return version;
            }

            var response = await _httpClient.GetAsync(string.Format(SearchUrl, packageName));
            var result = await response.Content.ReadAsStreamAsync();
            version = GetVersionFromQueryResponse(result);
            _packageToVersionCache[packageName] = version;
            return version;

            static string? GetVersionFromQueryResponse(Stream result)
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var array))
                {
                    foreach (var element in array.EnumerateArray())
                    {
                        return element.GetProperty("version").ToString();
                    }
                }

                return null;
            }
        }

        public static string FindPackageNameFromReferenceName(string referenceName)
        {
            if (StringComparer.OrdinalIgnoreCase.Compare(referenceName, "System.ComponentModel.DataAnnotations") == 0)
            {
                return "System.ComponentModel.Annotations";
            }

            return referenceName;
        }
    }
}
