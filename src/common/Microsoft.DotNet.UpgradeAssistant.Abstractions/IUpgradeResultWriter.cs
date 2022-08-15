// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Analysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUpgradeResultWriter
    {
        public void AddWriteDestination(Stream stream, string format);

        Task WriteAsync(IAsyncEnumerable<AnalyzeResultDefinition> results, CancellationToken token);
    }
}
