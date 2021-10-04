// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public static class StreamHelpers
    {
        private const int DefaultBufferSize = 1024;

        public static async Task CopyStreamWithNewLineAdded(string[] inputLines, Stream outputStream, string existingLine, string newLine)
        {
            if (inputLines is null)
            {
                throw new ArgumentNullException(nameof(inputLines));
            }

            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (existingLine is null)
            {
                throw new ArgumentNullException(nameof(existingLine));
            }

            if (newLine is null)
            {
                throw new ArgumentNullException(nameof(newLine));
            }

            using var output = new StreamWriter(outputStream, Encoding.UTF8, DefaultBufferSize, leaveOpen: true);
            foreach (var i in inputLines)
            {
                var line = new StringBuilder(i);
                if (i.Contains(existingLine))
                {
                    line.AppendLine();
                    line.Append(string.Concat("\t", newLine));
                }

                await output.WriteLineAsync(line.ToString()).ConfigureAwait(false);
            }
        }
    }
}
