// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using DiffPlex;
using DiffPlex.Chunkers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// Service for correlating an original set of text sections from a document with an updated list of text sections when some sections may have been removed.
    /// </summary>
    public class DefaultTextMatcher : ITextMatcher
    {
        private readonly IChunker _chunker;
        private readonly IDiffer _differ;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTextMatcher"/> class.
        /// </summary>
        public DefaultTextMatcher()
        {
            _chunker = new CharacterChunker();
            _differ = new Differ();
        }

        /// <summary>
        /// Matches ordered text sections with updated ordered text sections and returns an enumerable of text replacements pairing original sub-texts with their corresponding new text.
        /// </summary>
        /// <param name="originalTexts">MappedSubTexts representing the original text sections to be updated. These should appear in the order they appear in the source document.</param>
        /// <param name="newTexts">New texts that should replace the original texts. These should also appear in the order they appear in the document but not all original texts need to have a corresponding new text.</param>
        /// <returns>An enumerable of text replacements mapping original texts with corresponding new text. Sub-texts that are unchanged are not included in the returned enumerable.</returns>
        public IEnumerable<TextReplacement> MatchOrderedSubTexts(IEnumerable<MappedSubText> originalTexts, IEnumerable<string> newTexts)
        {
            if (originalTexts is null)
            {
                throw new System.ArgumentNullException(nameof(originalTexts));
            }

            if (newTexts is null)
            {
                throw new System.ArgumentNullException(nameof(newTexts));
            }

            if (newTexts.Count() > originalTexts.Count())
            {
                throw new ArgumentException("originalTexts length must be greated than or equal to newTexts length. Sub-texts can be removed but not added.");
            }

            var replacements = new List<TextReplacement>();

            // We want to correlate the mapped code blocks in the original document with corresponding ones in the updated document.
            // Unfortunately, this is non-trivial because some code blocks may have been removed and, in other cases, multiple code blocks
            // can have the same source location.

            // If both groups have the same number of elements, then they pair in order
            if (originalTexts.Count() == newTexts.Count())
            {
                AddReplacementCandidates(replacements, originalTexts.Zip(newTexts, (original, updated) => new TextReplacement(original, SourceText.From(updated))));
            }

            // If there are no new texts, then the original elements all pair with empty source text
            else if (!newTexts.Any())
            {
                AddReplacementCandidates(replacements, originalTexts.Select(t => new TextReplacement(t, SourceText.From(string.Empty))));
            }

            // This is the tricky one. If there are less updated code blocks than original code blocks, it will be necesary to guess which original code blocks
            // pair with the remaining updated code blocks based on text similarity.
            else
            {
                var bestPairings = GetAllPossiblePairings(originalTexts, newTexts).OrderBy(GetTotalDiffSize).First();
                AddReplacementCandidates(replacements, bestPairings);
            }

            return replacements;
        }

        /// <summary>
        /// Adds text replacements to a list only if the replacement is not a no-op.
        /// </summary>
        private static void AddReplacementCandidates(List<TextReplacement> replacements, IEnumerable<TextReplacement> candidates)
        {
            replacements.AddRange(candidates.Where(c => !c.NewText.ContentEquals(c.OriginalText)));
        }

        /// <summary>
        /// Recursively enumerate all the possible ways the source and updated texts can match
        /// </summary>
        private static IEnumerable<IEnumerable<TextReplacement>> GetAllPossiblePairings(IEnumerable<MappedSubText> originalTexts, IEnumerable<string> newTexts)
        {
            if (originalTexts.Count() == newTexts.Count())
            {
                yield return originalTexts.Zip(newTexts, (original, updated) => new TextReplacement(original, SourceText.From(updated)));
            }
            else
            {
                for (var i = 0; i <= newTexts.Count(); i++)
                {
                    var newTextsList = newTexts.ToList();
                    newTextsList.Insert(i, string.Empty);
                    foreach (var candidate in GetAllPossiblePairings(originalTexts, newTextsList))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the total size of diff (sum or all inserts and deletes) between old and new texts in an enumerable of text replacements
        /// </summary>
        private int GetTotalDiffSize(IEnumerable<TextReplacement> replacements) =>
            replacements.Sum(r =>
            {
                var diff = _differ.CreateDiffs(r.OriginalText.ToString(), r.NewText.ToString(), true, false, _chunker);
                return diff.DiffBlocks.Any() ? diff.DiffBlocks.Select(b => b.DeleteCountA + b.InsertCountB).Sum() : 0;
            });
    }
}
