// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.UpgradeAssistant.Analysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUpgradeResultCollector
    {
        public void AddResult(AnalyzeResultDefinition result);

        public void AddResultCollection(IEnumerable<AnalyzeResultDefinition> resultCollection);

        public void Clear();

        public IEnumerable<AnalyzeResultDefinition> Results { get; }
    }
}
