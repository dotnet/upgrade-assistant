// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public record AnalyzeResult
    {
        public ICollection<ResultObj> Results { get; init; } = new HashSet<ResultObj>();
    }

    public record ResultObj
    {
        public int LineNumber { get; init; }

        public string FileLocation { get; init; } = string.Empty;

        public string ResultMessage { get; init; } = string.Empty;
    }
}
