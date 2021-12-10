// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// An AdditionalText implementation that reads a document from disk.
    /// </summary>
    internal class AdditionalFileText : AdditionalText
    {
        /// <summary>
        /// Gets the path to read text file.
        /// </summary>
        public override string Path { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalFileText"/> class.
        /// </summary>
        /// <param name="filePath">The file path to read the text from.</param>
        public AdditionalFileText(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"File path {filePath} not found", nameof(filePath));
            }

            Path = filePath;
        }

        /// <summary>
        /// Gets the text of the file.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The text of the file specified by this object's path property.</returns>
        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(File.ReadAllText(Path));
    }
}
