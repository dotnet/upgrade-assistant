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
            IsHidden = !FeatureFlags.IsRequested("EXTENSION_MANAGEMENT");
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
                                            options.Extensions.Add(new(name) { Source = opts.Source });
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
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.OneOrMore });
                AddOption(new Option<string>(new[] { "--source" }, () => DefaultSource, LocalizedStrings.ExtensionManagementSource));
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
                        _logger.LogInformation("Adding extension {Name} from {Source}", n.Name, n.Source);

                        var result = await _extensionManager.AddAsync(n, token);

                        if (result is null)
                        {
                            _logger.LogWarning("Could not find extension {Name} from {Source}", n.Name, n.Source);
                        }
                        else
                        {
                            _logger.LogInformation("Added extension {Name} from {Source}", result.Name, result.Source);
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
                    _logger.LogInformation("Current extensions:");

                    foreach (var n in _extensionManager.Registered)
                    {
                        _logger.LogInformation("{Name}: {Source}", n.Name, n.Source);
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

                public Task RunAsync(CancellationToken token)
                {
                    foreach (var n in _options.Value.Extensions)
                    {
                        _logger.LogInformation("Removing extension '{Name}'", n.Name);

                        if (!_extensionManager.Remove(n.Name))
                        {
                            _logger.LogWarning("Could not remove extension '{Name}'", n.Name);
                        }
                    }

                    return Task.CompletedTask;
                }
            }
        }

        private class UpdateExtensionCommand : ExtensionCommandBase
        {
            public UpdateExtensionCommand()
                : base("update")
            {
                AddHandler<UpdateExtensionAppCommand>();
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.ZeroOrMore });
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
                        _logger.LogInformation("Searching for updates for {Name}", n.Name);

                        var result = await _extensionManager.UpdateAsync(n.Name, token);

                        if (result is null)
                        {
                            _logger.LogInformation("Could not find an update for extension {Name}", n.Name);
                        }
                        else
                        {
                            _logger.LogInformation("Found an update for {Name} to {Version}", n.Name, n.Version);
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

            public bool IgnoreUnsupportedFeatures { get; set; }

            public UpgradeTarget TargetTfmSupport { get; set; }

            public IReadOnlyCollection<string> Extension => Array.Empty<string>();

            public IEnumerable<AdditionalOption> AdditionalOptions => Enumerable.Empty<AdditionalOption>();
        }
    }
}
