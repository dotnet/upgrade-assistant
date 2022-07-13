// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.Analysis
{
    public record ApiResult
    {
        public ApiResult(ApiModel api,
                         IReadOnlyList<FrameworkResult> frameworkResults)
        {
            ArgumentNullException.ThrowIfNull(frameworkResults);

            Api = api;
            FrameworkResults = frameworkResults;
        }

        public ApiModel Api { get; }

        public IReadOnlyList<FrameworkResult> FrameworkResults { get; }

        public bool IsRelevant() => FrameworkResults.Any(fx => fx.IsRelevant());
    }
}
