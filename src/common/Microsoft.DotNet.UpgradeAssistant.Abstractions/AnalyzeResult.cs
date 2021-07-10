// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record AnalyzeResult
    {
        public string? AnalysisName { get; set; }

        public string? AnalysisFileLocation { get; set; }

        public IReadOnlyCollection<string>? AnalysisResults { get; set; }
    }
}
