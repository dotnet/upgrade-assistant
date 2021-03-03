﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Provides ability to verify that the project is available to be upgraded. This allows known issues to be caught and fixed before attempting an upgrade.
    /// </summary>
    public interface IUpgradeReadyCheck
    {
        /// <summary>
        /// Gets an id that identifies this check.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Verifies that given the supplied project, an upgrade is possible.
        /// </summary>
        /// <param name="context">The project to be upgraded.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns><c>true</c> if the project is ready, <c>false</c> if it is not.</returns>
        Task<bool> IsReadyAsync(IProject context, CancellationToken token);
    }
}
