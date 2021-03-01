// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Portability.Service
{
    public class PortabilityService : IPortabilityService
    {
        private static readonly Uri Url = new("/api/fxapi", UriKind.Relative);

        private readonly HttpClient _client;
        private readonly ILogger<PortabilityService> _logger;
        private readonly JsonSerializerOptions _options;

        public PortabilityService(HttpClient client, ILogger<PortabilityService> logger)
        {
            _client = client;
            _logger = logger;
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            _options.Converters.Add(new FrameworkNameConverter());
        }

        public async IAsyncEnumerable<ApiInformation> GetApiInformation(IReadOnlyCollection<string> apis, [EnumeratorCancellation] CancellationToken token)
        {
            using var message = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(apis));

            message.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            using var response = await _client.PostAsync(Url, message, token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("There was an error accessing the portability service: {Message}", response.ReasonPhrase);
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var result = await JsonSerializer.DeserializeAsync<IEnumerable<ApiInformation>>(stream, _options, cancellationToken: token).ConfigureAwait(false);

            if (result is null)
            {
                _logger.LogWarning("No api information available from request");
                yield break;
            }

            foreach (var item in result)
            {
                if (item.Definition is not null)
                {
                    yield return item;
                }
            }
        }

        private class FrameworkNameConverter : JsonConverter<FrameworkName>
        {
            public override FrameworkName? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var str = reader.GetString();

                if (str is null)
                {
                    return null;
                }

                try
                {
                    return new FrameworkName(str);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, FrameworkName value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
