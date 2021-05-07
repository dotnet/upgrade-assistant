// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor.Tests
{
    public record LocationLookup(string Path, string? Keyword, int StartOffset = 0, int EndOffset = 0);
}
