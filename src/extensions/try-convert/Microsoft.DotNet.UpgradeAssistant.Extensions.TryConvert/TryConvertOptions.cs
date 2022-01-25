// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.TryConvert
{
    public class TryConvertOptions
    {
        [Required]
        public string ToolPath { get; set; } = null!;

        public string[] Arguments { get; set; } = Array.Empty<string>();
    }
}
