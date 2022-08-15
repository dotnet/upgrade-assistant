// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.DotNet.UpgradeAssistant.Analysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    internal class UpgradeResultCollector : IUpgradeResultCollector
    {
        private List<AnalyzeResultDefinition> _resultCollection = new List<AnalyzeResultDefinition>();

        public void AddResult(AnalyzeResultDefinition result)
        {
            _resultCollection.Add(result);
        }

        public void AddResultCollection(IEnumerable<AnalyzeResultDefinition> resultCollection)
        {
            _resultCollection.AddRange(resultCollection);
        }

        public void Clear()
        {
            _resultCollection.Clear();
        }

        public IEnumerable<AnalyzeResultDefinition> Results => _resultCollection.ToImmutableList();
    }
}
