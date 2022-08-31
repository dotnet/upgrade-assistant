// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant
{
    /// <summary>
    /// Definition for storing Analysis Results per type of Analysis.
    /// </summary>
    public record OutputResultDefinition
    {
        /// <summary>
        /// Gets version of Analysis Tool.
        /// </summary>
        public string Version { get; init; } = string.Empty;

        /// <summary>
        /// Gets the type of analysis being done.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets link to information for the analysis tool.
        /// </summary>
        public Uri InformationUri { get; init; } = new("about:blank");

        /// <summary>
        /// Gets results of the analysis type defined above.
        /// </summary>
        public IAsyncEnumerable<OutputResult> Results { get; init; } = AsyncEnumerable.Empty<OutputResult>();
    }
}
