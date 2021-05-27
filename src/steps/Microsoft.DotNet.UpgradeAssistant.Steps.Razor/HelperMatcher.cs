// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// Determines whether a Razor document contains @helper functions
    /// and produces local method replacements for the @helpers.
    /// </summary>
    public class HelperMatcher : IHelperMatcher
    {
        // Regex used to identify @helper functions
        private static readonly Regex HelperDeclRegex = new Regex(@"@helper\s+.*?\(.*?\)", RegexOptions.Compiled);

        private readonly ILogger<HelperMatcher> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelperMatcher"/> class.
        /// </summary>
        /// <param name="logger">An ILogger for logging diagnostic messages.</param>
        public HelperMatcher(ILogger<HelperMatcher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Determines whether a RazorCodeDocument contains a @helper function.
        /// </summary>
        /// <param name="document">The Razor document to inspect.</param>
        /// <returns>True if the document contains a @helper, false otherwise.</returns>
        public async Task<bool> HasHelperAsync(RazorCodeDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            // Checking for '@helper' in the document is non-trivial becausae
            // ASP.NET Core's Razor engine doesn't understand the @helper syntax.
            //
            // Because of that, @helper functions show up in intermediate nodes and
            // syntax trees as C# expressions and the function body following is just
            // treated as HTML rather than a code block.
            //
            // Because of these challenges (along with the lack of a public API for
            // walking Razor syntax trees), it's simplest to just search the text of
            // the document
            using var reader = new StreamReader(document.Source.FilePath);
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            while (line is not null)
            {
                // Helper declarations need to be on a single line, so it's safe
                // to evaluate the regex on individual lines.
                if (HelperDeclRegex.IsMatch(line))
                {
                    _logger.LogInformation("Razor document {FilePath} contains a @helper function", document.Source.FilePath);
                    return true;
                }

                line = await reader.ReadLineAsync().ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        /// Creates HelperReplacements for each @helper function in a Razor document.
        /// </summary>
        /// <param name="document">The Razor document to find @helper functions in.</param>
        /// <returns>HelperReplacements proposing replacements of each @helper with a local method.</returns>
        public async Task<IEnumerable<HelperReplacement>> GetHelperReplacementsAsync(RazorCodeDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            // Unfortunately, there's no good way to walk or update Razor syntax trees, so
            // the Razor document contents are primarily treated as a string.
            var path = document.Source.FilePath;
            using var reader = new StreamReader(path);
            var text = await reader.ReadToEndAsync().ConfigureAwait(false);

            if (text is null)
            {
                _logger.LogWarning("Failed to read Razor document {FilePath}", path);
                return Enumerable.Empty<HelperReplacement>();
            }

            var helpers = HelperDeclRegex.Matches(text);

            // This would be a lot more succinct with Linq,
            // but MatchCollection doesn't implement IEnumerable<Match>.
            var replacements = new HelperReplacement[helpers.Count];
            for (var i = 0; i < helpers.Count; i++)
            {
                replacements[i] = GetHelperReplacement(text, helpers[i].Index);
            }

            return replacements;
        }

        private static HelperReplacement GetHelperReplacement(string text, int helperOffset)
        {
            var newText = new StringBuilder("@{ HelperResult ");
            var functionBodyBegun = false;
            var braceCount = 0;
            var index = helperOffset + "@helper ".Length;
            var lineStart = index;

            // Copy the helper function until the end of its body block
            while ((!functionBodyBegun || braceCount > 0) && index < text.Length)
            {
                var nextChar = text[index];

                // Match braces while moving through the function body so that
                // the loop can end when all braces are matched
                braceCount += nextChar switch
                {
                    '{' => 1,
                    '}' => -1,
                    _ => 0
                };

                if (!functionBodyBegun && braceCount > 0)
                {
                    functionBodyBegun = true;
                }

                newText.Append(nextChar);

                // Indent each new line one extra tab to account for the additional @{ wrapper
                // introduced by the replacement
                if (nextChar == '\n')
                {
                    lineStart = index + 1;
                    newText.Append('\t');
                }

                index++;
            }

            // Figure out how far the helper's text is indented by looking at the start of the last line
            int whitespaceCount;
            for (whitespaceCount = 0; lineStart + whitespaceCount < text.Length && char.IsWhiteSpace(text[lineStart + whitespaceCount]); whitespaceCount++)
            {
                // Intentionally empty; this is just walking to the end of the whitespace
            }

            var whitespace = text.Substring(lineStart, whitespaceCount);

            // Add a return statement for the local method
            newText.Insert(newText.Length - 1, $"\treturn new HelperResult(w => Task.CompletedTask);{Environment.NewLine}{whitespace}");

            newText.Append(" }");

            return new HelperReplacement(newText.ToString(), helperOffset, index - helperOffset);
        }
    }
}
