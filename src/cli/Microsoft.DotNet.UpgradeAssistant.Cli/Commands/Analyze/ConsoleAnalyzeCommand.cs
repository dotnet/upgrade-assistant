// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyzeCommand : UpgradeAssistantCommand
    {
        public ConsoleAnalyzeCommand()
            : base("analyze")
        {
            Handler = CommandHandler.Create<ParseResult, CommandOptions, CancellationToken>((result, options, token) =>
                Host.CreateDefaultBuilder()
                    .UseConsoleUpgradeAssistant<ConsoleAnalyze>(options, result)
                    .RunUpgradeAssistantAsync(token));

            AddOption(new Option<string>(new[] { "--format", "-f" }, LocalizedStrings.UpgradeAssistantCommandFormat));
        }
    }
}
