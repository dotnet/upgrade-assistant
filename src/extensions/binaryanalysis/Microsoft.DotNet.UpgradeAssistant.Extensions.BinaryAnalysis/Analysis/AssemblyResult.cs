// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.Analysis
{
    public sealed class AssemblyResult
    {
        public AssemblyResult(string assemblyName,
                              string? assemblyIssues,
                              IReadOnlyList<ApiResult> apis)
        {
            AssemblyName = assemblyName;
            AssemblyIssues = assemblyIssues;
            Apis = apis;
        }

        public string AssemblyName { get; }

        public string? AssemblyIssues { get; }

        public IReadOnlyList<ApiResult> Apis { get; }
    }
}
