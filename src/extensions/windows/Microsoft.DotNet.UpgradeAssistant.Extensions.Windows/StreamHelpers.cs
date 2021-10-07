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

        /// <summary>
        /// Write OutputStream with newLine that is added into the inputLines after any line containing existingLine.
        /// </summary>
        /// <param name="inputLines">lines from input source.</param>
        /// <param name="outputStream">stream to write the inputLines into.</param>
        /// <param name="existingLine">line that is already present in inputLines.</param>
        /// <param name="newLine">new line that will get added after any line containing existingLine.</param>
        /// <exception cref="ArgumentNullException">Throws a null exception if any of the params are null.</exception>
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
                    line.Append(string.Concat(GetLeadingWhitespace(i), newLine));
                }

                await output.WriteLineAsync(line.ToString()).ConfigureAwait(false);
            }
        }

        private static string GetLeadingWhitespace(string str)
        {
            return str.Replace(str.TrimStart(), string.Empty);
        }
    }
}
