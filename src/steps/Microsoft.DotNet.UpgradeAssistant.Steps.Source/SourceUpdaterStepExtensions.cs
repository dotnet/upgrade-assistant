// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Source;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class SourceUpdaterStepExtensions
    {
        private const string SourceUpdaterOptionsSection = "SourceUpdater";

        public static void AddSourceUpdaterStep(this IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Services.AddUpgradeStep<SourceUpdaterStep>();
            services.AddExtensionOption<SourceUpdaterOptions>(SourceUpdaterOptionsSection);

            // TODO - In the future, this should map the options to IEnumerable<AdditionalText> using
            //        extension mapping APIs. Currently, though, extension option mapping only works
            //        with json serialized files.
            services.Services.AddTransient<IEnumerable<AdditionalText>>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ICollection<SourceUpdaterOptions>>>().Value;
                var textPaths = options.SelectMany(o => o.AdditionalAnalyzerTexts);
                return textPaths.Select(p => new AdditionalFileText(p));
            });
        }
    }
}
