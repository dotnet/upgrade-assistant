// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.DotNet.UpgradeAssistant.Analysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IBinaryAnalysisExecutor
    {
        Task RunAsync(Func<OutputResult, Task> receiver);
    }
}
