// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private const string HelperDeclSyntax = @"@helper ";

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
        /// Creates HelperReplacements for each @helper function in a Razor document.
        /// </summary>
        /// <param name="document">The Razor document to find @helper functions in.</param>
        /// <returns>HelperReplacements proposing replacements of each @helper with a local method.</returns>
        public IEnumerable<TextReplacement> GetHelperReplacements(RazorCodeDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            // Unfortunately, there's no good way to walk or update Razor syntax trees, so
            // the Razor document contents are primarily treated as a string.
            var path = document.Source.FilePath;
            using var reader = new StreamReader(path);

            // Iterate through the document one character at a time looking for @helper functions.
            var matchCount = 0;
            var nextCharResult = reader.Read();
            var index = 1;
            while (nextCharResult >= 0)
            {
                var nextChar = (char)nextCharResult;
                if (nextChar == HelperDeclSyntax[matchCount])
                {
                    matchCount++;
                    if (matchCount == HelperDeclSyntax.Length)
                    {
                        matchCount = 0;
                        var replacement = GetHelperReplacement(reader, ref index);
                        if (replacement is null)
                        {
                            _logger.LogWarning("Invalid @helper function found in {Path}; not replacing", path);
                        }
                        else
                        {
                            yield return replacement;
                        }
                    }
                }
                else
                {
                    matchCount = 0;
                }

                nextCharResult = reader.Read();
                index++;
            }
        }

        private static TextReplacement? GetHelperReplacement(StreamReader reader, ref int index)
        {
            var newText = new StringBuilder("@{ HelperResult ");
            var functionBodyBegun = false;
            var braceCount = 0;
            var helperStart = index - HelperDeclSyntax.Length;

            // Copy the helper function until the end of its body block
            var nextCharResult = reader.Read();
            index++;
            while (nextCharResult != -1)
            {
                var nextChar = (char)nextCharResult;

                // Match braces while moving through the function body so that
                // the loop can end when all braces are matched
                braceCount += nextChar switch
                {
                    '{' => 1,
                    '}' => -1,
                    _ => 0
                };

#pragma warning disable CA1508 // Avoid dead conditional code
                if (functionBodyBegun && braceCount == 0)
#pragma warning restore CA1508 // Avoid dead conditional code
                {
                    // If we've reached the final brace, add a return statement for the local method,
                    // and return the text replacement.
                    newText.Append($"\treturn new HelperResult(w => Task.CompletedTask);\r\n}} }}");

                    return new TextReplacement(newText.ToString(), helperStart, index - helperStart);
                }

                if (!functionBodyBegun && braceCount > 0)
                {
                    functionBodyBegun = true;
                }

                newText.Append(nextChar);

                // Indent each new line one extra tab to account for the additional @{ wrapper
                // introduced by the replacement
                if (nextChar == '\n')
                {
                    newText.Append('\t');
                }

                nextCharResult = reader.Read();
                index++;
            }

            return null;
        }
    }
}
