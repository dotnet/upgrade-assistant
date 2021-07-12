// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    /// <summary>
    /// Definition for storing Analysis Results per type of Analysis.
    /// </summary>
    public record AnalyzeResultDefinition
    {
        /// <summary>
        /// Defined the type of analysis being done.
        /// </summary>
        public string AnalysisTypeName { get; init; } = string.Empty;

        /// <summary>
        /// Results of the analysis type defined above.
        /// </summary>
        public IAsyncEnumerable<AnalyzeResult> AnalysisResults { get; init; } = AsyncEnumerable.Empty<AnalyzeResult>();
    }
}
