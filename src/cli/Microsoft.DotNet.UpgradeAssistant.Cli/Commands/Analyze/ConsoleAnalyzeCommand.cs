// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;

using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyzeCommand : UpgradeAssistantCommand<ConsoleAnalyze>
    {
        public ConsoleAnalyzeCommand()
            : base("analyze", allowsOutputFormatting: true) { }

        internal class ListFormatsCommand : Command
        {
            public ListFormatsCommand()
                : base("list-formats")
            {
                Handler = CommandHandler.Create<ParseResult, UpgradeAssistantCommandOptions, CancellationToken>((result, options, token) =>
                    Host.CreateDefaultBuilder()
                        .ConfigureServices(options.ConfigureServices)
                        .UseConsoleUpgradeAssistant<ListFormats>(options, result)
                        .RunUpgradeAssistantAsync(token));
            }

            private class ListFormats : IAppCommand
            {
                private readonly IEnumerable<IOutputResultWriter> _writers;
                private readonly ILogger<ListFormats> _logger;

                public ListFormats(IEnumerable<IOutputResultWriter> writers, ILogger<ListFormats> logger)
                {
                    _writers = writers;
                    _logger = logger;
                }

                public Task RunAsync(CancellationToken token)
                {
                    foreach (var writer in _writers)
                    {
                        _logger.LogInformation("Analysis format available: '{Format}'", writer.Format);
                    }

                    return Task.CompletedTask;
                }
            }
        }
    }
}
