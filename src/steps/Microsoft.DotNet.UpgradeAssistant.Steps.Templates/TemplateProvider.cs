// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Templates
{
    public class TemplateProvider
    {
        private readonly ICollection<TemplateConfiguration> _options;

        public bool IsEmpty => _options.Count == 0;

        public IEnumerable<string> TemplateConfigFileNames => _options.SelectMany(o => o.TemplateItems).Select(t => t.Path);

        public TemplateProvider(IOptions<ICollection<TemplateConfiguration>> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        }

        internal async Task<Dictionary<string, RuntimeItemSpec>> GetTemplatesAsync(IProject project, CancellationToken token)
        {
            var templates = new Dictionary<string, RuntimeItemSpec>();

            // Iterate through all extensions' config files, adding template files from each to the list of items to add, as appropriate.
            // Later extensions can intentionally overwrite earlier extensions' items.
            foreach (var templateConfig in _options)
            {
                // If there was a problem reading the configuration or the configuration only applies to certain output types
                // or project types which don't match the project, then continue to the next configuration.
                if (templateConfig.TemplateItems is null || !await templateConfig.AppliesToProject(project, token).ConfigureAwait(false))
                {
                    continue;
                }

                foreach (var templateItem in templateConfig.TemplateItems)
                {
                    templates[templateItem.Path] = new RuntimeItemSpec(templateItem, templateConfig.Files, templateConfig.Replacements ?? new Dictionary<string, string>());
                }
            }

            return templates;
        }
    }
}
