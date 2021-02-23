// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    public enum UpgradeStepStatus
    {
        /// <summary>
        /// A upgrade step in an unknown state, possibly uninitialized.
        /// </summary>
        Unknown,

        /// <summary>
        /// A upgrade step that is initialized but has not completed.
        /// </summary>
        Incomplete,

        /// <summary>
        /// A upgrade step that has run successfully.
        /// </summary>
        Complete,

        /// <summary>
        /// A upgrade step that was initialized and then skipped.
        /// </summary>
        Skipped,

        /// <summary>
        /// A upgrade step that ran and failed while running.
        /// </summary>
        Failed
    }
}
