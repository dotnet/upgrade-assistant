// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public class TextReplacement
    {
        public string NewText { get; }

        public int Offset { get; }

        public int OldLength { get; }

        public TextReplacement(string newText, int offset, int oldLength)
        {
            NewText = newText ?? throw new ArgumentNullException(nameof(newText));
            Offset = offset;
            OldLength = oldLength;
        }

        public virtual void Apply(StringBuilder input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length < Offset + OldLength)
            {
                throw new InvalidOperationException($"Input string (length {input.Length}) is too short to replace {OldLength} characters beginnin at offset {Offset}");
            }

            input.Remove(Offset, OldLength);
            input.Insert(Offset, NewText);
        }
    }
}
