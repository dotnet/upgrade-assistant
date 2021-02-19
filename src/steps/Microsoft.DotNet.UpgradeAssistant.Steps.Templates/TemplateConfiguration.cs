﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateConfiguration
    {
        public Dictionary<string, string>? Replacements { get; set; }

        public ItemSpec[]? TemplateItems { get; set; }

        public bool UpdateWebAppsOnly { get; set; }
    }
}
