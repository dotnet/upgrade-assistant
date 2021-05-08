// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.DotNet.UpgradeAssistant.Steps.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public static class TemplateInserterStepExtensions
    {
        private const string TemplateInserterOptionsSectionName = "TemplateInserter";

        public static IServiceCollection AddTemplateInserterStep(this IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new System.ArgumentNullException(nameof(services));
            }

            services.Services.AddSingleton<TemplateProvider>();
            services.Services.AddUpgradeStep<TemplateInserterStep>();

            services.Services.AddOptions<JsonSerializerOptions>()
                .Configure(o => o.Converters.Add(new JsonStringProjectItemTypeConverter()));
            services.AddExtensionOption<TemplateInserterOptions>(TemplateInserterOptionsSectionName)
                .MapFiles<TemplateConfiguration>(t => t.TemplateConfigFiles, isArray: false);

            return services.Services;
        }
    }
}
