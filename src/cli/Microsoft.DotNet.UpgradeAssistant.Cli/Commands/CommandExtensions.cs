// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands
{
    internal static class CommandExtensions
    {
        public static void AddUniversalOptions(this Command command, bool enableOutputFormatting = false)
        {
            command.AddOption(new Option<UpgradeTarget>(new[] { "--target-tfm-support", "-t" }, LocalizedStrings.UpgradeAssistantCommandTargetTfm));
            command.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, LocalizedStrings.VerboseCommand));

            command.AddOption(new Option<string>(new[] { "--format", "-f" }, LocalizedStrings.UpgradeAssistantCommandFormat));

            command.AddCommand(new ConsoleAnalyzeCommand.ListFormatsCommand());
        }
    }
}
