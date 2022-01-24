// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IVersionComparer : IComparer<string>
    {
        bool TryFindBestVersion(IEnumerable<string> versions, [MaybeNullWhen(false)] out string bestMatch);

        bool IsMajorChange(string x, string y);
    }
}
