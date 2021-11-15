﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public record AnalyzeResult
    {
        public string RuleId { get; init; } = string.Empty;

        public string RuleName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the description of the analysis.
        /// </summary>
        public string FullDescription { get; init; } = string.Empty;

        /// <summary>
        /// Gets the direct link to the documentation.
        /// </summary>
        public Uri HelpUri { get; init; } = new("about:blank");

        public int LineNumber { get; init; }

        public string FileLocation { get; init; } = string.Empty;

        public string ResultMessage { get; init; } = string.Empty;
    }
}
