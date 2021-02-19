// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant.Portability
{
    /// <summary>
    /// An interface to define issues with portability. This may pull data from the Portability service, as well as config files, etc.
    /// </summary>
    internal interface IPortabilityAnalyzer
    {
        IAsyncEnumerable<PortabilityResult> Analyze(Compilation compilation, CancellationToken token);
    }
}
