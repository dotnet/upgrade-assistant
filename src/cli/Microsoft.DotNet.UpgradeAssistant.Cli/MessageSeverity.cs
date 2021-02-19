// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    /// <summary>
    /// Gives a suggestion to the calling code about how to format a message displayed to the user.
    /// </summary>
    public enum MessageSeverity
    {
        /// <summary>
        /// No special format recommended.
        /// </summary>
        None,

        /// <summary>
        /// Recommending this be subtly emphasized as important and a non-issue.
        /// </summary>
        Info,

        /// <summary>
        /// Recommend this be emphasized as something the user should be concerned about.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicate something has gone wrong and the user should be alerted.
        /// </summary>
        Error
    }
}
