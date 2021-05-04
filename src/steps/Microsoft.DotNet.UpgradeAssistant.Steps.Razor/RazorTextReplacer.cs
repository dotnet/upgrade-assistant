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
        private static readonly Regex UsingBlockRegex = new(@"^(\s*using\s+(?<namespace>.+?);+\s*)+$", RegexOptions.Compiled);
        private static readonly Regex UsingNamespaceRegex = new(@"using\s+(?<namespace>.+?);", RegexOptions.Compiled);

        private readonly ILogger<RazorTextReplacer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorTextReplacer"/> class.
        /// </summary>
        /// <param name="logger">Logger for logging diagnostics.</param>
        public RazorTextReplacer(ILogger<RazorTextReplacer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Updates source code in Razor documents based on provider TextReplacements, accounting for Razor source code transition syntax.
        /// </summary>
        /// <param name="replacements">The text replacements to apply.</param>
        public void ApplyTextReplacements(IEnumerable<TextReplacement> replacements)
        {
            if (replacements is null)
            {
                throw new ArgumentNullException(nameof(replacements));
            }

            // Load each file with replacements into memory and update based on replacements
            var replacementsByFile = replacements.Distinct().OrderByDescending(t => t.StartingLine).GroupBy(t => t.FilePath);
            foreach (var replacementGroup in replacementsByFile)
            {
                var documentTextStr = File.ReadAllText(replacementGroup.Key);
                var lineOffsets = GetLineOffsets(documentTextStr).ToArray();
                var documentText = new StringBuilder(documentTextStr);

                foreach (var replacement in replacementGroup)
                {
                    _logger.LogInformation("Updating source code in Razor document {FilePath} at line {Line}", replacement.FilePath, replacement.StartingLine);

                    // If the original text doesn't fit in the lines of the original document, then the replacement is invalid
                    if (replacement.StartingLine + replacement.OriginalText.Lines.Count >= lineOffsets.Length)
                    {
                        continue;
                    }

                    // Start looking for replacements at the start of the indicated line
                    var startOffset = lineOffsets[replacement.StartingLine];

                    // Stop looking for replacements at the start of the first line after the indicated line plus the number of lines in the indicated text
                    var endOffset = lineOffsets[replacement.StartingLine + replacement.OriginalText.Lines.Count];

                    // Trim the string that's being replaced because code from Razor code blocks will include a couple extra spaces (to make room for @{)
                    // compared to the source that actually appeared in the cshtml file.
                    var originalText = replacement.OriginalText.ToString().TrimStart();
                    var updatedText = replacement.NewText.ToString().TrimStart();

                    // Generally, it's not necessary to minimize replacements since the text should be the same in both documents
                    // However, in the specific case of text being added before the first #line pragma in a Razor doc, it's possible
                    // that C# unrelated to the original document will show up with the replaced text, so minimize replacements only
                    // in the case that text is being inserted before the original first line.
                    if (replacement.StartingLine == 0)
                    {
                        MinimizeReplacement(ref originalText, ref updatedText);
                    }

                    // If the changed text ends with a semi-colon, trim it since the semi-colon won't appear in implicit Razor expressions
                    if (originalText.Trim().EndsWith(";", StringComparison.Ordinal) && updatedText.Trim().EndsWith(";", StringComparison.Ordinal))
                    {
                        originalText = originalText.Trim().Trim(';');
                        updatedText = updatedText.Trim().Trim(';');
                    }

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
                            endOffset = Math.Min(endOffset, documentText.Length);

                            documentText.Replace(explicitExpression, updatedText, startOffset, endOffset - startOffset);
                            endOffset = Math.Min(endOffset, documentText.Length);
                        }

                        documentText.Replace(originalText, updatedText, startOffset, endOffset - startOffset);
                    }
                }

                File.WriteAllText(replacementGroup.Key, documentText.ToString());
            }
        }

        private static IEnumerable<int> GetLineOffsets(string text)
        {
            // Pre-line 1
            yield return 0;

            // Line 1
            if (text.Any())
            {
                yield return 0;
            }

            // Subsequent lines
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    yield return i + 1;
                }
            }

            // EOF
            yield return text.Length;
        }

        // Removes leading and trailing portions of original and updated that are the same
        private static void MinimizeReplacement(ref string original, ref string updated)
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
