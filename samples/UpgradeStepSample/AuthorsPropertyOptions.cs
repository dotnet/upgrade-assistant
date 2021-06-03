// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace UpgradeStepSample
{
    /// <summary>
    /// Configuration options for the AuthorsPropertyUpgradeStep.
    /// Will be read from extension configuration by
    /// AuthorsPropertyUpgradeStep's constructor.
    /// </summary>
    public class AuthorsPropertyOptions
    {
        /// <summary>
        /// Gets or sets the authors to include in the Authors property.
        /// </summary>
        public string? Authors { get; set; }
    }
}
