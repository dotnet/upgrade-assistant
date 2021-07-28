// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public record AnalyzeResult
    {
        public string FileLocation { get; init; } = string.Empty;

        public HashSet<string> Results { get; init; } = new HashSet<string>();
    }
}
