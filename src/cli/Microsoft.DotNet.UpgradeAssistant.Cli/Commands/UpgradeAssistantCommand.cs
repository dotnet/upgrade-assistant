// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using Microsoft.DotNet.UpgradeAssistant.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class UpgradeAssistantCommand<TAppCommand> : UpgradeAssistantCommand<TAppCommand, UpgradeAssistantCommandOptions>
        where TAppCommand : class, IAppCommand
    {
        public UpgradeAssistantCommand(string name, bool allowsOutputFormatting = false, Action<HostBuilderContext, IServiceCollection, UpgradeAssistantCommandOptions>? configure = null)
         : base(name, allowsOutputFormatting, configure)
        {
        }
    }
}
