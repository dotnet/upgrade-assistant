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
    public sealed record MappedSubText(SourceText Text, string FilePath, int StartingLine)
    {
        public string SourceLocation => $"{FilePath}@{StartingLine}";

        public static async Task<IEnumerable<MappedSubText>> GetMappedSubTextsAsync(Document document, CancellationToken token)
        {
            if (document is null)
            {
                throw new System.ArgumentNullException(nameof(document));
            }

            var syntaxRoot = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (syntaxRoot is null)
            {
                return Enumerable.Empty<MappedSubText>();
            }

            var ret = new List<MappedSubText>();
            var documentText = await document.GetTextAsync(token).ConfigureAwait(false);
            MappedSubText? currentMappedSubText = null;
            var subTextStart = 0;
            var lineDirectives = syntaxRoot.DescendantNodes(_ => true, true).OfType<LineDirectiveTriviaSyntax>();
            foreach (var directive in lineDirectives)
            {
                // Close the previous sub-text
                if (currentMappedSubText is not null)
                {
                    // Subtract two from directive.SpanStart to account for end-of-line trivia
                    ret.Add(currentMappedSubText with { Text = documentText.GetSubText(new TextSpan(subTextStart, directive.SpanStart - 2 - subTextStart)) });
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

        public bool Equals(MappedSubText other) =>
            other != null &&
            Text.ContentEquals(other.Text) &&
            SourceLocation.Equals(other.SourceLocation, StringComparison.Ordinal);

        public override int GetHashCode()
        {
            // https://stackoverflow.com/a/1646913
            var hash = 17;
            hash = (hash * 31) + Text.ToString().GetHashCode();
            hash = (hash * 31) + SourceLocation.GetHashCode();
            return hash;
        }
    }
}
