// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;

using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    internal class ConsoleUpgradeCommand : UpgradeAssistantCommand<ConsoleUpgrade, ConsoleUpgradeCommand.UpgradeOptions>
    {
        public ConsoleUpgradeCommand()
            : base("upgrade", configure: (ctx, services, options) =>
            {
                services.AddNonInteractive(opts =>
                {
                    opts.Wait = TimeSpan.FromSeconds(options.NonInteractiveWait);
                }, options.NonInteractive);

                services.AddKnownExtensionOptions(new()
                {
                    SkipBackup = options.SkipBackup,
                    Entrypoints = options.EntryPoint
                });
            })
        {
            AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."));
            AddOption(new Option<bool>(new[] { "--non-interactive" }, "Automatically select each first option in non-interactive mode."));
            AddOption(new Option<int>(new[] { "--non-interactive-wait" }, "Wait the supplied seconds before moving on to the next option in non-interactive mode."));
        }

        internal class UpgradeOptions : UpgradeAssistantCommandOptions
        {
            public bool SkipBackup { get; set; }

            public bool NonInteractive { get; set; }

            public int NonInteractiveWait { get; set; }
        }
    }
}
