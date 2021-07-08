// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
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
                    .UseUpgradeAssistantOptions(options)
                    .RunUpgradeAssistantAsync(token));

            AddOption(new Option<bool>(new[] { "--skip-backup" }, "Disables backing up the project. This is not recommended unless the project is in source control since this tool will make large changes to both the project and source files."));
            AddOption(new Option<bool>(new[] { "--non-interactive" }, "Automatically select each first option in non-interactive mode."));
            AddOption(new Option<int>(new[] { "--non-interactive-wait" }, "Wait the supplied seconds before moving on to the next option in non-interactive mode."));
        }
    }
}
