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
        string InputPath { get; }

        bool IsComplete { get; set; }

        IEnumerable<IProject> EntryPoints { get; set; }

        IProject? CurrentProject { get; }

        void SetCurrentProject(IProject? project);

        IEnumerable<IProject> Projects { get; }

        bool InputIsSolution { get; }

        bool UpdateSolution(Solution updatedSolution);

        IDictionary<string, string> GlobalProperties { get; }

        ValueTask ReloadWorkspaceAsync(CancellationToken token);

        /// <summary>
        /// Returns a property value stored in the context.
        /// </summary>
        /// <param name="propertyName">Name identifier for the property.</param>
        /// <returns>The property value.</returns>
        string? TryGetPropertyValue(string propertyName);

        /// <summary>
        /// Stores a property value to the context.
        /// </summary>
        /// <param name="propertyName">Name identifier for the property.</param>
        /// <param name="value">The property value to store.</param>
        /// <param name="persistent">Whether to persist the value when the program terminates.</param>
        void SetPropertyValue(string propertyName, string value, bool persistent);

        /// <summary>
        /// Returns persistent properties for storage.
        /// </summary>
        /// <returns>All defined persistent properties.</returns>
        Dictionary<string, string> GetPersistentProperties();
    }
}
