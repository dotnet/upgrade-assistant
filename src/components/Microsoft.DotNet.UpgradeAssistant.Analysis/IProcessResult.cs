// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public interface IProcessResult
    {
        IList<Run> RunProcessResult(string toolName, Dictionary<string, IDependencyAnalysisState> analysisResults);
    }
}
