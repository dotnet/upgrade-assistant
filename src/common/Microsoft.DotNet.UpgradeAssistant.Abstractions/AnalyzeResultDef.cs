// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public record AnalyzeResultDef
    {
        public string AnalysisTypeName { get; init; } = string.Empty;

        public IAsyncEnumerable<AnalyzeResult> AnalysisResults { get; init; } = AsyncEnumerable.Empty<AnalyzeResult>();
    }
}
