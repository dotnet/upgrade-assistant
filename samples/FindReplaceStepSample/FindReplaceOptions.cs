// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace FindReplaceStep
{
    /// <summary>
    /// Configuration options for the FindReplaceUpgradeStep.
    /// Will be read from extension configuration by
    /// FindReplaceUpgradeStep's constructor.
    /// </summary>
    public class FindReplaceOptions
    {
        /// <summary>
        /// Gets or sets the strings to be replaced by the FindReplaceUpgradeStep.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, string>? Replacements { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
