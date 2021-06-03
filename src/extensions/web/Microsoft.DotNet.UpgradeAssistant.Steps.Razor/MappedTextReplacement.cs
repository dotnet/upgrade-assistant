// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// Represents a proposed text replacement in a file. These are stored so that replacements can
    /// be identified in generated C# and then applied (in reverse line order) in cshtml documents.
    /// </summary>
    public class MappedTextReplacement : IEquatable<MappedTextReplacement>
    {
        /// <summary>
        /// Gets the path to the file text is to be replaced in.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the line in the file that the text replacement is to start in.
        /// </summary>
        public int StartingLine { get; }

        /// <summary>
        /// Gets the original text that is to be replaced.
        /// </summary>
        public string OriginalText { get; }

        /// <summary>
        /// Gets the new text to replace the original texts with.
        /// </summary>
        public string NewText { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappedTextReplacement"/> class, getting the original text, file path, and starting line from an original MappedSubText.
        /// </summary>
        /// <param name="originalText">A MappedSubText containing the original text and original text location.</param>
        /// <param name="newText">The new text to replace the original text with.</param>
        public MappedTextReplacement(MappedSubText originalText, string newText)
            : this(originalText?.Text.ToString() ?? throw new ArgumentNullException(nameof(originalText)), newText, originalText.FilePath, originalText.StartingLine)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappedTextReplacement"/> class.
        /// </summary>
        /// <param name="originalText">The original text to be replaced.</param>
        /// <param name="newText">The text to replace the original text with.</param>
        /// <param name="filePath">The path to the file text should be replaced in.</param>
        /// <param name="startingLine">The line number the original text starts on.</param>
        public MappedTextReplacement(string originalText, string newText, string filePath, int startingLine)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }

            OriginalText = originalText ?? throw new ArgumentNullException(nameof(originalText));
            NewText = newText ?? throw new ArgumentNullException(nameof(newText));
            FilePath = filePath;
            StartingLine = startingLine;
        }

        /// <summary>
        /// Returns true if another TextReplacement is equal to this one (same new and original text and location), false otherwise.
        /// </summary>
        /// <param name="other">The other TextReplacement to compare.</param>
        /// <returns>True of the other TextReplacement is equal to this one, false otherwise.</returns>
        public bool Equals(MappedTextReplacement? other) =>
            other != null &&
            FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase) &&
            StartingLine == other.StartingLine &&
            NewText.Equals(other.NewText, StringComparison.Ordinal) &&
            OriginalText.Equals(other.OriginalText, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as MappedTextReplacement);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashcode = default(HashCode);
            hashcode.Add(StartingLine);
            hashcode.Add(FilePath, StringComparer.OrdinalIgnoreCase);
            hashcode.Add(NewText, StringComparer.Ordinal);
            hashcode.Add(OriginalText, StringComparer.Ordinal);
            return hashcode.ToHashCode();
        }
    }
}
