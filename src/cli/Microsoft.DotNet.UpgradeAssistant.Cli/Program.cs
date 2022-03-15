// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant.Cli.Commands.ExtensionManagement;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Console apps don't have a synchronization context")]

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            if (FeatureFlags.IsWindowsCheckEnabled && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine(LocalizedStrings.NonWindowsWarning);
                return Task.FromResult(ErrorCodes.PlatformNotSupported);
            }

            var root = new RootCommand
            {
                // Get name from process so that it will show correctly if run as a .NET CLI tool
                Name = GetProcessName(),
            };

            // Top-level commands (upgrade, analyze, etc.) are registered as commands and parse with System.CommandLine
            root.AddCommand(new ConsoleAnalyzeCommand());
            root.AddCommand(new ConsoleUpgradeCommand());
            root.AddCommand(new ExtensionManagementCommand());
            root.AddCommand(new FeatureFlagCommand());

            var helpBuilder = new HelpBuilder(LocalizationResources.Instance, ConsoleUtils.Width);
            helpBuilder.CustomizeLayout(_ =>
                            HelpBuilder.Default
                                       .GetLayout()
                                       .Prepend(_ => ShowHeader()));

            return new CommandLineBuilder(root)
                .UseDefaults()
                .UseHelpBuilder(ctx => helpBuilder)
                .Build()
                .InvokeAsync(args);

            static string GetProcessName()
            {
                using var current = System.Diagnostics.Process.GetCurrentProcess();
                return current.ProcessName;
            }
        }

        public static void ShowHeader()
        {
            var header = SR.Format(LocalizedStrings.UpgradeAssistantHeader, UpgradeVersion.Current.FullVersion);
            var survey = LocalizedStrings.SurveyText;
            var length = Math.Max(header.Length, survey.Length);
            var bar = new string('-', length);

            Console.WriteLine(bar);
            Console.WriteLine(header);
            Console.WriteLine();
            Console.WriteLine(survey);
            Console.WriteLine(bar);
            Console.WriteLine();
        }
    }
}
