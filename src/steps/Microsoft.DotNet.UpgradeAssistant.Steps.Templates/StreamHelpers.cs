// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public static class StreamHelpers
    {
        public static async Task CopyStreamWithTokenReplacementAsync(Stream templateStream, Stream outputStream, Dictionary<string, string> tokenReplacements)
        {
            if (templateStream is null)
            {
                throw new ArgumentNullException(nameof(templateStream));
            }

            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (tokenReplacements is null)
            {
                throw new ArgumentNullException(nameof(tokenReplacements));
            }

            // Read the file contents locally line by line to simplify token replacement.
            // This is inefficient but makes the code straightforward. If performance becomes an issue
            // here, we can replace tokens as we're writing the stream to the new file instead.
            // That would be faster but would be more complicated and is likely not necessary.
            using var input = new StreamReader(templateStream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            using var output = new StreamWriter(outputStream, leaveOpen: true);
            while (!input.EndOfStream)
            {
                var line = new StringBuilder(await input.ReadLineAsync().ConfigureAwait(false));

                foreach (var key in tokenReplacements.Keys)
                {
                    line.Replace(key, tokenReplacements[key]);
                }

                await output.WriteLineAsync(line).ConfigureAwait(false);
            }
        }
    }
}
