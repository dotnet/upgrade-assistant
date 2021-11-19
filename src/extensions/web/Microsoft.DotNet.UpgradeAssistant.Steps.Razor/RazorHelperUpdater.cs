// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly IHelperMatcher _helperMatcher;
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
        /// <param name="helperMatcher">The matcher to use for finding (and proposing replacements for) @helper functions.</param>
        /// <param name="logger">The ILogger to use for logging diagnostic messages.</param>
        public RazorHelperUpdater(IHelperMatcher helperMatcher, ILogger<RazorHelperUpdater> logger)
        {
            _helperMatcher = helperMatcher ?? throw new ArgumentNullException(nameof(helperMatcher));
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
        public Task<IUpdaterResult> IsApplicableAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var inputsWithHelpers = new List<string>();
            foreach (var input in inputs)
            {
                // Check whether any of the documents contain a @helper function
                if (_helperMatcher.GetHelperReplacements(input).Any())
                {
                    // If a @helper is present, log it to the console and store the path to return later
                    var path = input.Source.FilePath;
                    inputsWithHelpers.Add(path);
                }
            }

            // Log how many Razor documents had @helpers and return a corresponding FileUpdaterResult
            _logger.LogInformation("Found @helper functions in {Count} documents", inputsWithHelpers.Count);
            return Task.FromResult<IUpdaterResult>(new FileUpdaterResult(
                RuleId: "Id",
                RuleName: Id,
                FullDescription: Title,
                inputsWithHelpers.Any(), inputsWithHelpers));
        }

        /// <summary>
        /// Upgrades a set of RazorCodeDocuments to replace @helper functions with local methods.
        /// </summary>
        /// <param name="context">The upgrade context being upgraded.</param>
        /// <param name="inputs">The Razor code documents to upgrade.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A FileUpdaterResult indicating whether the opertaion succeeded and which documents (if any) were updated.</returns>
        public Task<IUpdaterResult> ApplyAsync(IUpgradeContext context, ImmutableArray<RazorCodeDocument> inputs, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Track which documents are updated for inclusion in the updater result returned later
            var updatedDocuments = new List<string>();
            foreach (var document in inputs)
            {
                var path = document.Source.FilePath;

                if (path is null || !File.Exists(path))
                {
                    _logger.LogWarning("Could not find expected Razor document at {Path}", path);
                    continue;
                }

                var helperReplacements = _helperMatcher.GetHelperReplacements(document);

                if (helperReplacements.Any())
                {
                    // If the document contains a @helper, construct the new document text by removing the replacement's old text
                    // and inserting the new text in its place.
                    updatedDocuments.Add(path);
                    var newText = new StringBuilder(File.ReadAllText(path));
                    _logger.LogInformation("Replacing {HelperCount} @helper functions in {FilePath}", helperReplacements.Count(), path);
                    foreach (var helper in helperReplacements.OrderByDescending(h => h.Offset))
                    {
                        helper.Apply(newText);
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

            return Task.FromResult<IUpdaterResult>(new FileUpdaterResult(
                RuleId: "Id",
                RuleName: Id,
                FullDescription: Title,
                true,
                updatedDocuments));
        }

        private static bool HasMvcRazorUsing(RazorCodeDocument document)
        {
            // Check for the using statement in the C# code document (rather than in the Razor text)
            // since the import could come from a ViewImports file.
            var code = document.GetCSharpDocument().GeneratedCode;
            return code.Contains($"using {HelperResultNamespace}");
        }
    }
}
