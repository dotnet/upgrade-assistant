// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUpgradeContext : IDisposable
    {
        bool IsComplete { get; set; }

        IProject? EntryPoint { get; }

        void SetEntryPoint(IProject? entryPoint);

        IProject? CurrentProject { get; }

        void SetCurrentProject(IProject? project);

        IEnumerable<IProject> Projects { get; }

        bool UpdateSolution(Solution updatedSolution);

        IDictionary<string, string> GlobalProperties { get; }

        ValueTask ReloadWorkspaceAsync(CancellationToken token);
    }
}
