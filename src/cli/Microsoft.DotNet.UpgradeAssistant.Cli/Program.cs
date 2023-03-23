// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Runtime.InteropServices;

using Microsoft.DotNet.UpgradeAssistant.Cli.Commands.ExtensionManagement;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Console apps don't have a synchronization context")]

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine(LocalizedStrings.MacOSWarning);
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

            if (FeatureFlags.IsAnalyzeBinariesEnabled)
            {
                root.AddCommand(new ConsoleAnalyzeBinariesCommand());
            }

            return new CommandLineBuilder(root)
                .UseDefaults()
                .UseHelp(ctx => ctx.HelpBuilder.CustomizeLayout(_ =>
                    HelpBuilder.Default
                    .GetLayout()
                    .Skip(1)
                    .Prepend(_ =>
                    {
                        ShowHeader();

                        _.Output.WriteLine(LocalizedStrings.UpgradeAssistantHeaderDetails);
                    })))
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
