// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.DotNet.UpgradeAssistant.Portability
{
    public class PortabilityOptions
    {
        [Required]
        public Uri ServiceEndpoint { get; set; } = null!;
    }
}
