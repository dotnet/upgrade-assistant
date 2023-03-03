// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// Represents a portion of a source file that maps to a location in a different source file.
    /// </summary>
    public sealed record MappedSubText(SourceText Text, string FilePath, int StartingLine)
    {
        /// <summary>
        /// Gets the location of the mapped source text in the mapped file as a string with format "FilePath@FirstLineNumber".
        /// </summary>
        public string SourceLocation => $"{FilePath}@{StartingLine}";

        /// <summary>
        /// Gets all mapped source sections of a given Roslyn document based on #line pragmas.
        /// </summary>
        /// <param name="document">The document to look for mapped sections in.</param>
        /// <param name="defaultMapPath">The file path that code prior to the first #line pragma should map to or null to not map this code.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>An enumerable of mapped source sections containing the source text that is mapped and the location that source came from in the mapped file.</returns>
        public static async Task<IEnumerable<MappedSubText>> GetMappedSubTextsAsync(Document document, string? defaultMapPath, CancellationToken token)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var syntaxRoot = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (syntaxRoot is null)
            {
                return Enumerable.Empty<MappedSubText>();
            }

            var ret = new List<MappedSubText>();
            var documentText = await document.GetTextAsync(token).ConfigureAwait(false);

            var subTextStart = 0;

            // If the user has specified a file that code prior to the first #line should map to, then
            // create and initial mapped subtext that maps everything before the first #line to offset 0
            // to allow for code that's added prior to the first actual map.
            var currentMappedSubText = defaultMapPath is null
                ? null
                : new MappedSubText(documentText, defaultMapPath, 0);

            var lineDirectives = syntaxRoot.DescendantNodes(_ => true, true).OfType<LineDirectiveTriviaSyntax>();
            foreach (var directive in lineDirectives)
            {
                // Close the previous sub-text
                if (currentMappedSubText is not null)
                {
                    // Calculate the length of the new-line sequence (which may or may not be the native Environment.NewLine length)
                    var text = documentText.GetSubText(new TextSpan(subTextStart, directive.SpanStart - subTextStart));
                    int lineEndingLength = 0;

                    if (text[text.Length - 1] == '\n')
                    {
                        lineEndingLength++;
                        if (text[text.Length - 2] == '\r')
                        {
                            lineEndingLength++;
                        }
                    }

                    // Capture the mapped subtext minus the new-line sequence.
                    ret.Add(currentMappedSubText with { Text = documentText.GetSubText(new TextSpan(subTextStart, directive.SpanStart - lineEndingLength - subTextStart)) });
                    currentMappedSubText = null;
                }

                // If the #line directive has a valid line and file, then it is a new mapped sub-section
                if (directive.Line.IsKind(SyntaxKind.NumericLiteralToken) && directive.Line.Value is int lineNumber && directive.File.IsKind(SyntaxKind.StringLiteralToken))
                {
                    subTextStart = directive.FullSpan.End;
                    currentMappedSubText = new MappedSubText(documentText.GetSubText(subTextStart), directive.File.ValueText, lineNumber);
                }
            }

            if (currentMappedSubText is not null)
            {
                ret.Add(currentMappedSubText);
            }

            return ret;
        }

        /// <inheritdoc/>
        public bool Equals(MappedSubText other) =>
            other != null &&
            Text.ContentEquals(other.Text) &&
            SourceLocation.Equals(other.SourceLocation, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashcode = default(HashCode);
            hashcode.Add(Text.ToString(), StringComparer.Ordinal);
            hashcode.Add(SourceLocation, StringComparer.OrdinalIgnoreCase);
            return hashcode.ToHashCode();
        }
    }
}
