// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// Service for correlating an original set of text sections from a document with an updated list of text sections when some sections may have been removed.
    /// </summary>
    public interface ITextMatcher
    {
        /// <summary>
        /// Matches ordered text sections with updated ordered text sections and returns an enumerable of text replacements pairing original sub-texts with their corresponding new text.
        /// </summary>
        /// <param name="originalTexts">MappedSubTexts representing the original text sections to be updated. These should appear in the order they appear in the source document.</param>
        /// <param name="newTexts">New texts that should replace the original texts. These should also appear in the order they appear in the document but not all original texts need to have a corresponding new text.</param>
        /// <returns>An enumerable of text replacements mapping original texts with corresponding new text.</returns>
        IEnumerable<MappedTextReplacement> MatchOrderedSubTexts(IEnumerable<MappedSubText> originalTexts, IEnumerable<string> newTexts);
    }
}
