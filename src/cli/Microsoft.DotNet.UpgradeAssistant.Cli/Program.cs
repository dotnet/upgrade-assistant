// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.DotNet.UpgradeAssistant.Cli.Commands.ExtensionManagement;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Console apps don't have a synchronization context")]

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public static class Program
    {
        static readonly string location;

        static Program()
        {
            location = Assembly.GetCallingAssembly().Location;
            location = Path.GetDirectoryName(location) ?? string.Empty;
        }

        public static Task<int> Main(string[] args)
        {
            if (FeatureFlags.IsWindowsCheckEnabled && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine(LocalizedStrings.NonWindowsWarning);
                return Task.FromResult(ErrorCodes.PlatformNotSupported);
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

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

        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            Assembly? result = null;
            AssemblyName an = new(args.Name);
            if (an.Name?.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) == false)
            {
                string dllName = an.Name + ".dll";
                string path = Path.Combine(location, dllName);
                if (!File.Exists(path))
                {
                    Debug.WriteLine("ADDED " + dllName);
                    string source = @"C:\v1\out\tests\x86chk\DesignTools.Tests.Component.SurfaceDesigner\" + dllName;
                    string dest = @"C:\x1\upas\artifacts\bin\Microsoft.DotNet.UpgradeAssistant.Cli\Debug\net7.0\" + dllName;
                    File.Copy(source, dest);
                }

                try
                {
                    result = Assembly.LoadFile(path);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            return result;
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
