// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public interface IAnalyzeResultProvider
    {
        string ToolName { get; }

        Uri InformationURI { get; }

        Task<bool> IsApplicableAsync(AnalyzeContext analysis, CancellationToken token);

        IAsyncEnumerable<AnalyzeResult> AnalyzeAsync(AnalyzeContext analysis, CancellationToken token);
    }
}
