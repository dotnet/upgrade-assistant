// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IAnalyzeResultProvider
    {
        [ObsoleteAttribute("This property is WIP, expect changes in this area.", false)]
        Task AnalyzeAsync(AnalyzeContext analysis, CancellationToken token);
    }
}
