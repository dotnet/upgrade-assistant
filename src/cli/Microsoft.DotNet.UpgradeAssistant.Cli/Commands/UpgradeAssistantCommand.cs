// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class UpgradeAssistantCommand : Command
    {
        public UpgradeAssistantCommand(string name)
            : base(name)
        {
            AddArgument(new Argument<FileInfo>("project", LocalizedStrings.UpgradeAssistantCommandProject) { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            AddOption(new Option<bool>(new[] { "--verbose", "-v" }, LocalizedStrings.VerboseCommand));
            AddOption(new Option<IReadOnlyCollection<string>>(new[] { "--extension" }, LocalizedStrings.UpgradeAssistantCommandExtension));
            AddOption(new Option<IReadOnlyCollection<string>>(new[] { "--option" }, LocalizedStrings.UpgradeAssistantCommandOption));
            AddOption(new Option<IReadOnlyCollection<string>>(new[] { "--entry-point", "-e" }, LocalizedStrings.UpgradeAssistantCommandEntrypoint));
            AddOption(new Option<UpgradeTarget>(new[] { "--target-tfm-support" }, LocalizedStrings.UpgradeAssistantCommandTargetTfm));
            AddOption(new Option<bool>(new[] { "--ignore-unsupported-features" }, LocalizedStrings.UpgradeAssistantCommandIgnoreUnsupported));
            AddOption(new Option<DirectoryInfo>(new[] { "--vs-path" }, LocalizedStrings.UpgradeAssistantCommandVS));
            AddOption(new Option<DirectoryInfo>(new[] { "--msbuild-path" }, LocalizedStrings.UpgradeAssistantCommandMsbuild));
        }

        protected class CommandOptions : IUpgradeAssistantOptions
        {
            public FileInfo Project { get; set; } = null!;

            // Name must be Extension and not plural as the name of the argument that it binds to is `--extension`
            public IReadOnlyCollection<string> Extension { get; set; } = Array.Empty<string>();

            // Name must be EntryPoint and not plural as the name of the argument that it binds to is `--entry-point`
            public IReadOnlyCollection<string> EntryPoint { get; set; } = Array.Empty<string>();

            public IReadOnlyCollection<string> Option { get; set; } = Array.Empty<string>();

            public bool Verbose { get; set; }

            public bool IsVerbose => Verbose;

            public bool IgnoreUnsupportedFeatures { get; set; }

            public UpgradeTarget TargetTfmSupport { get; set; } = UpgradeTarget.Current;

            public IEnumerable<AdditionalOption> AdditionalOptions => Option.ParseOptions();

            public DirectoryInfo? VSPath { get; set; }

            public DirectoryInfo? MSBuildPath { get; set; }

            public string? Format { get; set; }
        }
    }
}
