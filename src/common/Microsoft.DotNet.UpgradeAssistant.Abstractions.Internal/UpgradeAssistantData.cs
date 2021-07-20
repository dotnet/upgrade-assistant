// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record UpgradeAssistantData
    {
        public IEnumerable<ExtensionSource> Extensions { get; init; } = Enumerable.Empty<ExtensionSource>();

        public async Task SaveAsync(string path, CancellationToken token)
        {
            using var stream = File.OpenWrite(path);
            await JsonSerializer.SerializeAsync(stream, this, cancellationToken: token).ConfigureAwait(false);
        }

        public static async Task<UpgradeAssistantData> LoadAsync(string path, CancellationToken token)
        {
            if (File.Exists(path))
            {
                using var stream = File.OpenRead(path);
                var data = await JsonSerializer.DeserializeAsync<UpgradeAssistantData>(stream, cancellationToken: token).ConfigureAwait(false);

                if (data is not null)
                {
                    return data;
                }
            }

            return new();
        }
    }
}
