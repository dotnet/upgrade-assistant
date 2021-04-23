// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    public interface ITextReplacer
    {
        void ApplyTextReplacements(IList<TextReplacement> replacements);
    }
}