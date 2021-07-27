// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record UpgradeAssistantConfiguration
    {
        public ImmutableArray<ExtensionSource> Extensions { get; init; } = ImmutableArray<ExtensionSource>.Empty;
    }
}
