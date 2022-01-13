// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public interface IAnalyzeResultWriter
    {
        Task WriteAsync(IAsyncEnumerable<AnalyzeResultDefinition> results, string? format, CancellationToken token);

        string GetFormat();
    }
}
