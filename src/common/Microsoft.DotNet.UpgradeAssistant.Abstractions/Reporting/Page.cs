// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Reporting
{
    public record Page(string Title)
    {
        public IReadOnlyCollection<Content> Content { get; init; } = Array.Empty<Content>();
    }
}
