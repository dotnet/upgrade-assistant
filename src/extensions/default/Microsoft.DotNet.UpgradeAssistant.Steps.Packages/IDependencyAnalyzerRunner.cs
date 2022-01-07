// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public interface IDependencyAnalyzerRunner
    {
        Task<IDependencyAnalysisState> AnalyzeAsync(IUpgradeContext context, IProject? projectRoot, IEnumerable<IDependencyAnalyzer> analyzers, IReadOnlyCollection<TargetFrameworkMoniker> targetframeworks, CancellationToken token);
    }
}
