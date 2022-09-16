// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Linq;

using Microsoft.DotNet.UpgradeAssistant.Cli.Commands;
using Microsoft.DotNet.UpgradeAssistant.Cli.Commands.AnalyzeBinaries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyzeBinariesCommand : Command
    {
        public ConsoleAnalyzeBinariesCommand()
            : base("analyzebinaries")
        {
            Handler = CommandHandler.Create<ParseResult, ConsoleAnalyzeBinariesCommandOptions, CancellationToken>((result, options, token) =>
                Host.CreateDefaultBuilder()
                    .ConfigureServices(services => services.AddSingleton<IBinaryAnalysisExecutorOptions>(options))
                    .UseConsoleUpgradeAssistant<ConsoleAnalyzeBinaries>(options, result)
                    .RunUpgradeAssistantAsync(token));

            AddArgument(new Argument<FileSystemInfo[]>("files-or-directories", parse: r =>
            {
                if (r?.Tokens is null || !r.Tokens.Any())
                {
                    throw new ArgumentException(@"Must specify target file/directory for analysis");
                }

                return r.Tokens.Select<Token, FileSystemInfo?>(i =>
                {
                    var path = i.Value;
                    if (string.IsNullOrEmpty(path))
                    {
                        return default;
                    }

                    if (Directory.Exists(path))
                    {
                        return new DirectoryInfo(path);
                    }
                    else if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                             path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    {
                        return new DirectoryInfo(path);
                    }
                    else
                    {
                        return new FileInfo(path);
                    }
                }).Where(i => i is not null).Cast<FileSystemInfo>().ToArray();
            }, description: LocalizedStrings.BinaryAnalysisContentHelp)
            {
                Arity = ArgumentArity.OneOrMore,
            }.ExistingOnly());

            AddOption(new Option<bool>(new[] { "--allow-prerelease", "-pre" }, LocalizedStrings.BinaryAnalysisAllowPrereleaseHelp));
            AddOption(new Option<bool>(new[] { "--obsoletion", "-obs" }, LocalizedStrings.BinaryAnalysisObsoletedApisHelp));

            var platformOption = new Option<Platform>(
                aliases: new[] { "--platform", "-p" },
                getDefaultValue: () => Platform.Linux,
                description: LocalizedStrings.BinaryAnalysisPlatformHelp);
            AddOption(platformOption);
            this.AddUniversalOptions(enableOutputFormatting: true);
        }
    }
}
