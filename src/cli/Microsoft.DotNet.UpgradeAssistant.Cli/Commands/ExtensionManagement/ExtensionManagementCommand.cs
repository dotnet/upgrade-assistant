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

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands.ExtensionManagement
{
    internal class ExtensionManagementCommand : Command
    {
        private const string DefaultSource = "https://api.nuget.org/v3/index.json";

        public ExtensionManagementCommand()
            : base("extensions")
        {
            AddCommand(new AddExtensionCommand());
            AddCommand(new ListExtensionCommand());
            AddCommand(new RemoveExtensionCommand());
            AddCommand(new UpdateExtensionCommand());
        }

        private class ExtensionCommandBase : Command
        {
            public ExtensionCommandBase(string name)
                : base(name)
            {
            }

            protected void AddHandler<TAppCommand>()
                where TAppCommand : class, IAppCommand
                => Handler = CommandHandler.Create<ExtensionOptions, ParseResult, CancellationToken>((opts, parseResult, token)
                       => Host.CreateDefaultBuilder()
                              .UseConsoleUpgradeAssistant<TAppCommand>(opts, parseResult)
                              .ConfigureServices(services =>
                              {
                                  services.AddOptions<ExtensionNameOptions>()
                                    .Configure(options =>
                                    {
                                        foreach (var name in opts.Name)
                                        {
                                            options.Extensions.Add(new(name) { Source = opts.Source, Version = opts.Version });
                                        }
                                    });
                              })
                              .RunConsoleAsync(token));
        }

        private class AddExtensionCommand : ExtensionCommandBase
        {
            public AddExtensionCommand()
                : base("add")
            {
                AddHandler<AddExtensionAppCommand>();
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName));
                AddOption(new Option<string>(new[] { "--source" }, () => DefaultSource, LocalizedStrings.ExtensionManagementSource));
                AddOption(new Option<string>(new[] { "--version" }, LocalizedStrings.ExtensionManagementVersion));
            }

            private class AddExtensionAppCommand : IAppCommand
            {
                private readonly IOptions<ExtensionNameOptions> _options;
                private readonly IExtensionManager _extensionManager;
                private readonly ILogger<AddExtensionAppCommand> _logger;

                public AddExtensionAppCommand(IOptions<ExtensionNameOptions> options, IExtensionManager extensionManager, ILogger<AddExtensionAppCommand> logger)
                {
                    _options = options;
                    _extensionManager = extensionManager;
                    _logger = logger;
                }

                public async Task RunAsync(CancellationToken token)
                {
                    foreach (var n in _options.Value.Extensions)
                    {
                        _logger.LogInformation(LocalizedStrings.AddExtensionDetails, n.Name, n.Source);

                        var result = await _extensionManager.AddAsync(n, token);

                        if (result is null)
                        {
                            _logger.LogWarning(LocalizedStrings.AddExtensionFailed, n.Name, n.Source);
                        }
                        else
                        {
                            _logger.LogInformation(LocalizedStrings.AddExtensionSuccess, result.Name, result.Source);
                        }
                    }
                }
            }
        }

        private class ListExtensionCommand : ExtensionCommandBase
        {
            public ListExtensionCommand()
                : base("list")
            {
                AddHandler<ListExtensionAppCommand>();
            }

            private class ListExtensionAppCommand : IAppCommand
            {
                private readonly IExtensionManager _extensionManager;
                private readonly ILogger<ListExtensionAppCommand> _logger;

                public ListExtensionAppCommand(IExtensionManager extensionManager, ILogger<ListExtensionAppCommand> logger)
                {
                    _extensionManager = extensionManager;
                    _logger = logger;
                }

                public Task RunAsync(CancellationToken token)
                {
                    _logger.LogInformation(LocalizedStrings.ListExtensionDetails);

                    foreach (var n in _extensionManager.Registered)
                    {
                        _logger.LogInformation(LocalizedStrings.ListExtensionItem, n.Name, n.Source);
                    }

                    return Task.CompletedTask;
                }
            }
        }

        private class RemoveExtensionCommand : ExtensionCommandBase
        {
            public RemoveExtensionCommand()
                : base("remove")
            {
                AddHandler<RemoveExtensionAppCommand>();
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.OneOrMore });
            }

            private class RemoveExtensionAppCommand : IAppCommand
            {
                private readonly IOptions<ExtensionNameOptions> _options;
                private readonly IExtensionManager _extensionManager;
                private readonly ILogger<RemoveExtensionAppCommand> _logger;

                public RemoveExtensionAppCommand(IOptions<ExtensionNameOptions> options, IExtensionManager extensionManager, ILogger<RemoveExtensionAppCommand> logger)
                {
                    _options = options;
                    _extensionManager = extensionManager;
                    _logger = logger;
                }

                public async Task RunAsync(CancellationToken token)
                {
                    foreach (var n in _options.Value.Extensions)
                    {
                        _logger.LogInformation(LocalizedStrings.RemovingExtension, n.Name);

                        if (!await _extensionManager.RemoveAsync(n.Name, token))
                        {
                            _logger.LogWarning(LocalizedStrings.RemovingExtensionFailed, n.Name);
                        }
                    }
                }
            }
        }

        private class UpdateExtensionCommand : ExtensionCommandBase
        {
            public UpdateExtensionCommand()
                : base("update")
            {
                AddHandler<UpdateExtensionAppCommand>();
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName));
                AddOption(new Option<string>(new[] { "--version" }, () => DefaultSource, LocalizedStrings.ExtensionManagementVersion));
            }

            private class UpdateExtensionAppCommand : IAppCommand
            {
                private readonly IOptions<ExtensionNameOptions> _options;
                private readonly IExtensionManager _extensionManager;
                private readonly ILogger<UpdateExtensionAppCommand> _logger;

                public UpdateExtensionAppCommand(IOptions<ExtensionNameOptions> options, IExtensionManager extensionManager, ILogger<UpdateExtensionAppCommand> logger)
                {
                    _options = options;
                    _extensionManager = extensionManager;
                    _logger = logger;
                }

                public async Task RunAsync(CancellationToken token)
                {
                    foreach (var n in _options.Value.Extensions)
                    {
                        _logger.LogInformation(LocalizedStrings.UpdateExtensionDetails, n.Name);

                        var result = await _extensionManager.UpdateAsync(n.Name, token);

                        if (result is null)
                        {
                            _logger.LogInformation(LocalizedStrings.UpdateExtensionFailed, n.Name);
                        }
                        else
                        {
                            _logger.LogInformation(LocalizedStrings.UpdateExtensionSuccess, n.Name, n.Version);
                        }
                    }
                }
            }
        }

        private class ExtensionNameOptions
        {
            public ICollection<ExtensionSource> Extensions { get; } = new List<ExtensionSource>();
        }

        private class ExtensionOptions : IUpgradeAssistantOptions
        {
            public bool Verbose { get; set; }

            public bool IsVerbose => Verbose;

            public FileInfo Project { get; set; } = null!;

            public IReadOnlyCollection<string> Name { get; set; } = Array.Empty<string>();

            public string? Source { get; set; }

            public string? Version { get; set; }

            public bool IgnoreUnsupportedFeatures { get; set; }

            public UpgradeTarget TargetTfmSupport { get; set; }

            public IReadOnlyCollection<string> Extension => Array.Empty<string>();

            public IEnumerable<AdditionalOption> AdditionalOptions => Enumerable.Empty<AdditionalOption>();

            public DirectoryInfo? VSPath { get; set; }
        }
    }
}
