// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public class TextReplacement: IEquatable<TextReplacement>
    {
        public MappedSubText OriginalText { get; }

        public SourceText NewText { get; }

        public TextReplacement(MappedSubText originalText, SourceText newText)
        {
            OriginalText = originalText ?? throw new ArgumentNullException(nameof(originalText));
            NewText = newText ?? throw new ArgumentNullException(nameof(newText));
        }

        public bool Equals(TextReplacement other) =>
            other != null &&
            NewText.ContentEquals(other.NewText) &&
            OriginalText.Equals(other.OriginalText);

        public override bool Equals(object obj) => Equals(obj as TextReplacement);

        public override int GetHashCode()
        {
            // https://stackoverflow.com/a/1646913
            var hash = 17;
            hash = (hash * 31) + NewText.ToString().GetHashCode();
            hash = (hash * 31) + OriginalText.GetHashCode();
            return hash;
        }
    }
}
