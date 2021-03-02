// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// An abstraction used to make contextual decisions based on language.
    /// </summary>
    public enum Languages
    {
        /// <summary>
        /// The default. Used when the language has not been inspected, or cannot be determined.
        /// </summary>
        Unknown,

        /// <summary>
        /// Use for C#.
        /// </summary>
        CSharp,

        /// <summary>
        /// Use for F#.
        /// </summary>
        FSharp,

        /// <summary>
        /// Use for Visual Basic .NET.
        /// </summary>
        VisualBasic
    }
}
