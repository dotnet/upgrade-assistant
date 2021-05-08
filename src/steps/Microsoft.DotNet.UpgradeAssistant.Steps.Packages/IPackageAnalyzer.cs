// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Packages
{
    public interface IPackageAnalyzer
    {
        public Task<bool> RunPackageAnalyzersAsync(IUpgradeContext context, PackageAnalysisState? analysisState, CancellationToken token);
    }
}
