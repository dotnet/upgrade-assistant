// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public record AnalyzeResultDef
    {
        public string AnalysisTypeName { get; init; }

        public IAsyncEnumerable<AnalyzeResult> AnalysisResults { get; init; }
    }
}
