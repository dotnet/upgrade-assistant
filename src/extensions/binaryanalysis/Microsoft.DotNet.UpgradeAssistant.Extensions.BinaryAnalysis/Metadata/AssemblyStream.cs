// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

internal static class AssemblyStream
{
    public static async Task<MetadataReference> CreateAsync(Stream stream, string path)
    {
        if (!stream.CanSeek)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;
            stream.Dispose();
            stream = memoryStream;
        }

        return MetadataReference.CreateFromStream(stream, filePath: path);
    }
}
