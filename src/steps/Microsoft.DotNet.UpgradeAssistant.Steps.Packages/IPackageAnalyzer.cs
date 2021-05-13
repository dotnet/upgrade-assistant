// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public interface IPackageAnalyzer
    {
        Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, IDependencyAnalysisState? analysisState, CancellationToken token);
    }
}
