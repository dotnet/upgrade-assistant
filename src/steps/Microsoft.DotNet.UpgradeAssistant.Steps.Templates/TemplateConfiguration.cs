// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateConfiguration
    {
    // This is a false positive
#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, string>? Replacements { get; init; }
#pragma warning restore CA2227 // Collection properties should be read only

        public ItemSpec[]? TemplateItems { get; set; }

        public bool UpdateWebAppsOnly { get; set; }
    }
}
