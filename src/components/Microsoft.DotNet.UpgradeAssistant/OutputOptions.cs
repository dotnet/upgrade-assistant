// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis
{
    public class OutputOptions
    {
        [Required]
        public string Format { get; set; } = null!;
    }
}
