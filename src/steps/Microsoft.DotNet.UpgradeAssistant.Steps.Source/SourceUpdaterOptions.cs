// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Source
{
    public class SourceUpdaterOptions : IFileOption
    {
        public string[] AdditionalAnalyzerTexts { get; set; } = Array.Empty<string>();

        public IFileProvider Files { get; set; } = null!;
    }
}
