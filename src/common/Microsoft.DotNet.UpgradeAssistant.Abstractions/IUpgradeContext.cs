// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Telemetry;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IUpgradeContext : IDisposable
    {
        string? SolutionId { get; }

        string InputPath { get; }

        bool IsComplete { get; set; }

        IEnumerable<IProject> EntryPoints { get; set; }

        /// <summary>
        /// Gets or sets the current step. This property is intended for query operations only and will be updated by UpgraderManager. Changing this property will not change the control flow.
        /// </summary>
        UpgradeStep? CurrentStep { get; set; }

        IProject? CurrentProject { get; }

        void SetCurrentProject(IProject? project);

        IEnumerable<IProject> Projects { get; }

        IProject GetProject(string path);

        bool InputIsSolution { get; }

        bool UpdateSolution(Solution updatedSolution);

        IDictionary<string, string> GlobalProperties { get; }

        ValueTask ReloadWorkspaceAsync(CancellationToken token);

        IUpgradeContextProperties Properties { get; }

        ICollector<OutputResultDefinition> Results { get; }

        ITelemetry? Telemetry { get; }
    }
}
