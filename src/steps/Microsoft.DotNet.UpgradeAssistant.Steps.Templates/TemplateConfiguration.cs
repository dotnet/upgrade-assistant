// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateConfiguration
    {
        // This is a false positive
#pragma warning disable CA2227 // Collection properties should be read only
        public Dictionary<string, string>? Replacements { get; init; }
#pragma warning restore CA2227 // Collection properties should be read only

        public ItemSpec[]? TemplateItems { get; set; }

        public ProjectOutputType[]? TemplateOutputType { get; set; }

        public ProjectComponents[]? TemplateAppliesTo { get; set; }

        public Language? TemplateLanguage { get; set; }

        internal async ValueTask<bool> AppliesToProject(IProject project, CancellationToken token)
        {
            if (TemplateOutputType is not null && !TemplateOutputType.Contains(project.OutputType))
            {
                return false;
            }

            var components = await project.GetComponentsAsync(token).ConfigureAwait(false);

            if (TemplateAppliesTo is not null && !TemplateAppliesTo.All(flag => components.HasFlag(flag)))
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
