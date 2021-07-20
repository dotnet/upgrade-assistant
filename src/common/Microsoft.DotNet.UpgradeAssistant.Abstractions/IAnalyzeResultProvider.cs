// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public interface IAnalyzeResultProvider
    {
        string Id { get; }

        string Name { get; }

        Uri InformationURI { get; }

        IAsyncEnumerable<AnalyzeResult> AnalyzeAsync(AnalyzeContext analysis, CancellationToken token);
    }
}
