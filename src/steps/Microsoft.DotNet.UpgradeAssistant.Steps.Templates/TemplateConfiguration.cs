// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateConfiguration
    {
        public Dictionary<string, string>? Replacements { get; init; }

        public ItemSpec[]? TemplateItems { get; set; }

        public ProjectComponents[]? TemplateAppliesTo { get; set; }

        public Language? TemplateLanguage { get; set; }

        internal bool AppliesToProject(IProject project)
        {
            if (TemplateAppliesTo is not null && !TemplateAppliesTo.All(flag => project.Components.HasFlag(flag)))
            {
                return false;
            }

            if (TemplateLanguage is not null && TemplateLanguage != project.Language)
            {
                return false;
            }

            return true;
        }
    }
}
