// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.NuGet
{
    public class HttpPackageSearch : IPackageSearch
    {
        private readonly ILogger<HttpPackageSearch> _logger;
        private readonly HttpClient _client;

        public HttpPackageSearch(ILogger<HttpPackageSearch> logger, HttpClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async IAsyncEnumerable<NuGetReference> SearchAsync(string name, string source, string? packageType, [EnumeratorCancellation] CancellationToken token)
        {
            const int SearchTake = 100;

            if (!string.Equals(source, "https://api.nuget.org/v3/index.json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Search is only supported on NuGet.org");
                yield break;
            }

            _logger.LogInformation("Searching '{Query}' from {Source} for {PackageType}", name, source, packageType);

            if (packageType is null)
            {
                packageType = string.Empty;
            }

            // This is the URL for searching NuGet.org via REST: https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource
            var url = new Uri($"https://azuresearch-usnc.nuget.org/query?q={name}&skip=0&take={SearchTake}&prerelease=true&packageType={packageType}&semVerLevel=2.0.0");

            var result = await _client.GetByteArrayAsync(url, token).ConfigureAwait(false);
            var deserialized = JsonSerializer.Deserialize<Root>(result);

            if (deserialized is null)
            {
                yield break;
            }

            foreach (var data in deserialized.Data)
            {
                if (data.Id is not null && data.Version is not null)
                {
                    yield return new NuGetReference(data.Id, data.Version);
                }
            }
        }

        private class Root
        {
            [JsonPropertyName("data")]
            public Datum[] Data { get; set; } = Array.Empty<Datum>();
        }

        private class Datum
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("version")]
            public string? Version { get; set; }
        }
    }
}
