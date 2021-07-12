﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.UpgradeAssistant.Dependencies;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class AnalyzeContext
    {
        public AnalyzeContext(IUpgradeContext context)
        {
            UpgradeContext = context;
            Dependencies = new Dictionary<string, IDependencyAnalysisState>();
        }

        public IUpgradeContext UpgradeContext { get; }

        public Dictionary<string, IDependencyAnalysisState> Dependencies { get; }
    }
}
