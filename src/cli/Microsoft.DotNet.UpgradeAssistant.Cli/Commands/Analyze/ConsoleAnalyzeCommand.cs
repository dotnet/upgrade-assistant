// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using Microsoft.DotNet.UpgradeAssistant.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyzeCommand : UpgradeAssistantCommand
    {
        public ConsoleAnalyzeCommand()
            : base("analyze")
        {
            IsHidden = true;
            Handler = CommandHandler.Create<ParseResult, AnalyzeOptions, CancellationToken>((result, options, token) =>
                Host.CreateDefaultBuilder()
                    .UseConsoleUpgradeAssistant<ConsoleAnalyze>(options, result)
                    .RunUpgradeAssistantAsync(token));
        }

        private class AnalyzeOptions : IUpgradeAssistantOptions
        {
            public bool Verbose { get; set; }

            public bool IsVerbose => Verbose;

            public FileInfo Project { get; set; } = null!;

            public bool IgnoreUnsupportedFeatures { get; set; }

            public UpgradeTarget TargetTfmSupport { get; set; }

            public IReadOnlyCollection<string> Extension { get; set; } = Array.Empty<string>();

            public IReadOnlyCollection<string> Option { get; set; } = Array.Empty<string>();

            public IEnumerable<AdditionalOption> AdditionalOptions => Option.ParseOptions();
        }
    }
}
