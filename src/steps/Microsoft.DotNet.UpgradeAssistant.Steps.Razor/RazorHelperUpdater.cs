// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// An updater for RazorCodeDocuments that updates @helper syntax to create local methods, instead.
    /// </summary>
    public class RazorHelperUpdater : IUpdater<RazorCodeDocument>
    {
        private const string HelperResultNamespace = "Microsoft.AspNetCore.Mvc.Razor";

        // Regex used to identify @helper functions
        private static readonly Regex HelperDeclRegex = new Regex(@"@helper\s+.*?\(.*?\)", RegexOptions.Compiled);

        private readonly ILogger<RazorHelperUpdater> _logger;

        /// <summary>
        /// Gets an identifier unique to this updater type.
        /// </summary>
        public string Id => typeof(RazorHelperUpdater).FullName;

        /// <summary>
        /// Gets this updater's title.
        /// </summary>
        public string Title => "Replace @helper syntax in Razor files";

        /// <summary>
        /// Gets this updater's description.
        /// </summary>
        public string Description => "Update Razor documents to use local methods instead of @helper functions";

        /// <summary>
        /// Gets the risk that applying this updater will introduce build breaks in the updated project. For the RazorHelperUpdater, the risk
        /// is low because the changes to the Razor documents should be compatible with ASP.NET Core, but it is not 'none' because source is
        /// being updated.
        /// </summary>
        public BuildBreakRisk Risk => BuildBreakRisk.Low;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorHelperUpdater"/> class.
        /// </summary>
        /// <param name="logger">The ILogger to use for logging diagnostic messages.</param>
        public RazorHelperUpdater(ILogger<RazorHelperUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Determines whether this updater needs to run on a given set of RazorCodeDocuments. Will return true if there are @helper functions
        /// in any of the Razor documents and false otherwise.
        /// </summary>
        /// <param name="context">The upgrade context for the currently upgrading solution.</param>
        /// <param name="inputs">The Razor code documents being upgraded.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A FileUpdaterResult with a 'true' value if any documents contain a @helper and false otherwise. If true, the result will also include the paths of the documents containing a @helper.</returns>
        public async Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var inputsWithHelpers = new List<string>();
            foreach (var input in inputs)
            {
                // Check whether any of the documents contain a @helper function
                if (await HasHelperAsync(input).ConfigureAwait(false))
                {
                    // If a @helper is present, log it to the console and store the path to return later
                    var path = input.Source.FilePath;
                    _logger.LogInformation("Razor document {FilePath} contains a @helper function", path);
                    inputsWithHelpers.Add(path);
                }
            }

            // Log how many Razor documents had @helpers and return a corresponding FileUpdaterResult
            _logger.LogInformation("Found @helper functions in {Count} documents", inputsWithHelpers.Count);
            return new FileUpdaterResult(inputsWithHelpers.Any(), inputsWithHelpers);
        }

        /// <summary>
        /// Upgrades a set of RazorCodeDocuments to replace @helper functions with local methods.
        /// </summary>
        /// <param name="context">The upgrade context being upgraded.</param>
        /// <param name="inputs">The Razor code documents to upgrade.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A FileUpdaterResult indicating whether the opertaion succeeded and which documents (if any) were updated.</returns>
        public async Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Track which documents are updated for inclusion in the updater result returned later
            var updatedDocuments = new List<string>();
            foreach (var document in inputs)
            {
                // Unfortunately, there's no good way to walk or update Razor syntax trees, so
                // the Razor document contents are primarily treated as a string.
                var path = document.Source.FilePath;
                string? text = null;
                using (var reader = new StreamReader(path))
                {
                    text = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (text is null)
                {
                    _logger.LogWarning("Failed to read Razor document {FilePath}", path);
                    continue;
                }

                var helperReplacements = await GetHelperReplacementsAsync(text, token).ConfigureAwait(false);

                if (helperReplacements.Any())
                {
                    // If the document contains a @helper, construct the new document text by removing the replacement's old text
                    // and inserting the new text in its place.
                    updatedDocuments.Add(path);
                    var newText = new StringBuilder(text);
                    _logger.LogInformation("Replacing {HelperCount} @helper functions in {FilePath}", helperReplacements.Count(), path);
                    foreach (var helper in helperReplacements.OrderByDescending(h => h.Offset))
                    {
                        newText.Remove(helper.Offset, helper.OldLength);
                        newText.Insert(helper.Offset, helper.NewText);
                    }

                    // Check whether the document has a using statement for HelperResult's namespace (Microsoft.AspNetCore.Mvc.Razor) and
                    // add one if none is present. Again, this is done with string manipulation since there's no good API for updating
                    // Razor syntax.
                    if (!HasMvcRazorUsing(document))
                    {
                        _logger.LogInformation("Adding an import statement for the '{namespace}' namespace to {FilePath}", HelperResultNamespace, path);
                        newText.Insert(0, $"@using {HelperResultNamespace}{Environment.NewLine}");
                    }

                    _logger.LogDebug("Writing updates to {FilePath} to replace @helper functions", path);
                    File.WriteAllText(path, newText.ToString());
                }
                else
                {
                    _logger.LogTrace("No @helper functions found in {FilePath}", path);
                }
            }

            return new FileUpdaterResult(true, updatedDocuments);
        }

        private static bool HasMvcRazorUsing(RazorCodeDocument document)
        {
            // Check for the using statement in the C# code document (rather than in the Razor text)
            // since the import could come from a ViewImports file.
            var code = document.GetCSharpDocument().GeneratedCode;
            return code.Contains($"using {HelperResultNamespace}");
        }

        private static async Task<bool> HasHelperAsync(RazorCodeDocument document)
        {
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
                    return true;
                }

                line = await reader.ReadLineAsync().ConfigureAwait(false);
            }

            return false;
        }

        private static async Task<IEnumerable<HelperReplacement>> GetHelperReplacementsAsync(string text, CancellationToken token)
        {
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
            StringBuilder newText = new StringBuilder("@{ HelperResult ");

            var functionBodyBegun = false;
            var bracketCount = 0;
            var index = helperOffset + "@helper ".Length;
            var lineStart = index;

            // Copy the helper function until the end of its body block
            while ((!functionBodyBegun || bracketCount > 0) && index < text.Length)
            {
                var nextChar = text[index];
                bracketCount += nextChar switch
                {
                    '{' => 1,
                    '}' => -1,
                    _ => 0
                };

                if (!functionBodyBegun && bracketCount > 0)
                {
                    functionBodyBegun = true;
                }

                newText.Append(nextChar);

                if (nextChar == '\n')
                {
                    lineStart = index + 1;
                    newText.Append('\t');
                }

                index++;
            }

            // Figure out how far the helper's text is indented by looking at the start of the last line
            int whitespaceCount;
            for (whitespaceCount = 0; lineStart + whitespaceCount < text.Length && char.IsWhiteSpace(text[lineStart + whitespaceCount]); whitespaceCount++) ;
            var whitespace = text.Substring(lineStart, whitespaceCount);

            // Add a return statement for the local method
            newText.Insert(newText.Length - 1, $"\treturn new HelperResult(w => Task.CompletedTask);{Environment.NewLine}{whitespace}");

            newText.AppendLine(" }");

            return new HelperReplacement(newText.ToString(), helperOffset, index - helperOffset);
        }

        private record HelperReplacement(string NewText, int Offset, int OldLength);
    }
}
