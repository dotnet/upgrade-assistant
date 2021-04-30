// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Provides access to the MSBuildPath. This value is set by MSBuildRegistrationStartup.
    /// </summary>
    public class MSBuildPathLocator
    {
        public string? MSBuildPath { get; set; }
    }
}
