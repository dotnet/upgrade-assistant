// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            AddOption(new Option<bool>("--list", description: "List available feature flags and status"));
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
                _logger.LogWarning("Features are subject to change and may be disabled in a subsequent release.");
                _logger.LogInformation("Features are registered in the environment variable `UA_FEATURES`. Below are available features:");

                if (_options.Value.Command is FeatureCommand.List)
                {
                    foreach (var feature in FeatureFlags.RegisteredFeatures.OrderBy(static f => f))
                    {
                        _logger.LogInformation("'{Feature}': {IsEnabled}", feature, FeatureFlags.IsRegistered(feature));
                    }
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

        private class FeatureUpgradeOptions : IUpgradeAssistantOptions
        {
            public bool List { get; set; }

            public bool Verbose { get; set; }

            public bool IsVerbose => Verbose;

            public FileInfo? Project => null;

            public bool IgnoreUnsupportedFeatures => false;

            public UpgradeTarget TargetTfmSupport => default;

            public IReadOnlyCollection<string> Extension => Array.Empty<string>();

            public IEnumerable<AdditionalOption> AdditionalOptions => Enumerable.Empty<AdditionalOption>();

            public DirectoryInfo? VSPath => null;

            public DirectoryInfo? MSBuildPath => null;

            public string? Format => null;
        }
    }
}
