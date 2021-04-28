// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// Updater service for applying text replacements to Razor documents.
    /// </summary>
    public class RazorTextReplacer : ITextReplacer
    {
        private static readonly Regex UsingBlockRegex = new Regex(@"^(\s*using\s+(?<namespace>.+?);+\s*)+$", RegexOptions.Compiled);
        private static readonly Regex UsingNamespaceRegex = new Regex(@"using\s+(?<namespace>.+?);", RegexOptions.Compiled);

        private readonly ILogger<RazorTextReplacer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorTextReplacer"/> class.
        /// </summary>
        /// <param name="logger"></param>
        public RazorTextReplacer(ILogger<RazorTextReplacer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Updates source code in Razor documents based on provider TextReplacements, accounting for Razor source code transition syntax.
        /// </summary>
        /// <param name="replacements">The text replacements to apply.</param>
        public void ApplyTextReplacements(IList<TextReplacement> replacements)
        {
            if (replacements is null)
            {
                throw new ArgumentNullException(nameof(replacements));
            }

            // Load each file with replacements into memory and update based on replacements
            var replacementsByFile = replacements.Distinct().OrderByDescending(t => t.StartingLine).GroupBy(t => t.FilePath);
            foreach (var replacementGroup in replacementsByFile)
            {
                // Read the document as lines instead of all as one string because replacements
                // include line offsets.
                var documentLines = File.ReadAllLines(replacementGroup.Key);
                var documentText = new StringBuilder();
                foreach (var line in documentLines)
                {
                    documentText.AppendLine(line);
                }

                foreach (var replacement in replacementGroup)
                {
                    _logger.LogInformation("Updating source code in Razor document {FilePath} at line {Line}", replacement.FilePath, replacement.StartingLine);

                    // Start looking for replacements at the start of the indicated line
                    var startOffset = GetLineOffset(documentLines, replacement.StartingLine);

                    // Stop looking for replacements at the start of the first line after the indicated line plus the number of lines in the indicated text
                    var endOffset = GetLineOffset(documentLines, replacement.StartingLine + replacement.OriginalText.Lines.Count);

                    // Trim the string that's being replaced because code from Razor code blocks will include a couple extra spaces (to make room for @{)
                    // compared to the source that actually appeared in the cshtml file.
                    var originalText = replacement.OriginalText.ToString().TrimStart();
                    var updatedText = replacement.NewText.ToString().TrimStart();
                    MinimizeReplacement(ref originalText, ref updatedText);

                    // If new text is being added, insert it with correct Razor transition syntax
                    if (string.IsNullOrWhiteSpace(originalText))
                    {
                        // Using statements are inserted with special implicit Razor expression syntax
                        if (UsingBlockRegex.IsMatch(updatedText))
                        {
                            var formattedDeclarations = new StringBuilder();
                            var usingStatementMatches = UsingNamespaceRegex.Matches(updatedText);
                            foreach (Match match in usingStatementMatches)
                            {
                                formattedDeclarations.AppendLine($"@using {match.Groups["namespace"].Value}");
                            }

                            documentText.Insert(startOffset, formattedDeclarations.ToString());
                        }
                        else
                        {
                            documentText.Insert(startOffset, $"@{{ {updatedText} }}");
                        }
                    }
                    else
                    {
                        // If the original text was completely removed, also search for implicit and explicit Razor expression syntax (@ or @()) so that it will be cleaned up, too
                        if (string.IsNullOrWhiteSpace(updatedText))
                        {
                            var implicitExpression = $"@{originalText.Replace(";", string.Empty)}";
                            var explicitExpression = $"@({originalText.Replace(";", string.Empty).Trim()})";
                            documentText.Replace(implicitExpression, updatedText, startOffset, endOffset - startOffset);
                            documentText.Replace(explicitExpression, updatedText, startOffset, endOffset - startOffset);
                        }

                        documentText.Replace(originalText, updatedText, startOffset, endOffset - startOffset);
                    }
                }

                File.WriteAllText(replacementGroup.Key, documentText.ToString());
            }
        }

        private static int GetLineOffset(string[] lines, int startingLine)
        {
            var offset = 0;

            for (var i = 1; i < startingLine && i <= lines.Length; i++)
            {
                // StreamSourceDoc.Lines is 0-based but line directives (as used in MappedSubText) are 1-based,
                // so subtract one from i.
                offset += lines[i - 1].Length + Environment.NewLine.Length;
            }

            return offset;
        }

        // Removes leading and trailing portions of original and updated that are the same
        private static void MinimizeReplacement(ref string original, ref string? updated)
        {
            if (updated is null)
            {
                return;
            }

            var index = 0;
            while (index < original.Length && index < updated.Length && original[index] == updated[index])
            {
                index++;
            }

            var endIndex = 0;
            while (endIndex > -original.Length && endIndex > -updated.Length && original[original.Length - 1 + endIndex] == updated[updated.Length - 1 + endIndex])
            {
                endIndex--;
            }

            original = original.Substring(index, Math.Max(0, original.Length + endIndex - index));
            updated = updated.Substring(index, Math.Max(0, updated.Length + endIndex - index));
        }
    }
}
