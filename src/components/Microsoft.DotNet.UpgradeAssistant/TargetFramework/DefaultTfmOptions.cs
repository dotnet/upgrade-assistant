// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class DefaultTfmOptions
    {
        [Required]
        public string Current { get; set; } = null!;

        [Required]
        public string LTS { get; set; } = null!;

        [Required]
        public string Preview { get; set; } = null!;

        public UpgradeTarget TargetTfmSupport { get; set; } = UpgradeTarget.Current;

        public string DetermineTargetTfmValue() => this.TargetTfmSupport switch
        {
            UpgradeTarget.Current => this.Current,
            UpgradeTarget.Preview => this.Preview,
            UpgradeTarget.LTS => this.LTS,
            _ => this.LTS,
        };
    }
}
