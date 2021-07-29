// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions
{
    public record ExtensionSource(string Name)
    {
        public string Source { get; init; } = null!;

        public string? Version { get; init; }
    }
}
