// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;

using Microsoft.DotNet.UpgradeAssistant.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class UpgradeAssistantCommand<TAppCommand, TOptions> : Command
    where TAppCommand : class, IAppCommand
    where TOptions : IUpgradeAssistantOptions
    {
        public UpgradeAssistantCommand(string name, bool allowsOutputFormatting = false, Action<HostBuilderContext, IServiceCollection, TOptions>? configure = null)
            : base(name)
        {
            Handler = CommandHandler.Create<ParseResult, TOptions, CancellationToken>((result, options, token) =>
                Host.CreateDefaultBuilder()
                    .UseConsoleUpgradeAssistant<TAppCommand>(options, result)
                    .ConfigureServices((context, services) =>
                    {
                        (options as UpgradeAssistantCommandOptions)?.ConfigureServices(context, services);
                        configure?.Invoke(context, services, options);
                    })
                    .RunUpgradeAssistantAsync(token));

            AddArgument(new Argument<FileInfo>("project", LocalizedStrings.UpgradeAssistantCommandProject) { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            AddOption(new Option<ICollection<string>>(new[] { "--extension", "-x" }, LocalizedStrings.UpgradeAssistantCommandExtension));
            AddOption(new Option<ICollection<string>>(new[] { "--option", "-o" }, LocalizedStrings.UpgradeAssistantCommandOption));
            AddOption(new Option<ICollection<string>>(new[] { "--entry-point", "-e" }, LocalizedStrings.UpgradeAssistantCommandEntrypoint));
            AddOption(new Option<bool>(new[] { "--ignore-unsupported-features", "-i" }, LocalizedStrings.UpgradeAssistantCommandIgnoreUnsupported));
            AddOption(new Option<DirectoryInfo>(new[] { "--vs-path" }, LocalizedStrings.UpgradeAssistantCommandVS));
            AddOption(new Option<DirectoryInfo>(new[] { "--msbuild-path" }, LocalizedStrings.UpgradeAssistantCommandMsbuild));

            this.AddUniversalOptions(enableOutputFormatting: allowsOutputFormatting);
        }
    }
}
