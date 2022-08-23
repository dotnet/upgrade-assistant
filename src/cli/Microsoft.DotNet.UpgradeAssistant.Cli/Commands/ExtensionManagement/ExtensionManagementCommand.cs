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

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands.ExtensionManagement
{
    internal class ExtensionManagementCommand : Command
    {
        public ExtensionManagementCommand()
            : base("extensions")
        {
            Handler = CommandHandler.Create<ParseResult, UpgradeAssistantCommandOptions, CancellationToken>((result, options, token) =>
                Host.CreateDefaultBuilder()
                    .ConfigureServices(options.ConfigureServices)
                    .UseConsoleUpgradeAssistant<ConsoleAnalyze>(options, result)
                    .RunUpgradeAssistantAsync(token));

            AddCommand(new AddExtensionCommand());
            AddCommand(new ListExtensionCommand());
            AddCommand(new RemoveExtensionCommand());
            AddCommand(new RestoreExtensionCommand());
            AddCommand(new UpdateExtensionCommand());
            AddCommand(new CreateExtensionCommand());
            AddCommand(new SearchExtensionCommand());
        }

        private class ExtensionCommandBase : Command
        {
            public ExtensionCommandBase(string name)
                : base(name)
            {
                AddOption(new Option<bool>(new[] { "--verbose", "-v" }, LocalizedStrings.VerboseCommand));
            }

            protected void AddHandler<TAppCommand>()
                where TAppCommand : class, IAppCommand
                => Handler = CommandHandler.Create<ExtensionUpgradeAssistantOptions, ParseResult, CancellationToken>((opts, parseResult, token)
                       => Host.CreateDefaultBuilder()
                              .ConfigureServices(opts.ConfigureServices)
                              .UseConsoleUpgradeAssistant<TAppCommand>(opts, parseResult)
                              .ConfigureServices(services =>
                              {
                                  services.AddOptions<ExtensionOptions>()
                                      .Configure(options =>
                                      {
                                          options.LoadExtensions = false;
                                      });

                                  services.AddOptions<ExtensionNameOptions>()
                                    .Configure<IOptions<ExtensionOptions>>((options, extensionOptions) =>
                                    {
                                        options.Path = opts.ExtensionPath;
                                        options.Source = NormalizeSource(opts.Source, extensionOptions.Value.DefaultSource);

                                        if (opts.Name is not null)
                                        {
                                            foreach (var name in opts.Name)
                                            {
                                                options.Extensions.Add(new(name) { Source = options.Source, Version = opts.Version });
                                            }

                                            if (opts.Name.Count == 1)
                                            {
                                                options.Name = opts.Name.First();
                                            }
                                        }
                                    });
                              })
                              .RunConsoleAsync(token));

            private static string NormalizeSource(string? source, string defaultSource)
            {
                if (source is null)
                {
                    return defaultSource;
                }

                if (source.StartsWith('.'))
                {
                    source = Path.GetFullPath(source, Environment.CurrentDirectory);
                }

                return source;
            }
        }

        private class AddExtensionCommand : ExtensionCommandBase
        {
            public AddExtensionCommand()
                : base("add")
            {
                AddHandler<AddExtensionAppCommand>();
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName));
                AddOption(new Option<string>(new[] { "--source" }, LocalizedStrings.ExtensionManagementSource));
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

        private class CreateExtensionCommand : ExtensionCommandBase
        {
            public CreateExtensionCommand()
                : base("create")
            {
                AddHandler<CreateExtensionAppCommand>();
                AddArgument(new Argument<string>("extensionPath", LocalizedStrings.ExtensionManagementName));
            }

            private class CreateExtensionAppCommand : IAppCommand
            {
                private readonly IExtensionProvider _extensionProvider;
                private readonly IExtensionCreator _extensionCreator;
                private readonly IOptions<ExtensionNameOptions> _options;
                private readonly ILogger<CreateExtensionAppCommand> _logger;

                public CreateExtensionAppCommand(
                    IExtensionProvider extensionProvider,
                    IExtensionCreator extensionCreator,
                    IOptions<ExtensionNameOptions> options,
                    ILogger<CreateExtensionAppCommand> logger)
                {
                    _extensionProvider = extensionProvider ?? throw new ArgumentNullException(nameof(extensionProvider));
                    _extensionCreator = extensionCreator ?? throw new ArgumentNullException(nameof(extensionCreator));
                    _options = options ?? throw new ArgumentNullException(nameof(options));
                    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                }

                public Task RunAsync(CancellationToken token)
                {
                    var options = _options.Value;

                    if (string.IsNullOrEmpty(options.Path))
                    {
                        _logger.LogError("Path must be set");
                        return Task.CompletedTask;
                    }

                    var extension = _extensionProvider.OpenExtension(options.Path);

                    if (extension?.Version is null)
                    {
                        _logger.LogError("Incorrectly formed extension.");
                        return Task.CompletedTask;
                    }

                    var output = Path.Combine(Environment.CurrentDirectory, $"{extension.Name}.{extension.Version}.nupkg");

                    using var fs = File.Open(output, FileMode.Create, FileAccess.ReadWrite);
                    if (_extensionCreator.TryCreateExtensionFromDirectory(extension, fs))
                    {
                        _logger.LogInformation("Created extension '{Name}' to file {Path}", extension.Name, output);
                    }
                    else
                    {
                        _logger.LogError("Error creating extension");
                    }

                    return Task.CompletedTask;
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
                private readonly IExtensionProvider _extensionProvider;
                private readonly ILogger<ListExtensionAppCommand> _logger;

                public ListExtensionAppCommand(IExtensionProvider extensionProvider, ILogger<ListExtensionAppCommand> logger)
                {
                    _extensionProvider = extensionProvider;
                    _logger = logger;
                }

                public Task RunAsync(CancellationToken token)
                {
                    _logger.LogInformation(LocalizedStrings.ListExtensionDetails);

                    foreach (var n in _extensionProvider.Registered)
                    {
                        _logger.LogInformation(LocalizedStrings.ListExtensionItem, n.Name, n.Source);
                    }

                    return Task.CompletedTask;
                }
            }
        }

        private class RestoreExtensionCommand : ExtensionCommandBase
        {
            public RestoreExtensionCommand()
                : base("restore")
            {
                AddHandler<RestoreExtensionAppCommand>();
            }

            private class RestoreExtensionAppCommand : IAppCommand
            {
                private readonly IExtensionManager _extensionManager;
                private readonly ILogger<RestoreExtensionAppCommand> _logger;

                public RestoreExtensionAppCommand(IExtensionManager extensionManager, ILogger<RestoreExtensionAppCommand> logger)
                {
                    _extensionManager = extensionManager;
                    _logger = logger;
                }

                public async Task RunAsync(CancellationToken token)
                {
                    _logger.LogInformation("Restoring extensions...");
                    await _extensionManager.RestoreExtensionsAsync(token);
                    _logger.LogInformation("Done restoring extensions...");
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
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.ZeroOrMore });
            }

            private class UpdateExtensionAppCommand : IAppCommand
            {
                private readonly IOptions<ExtensionNameOptions> _options;
                private readonly IExtensionProvider _extensionProvider;
                private readonly IExtensionManager _extensionManager;
                private readonly ILogger<UpdateExtensionAppCommand> _logger;

                public UpdateExtensionAppCommand(IOptions<ExtensionNameOptions> options, IExtensionProvider extensionProvider, IExtensionManager extensionManager, ILogger<UpdateExtensionAppCommand> logger)
                {
                    _options = options ?? throw new ArgumentNullException(nameof(options));
                    _extensionProvider = extensionProvider ?? throw new ArgumentNullException(nameof(extensionProvider));
                    _extensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
                    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                }

                public async Task RunAsync(CancellationToken token)
                {
                    foreach (var packageName in PackagesToUpdate)
                    {
                        _logger.LogInformation(LocalizedStrings.UpdateExtensionDetails, packageName);

                        var result = await _extensionManager.UpdateAsync(packageName, token);

                        if (result is null)
                        {
                            _logger.LogInformation(LocalizedStrings.UpdateExtensionFailed, packageName);
                        }
                        else
                        {
                            _logger.LogInformation(LocalizedStrings.UpdateExtensionSuccess, result.Name, result.Version);
                        }
                    }
                }

                private IEnumerable<string> PackagesToUpdate
                {
                    get
                    {
                        var requested = _options.Value.Extensions;

                        if (requested.Count > 0)
                        {
                            return requested.Select(r => r.Name);
                        }

                        return _extensionProvider.Registered.Select(e => e.Name);
                    }
                }
            }
        }

        private class SearchExtensionCommand : ExtensionCommandBase
        {
            public SearchExtensionCommand()
                : base("search")
            {
                AddHandler<SearchExtensionAppCommand>();
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.ZeroOrOne });
            }

            private class SearchExtensionAppCommand : IAppCommand
            {
                private readonly IOptions<ExtensionNameOptions> _options;
                private readonly IExtensionManager _extensionManager;
                private readonly ILogger<SearchExtensionAppCommand> _logger;

                public SearchExtensionAppCommand(IOptions<ExtensionNameOptions> options, IExtensionManager extensionManager, ILogger<SearchExtensionAppCommand> logger)
                {
                    _options = options ?? throw new ArgumentNullException(nameof(options));
                    _extensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
                    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                }

                public async Task RunAsync(CancellationToken token)
                {
                    if (_options.Value.Source is string source)
                    {
                        var name = _options.Value.Name ?? string.Empty;

                        await foreach (var result in _extensionManager.SearchAsync(name, source, token).WithCancellation(token))
                        {
                            _logger.LogInformation(LocalizedStrings.ExtensionManagementSearch, result.Name, result.Version);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(LocalizedStrings.ExtensionManagementNoQuery);
                    }
                }
            }
        }

        private class ExtensionNameOptions
        {
            public ICollection<ExtensionSource> Extensions { get; } = new List<ExtensionSource>();

            public string? Path { get; set; }

            // Only populated when a single name has been provided
            public string? Name { get; set; }

            public string Source { get; set; } = null!;
        }

        internal class ExtensionUpgradeAssistantOptions : UpgradeAssistantCommandOptions
        {
            public IReadOnlyCollection<string> Name { get; set; } = Array.Empty<string>();

            public string? Source { get; set; }

            public string? Version { get; set; }

            public string? ExtensionPath { get; set; }
        }
    }
}
