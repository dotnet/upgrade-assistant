// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleUpgradeCommand : UpgradeAssistantCommand
    {
        public ConsoleUpgradeCommand()
            : base("upgrade")
        {
            Handler = CommandHandler.Create<ParseResult, UpgradeOptions, CancellationToken>((result, options, token) =>
                Host.CreateDefaultBuilder()
                    .UseConsoleUpgradeAssistant<ConsoleUpgrade>(options, result)
                    .ConfigureServices(services =>
                    {
                        services.AddNonInteractive(opts =>
                        {
                            opts.Wait = TimeSpan.FromSeconds(options.NonInteractiveWait);
                        }, options.NonInteractive);

                        services.AddKnownExtensionOptions(new() { SkipBackup = options.SkipBackup, Entrypoints = options.EntryPoint });
                    })
                    .RunUpgradeAssistantAsync(token));

            AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."));
            AddOption(new Option<bool>(new[] { "--non-interactive" }, "Automatically select each first option in non-interactive mode."));
            AddOption(new Option<int>(new[] { "--non-interactive-wait" }, "Wait the supplied seconds before moving on to the next option in non-interactive mode."));
        }

        private class UpgradeOptions : IUpgradeAssistantOptions
        {
            public FileInfo Project { get; set; } = null!;

            // Name must be Extension and not plural as the name of the argument that it binds to is `--extension`
            public IReadOnlyCollection<string> Extension { get; set; } = Array.Empty<string>();

            public bool SkipBackup { get; set; }

            // Name must be EntryPoint and not plural as the name of the argument that it binds to is `--entry-point`
            public IReadOnlyCollection<string> EntryPoint { get; set; } = Array.Empty<string>();

            public IReadOnlyCollection<string> Option { get; set; } = Array.Empty<string>();

            public bool Verbose { get; set; }

            public bool IsVerbose => Verbose;

            public bool IgnoreUnsupportedFeatures { get; set; }

            public bool NonInteractive { get; set; }

            public int NonInteractiveWait { get; set; } = 2;

            public UpgradeTarget TargetTfmSupport { get; set; } = UpgradeTarget.Current;

            public IEnumerable<AdditionalOption> AdditionalOptions => Option.ParseOptions();
        }
    }
}
