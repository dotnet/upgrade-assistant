// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Used by IUpgradeReadyCheck instances to declare whether or not upgrade-assistant can upgrade a project.
    /// </summary>
    public enum UpgradeReadiness
    {
        /// <summary>
        /// Upgrade-assistant is ready to help upgrade this project.
        /// </summary>
        Ready,

        /// <summary>
        /// Upgrade-assistant can help with some of the tasks required to upgrade this project.
        /// But users should be informed that upgrade-assistant cannot completely upgrade this project.
        /// </summary>
        Unsupported,

        /// <summary>
        /// This project contains something that prevents upgrade.
        /// </summary>
        NotReady
    }
}
