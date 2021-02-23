// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    public enum BuildBreakRisk
    {
        /// <summary>
        /// Unknown or inapplicable chance that a upgrade step will break a previously working build.
        /// </summary>
        Unknown,

        /// <summary>
        /// No chance that a upgrade step will break a previously working build.
        /// </summary>
        None,

        /// <summary>
        /// A low chance that a upgrade step will break a previously working build.
        /// </summary>
        Low,

        /// <summary>
        /// A medium chance that a upgrade step will break a previously working build.
        /// </summary>
        Medium,

        /// <summary>
        /// A high chance that a upgrade step will break a previously working build.
        /// </summary>
        High
    }
}
