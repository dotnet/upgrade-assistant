// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;

using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal class FeatureFlagCommand : Command
    {
        public FeatureFlagCommand()
            : base("features")
        {
            AddOption(new Option<bool>("--list", description: LocalizedStrings.FeaturesListOptionHelp));
            Handler = CommandHandler.Create<ParseResult, FeatureUpgradeOptions, CancellationToken>((result, options, token) =>
             Host.CreateDefaultBuilder()
                 .UseConsoleUpgradeAssistant<Features>(options, result)
                 .ConfigureServices(services =>
                 {
                     services.AddOptions<FeatureOption>()
                         .Configure(opts =>
                         {
                             opts.Command = options switch
                             {
                                 { List: true } => FeatureCommand.List,
                                 _ => FeatureCommand.None,
                             };
                         });

                     services.AddOptions<ExtensionOptions>()
                         .Configure(options =>
                         {
                             options.LoadExtensions = false;
                         });
                 })
                 .RunUpgradeAssistantAsync(token));
        }

        private class Features : IAppCommand
        {
            private readonly IOptions<FeatureOption> _options;
            private readonly ILogger<Features> _logger;

            public Features(IOptions<FeatureOption> options, ILogger<Features> logger)
            {
                _options = options;
                _logger = logger;
            }

            public Task RunAsync(CancellationToken token)
            {
                _logger.LogWarning(LocalizedStrings.FeaturesDisclaimer);

                if (_options.Value.Command is FeatureCommand.List)
                {
                    _logger.LogInformation(LocalizedStrings.FeaturesListHeader);

                    foreach (var feature in FeatureFlags.RegisteredFeatures.OrderBy(static f => f))
                    {
                        _logger.LogInformation("'{Feature}': {IsEnabled}", feature, FeatureFlags.IsRegistered(feature));
                    }
                }
                else
                {
                    _logger.LogError(LocalizedStrings.FeaturesListOptionRequired);
                }

                return Task.CompletedTask;
            }
        }

        private enum FeatureCommand
        {
            None,
            List,
        }

        private class FeatureOption
        {
            public FeatureCommand Command { get; set; }
        }

        private class FeatureUpgradeOptions : BaseUpgradeAssistantOptions
        {
            public bool List { get; set; }
        }
    }
}
