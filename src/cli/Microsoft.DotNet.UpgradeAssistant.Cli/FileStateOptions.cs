// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class FileStateOptions
    {
        [Required]
        public string Path { get; set; } = null!;
    }
}
