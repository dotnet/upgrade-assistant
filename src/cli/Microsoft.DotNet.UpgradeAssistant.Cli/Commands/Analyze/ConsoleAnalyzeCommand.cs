// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public delegate IHostBuilder CreateConsoleHost(UpgradeOptions options, ParseResult parseResult);

    public class ConsoleAnalyzeCommand : UpgradeAssistantCommand
    {
        public ConsoleAnalyzeCommand(CreateConsoleHost createHost)
            : base("analyze")
        {
            IsHidden = true;
            Handler = CommandHandler.Create<ParseResult, UpgradeOptions, CancellationToken>((result, options, token) => createHost(options, result)
                .ConfigureServices(ConfigureService)
                .RunUpgradeAssistantAsync(token));
        }

        public static void ConfigureService(IServiceCollection services)
        {
            services.AddScoped<IAppCommand, ConsoleAnalyze>();
        }
    }
}
