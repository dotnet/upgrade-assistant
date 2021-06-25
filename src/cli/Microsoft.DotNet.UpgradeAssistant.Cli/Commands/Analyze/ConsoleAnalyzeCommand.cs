// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class ConsoleAnalyzeCommand : UpgradeAssistantCommand
    {
        public ConsoleAnalyzeCommand()
            : base("analyze")
        {
            IsHidden = true;
            Handler = CommandHandler.Create<ParseResult, UpgradeOptions, CancellationToken>((result, options, token) =>
                Host.CreateDefaultBuilder()
                    .UseConsoleUpgradeAssistant(options, result)
                    .ConfigureServices(ConfigureService)
                    .RunUpgradeAssistantAsync(token));
        }

        public static void ConfigureService(IServiceCollection services)
        {
            services.AddScoped<IAppCommand, ConsoleAnalyze>();
        }
    }
}
