// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public class UpgradeAssistantCommand : Command
    {
        public UpgradeAssistantCommand(string name)
            : base(name)
        {
            AddArgument(new Argument<FileInfo>("project") { Arity = ArgumentArity.ExactlyOne }.ExistingOnly());
            AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose diagnostics"));
            AddOption(new Option<IReadOnlyCollection<string>>(new[] { "--extension" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));
            AddOption(new Option<IReadOnlyCollection<string>>(new[] { "--option" }, "Specifies a .NET Upgrade Assistant extension package to include. This could be an ExtensionManifest.json file, a directory containing an ExtensionManifest.json file, or a zip archive containing an extension. This option can be specified multiple times."));
            AddOption(new Option<IReadOnlyCollection<string>>(new[] { "--entry-point", "-e" }, "Provides the entry-point project to start the upgrade process. This may include globbing patterns such as '*' for match."));
            AddOption(new Option<UpgradeTarget>(new[] { "--target-tfm-support" }, "Select if you would like the Long Term Support (LTS), Current, or Preview TFM. See https://dotnet.microsoft.com/platform/support/policy/dotnet-core for details for what these mean."));
            AddOption(new Option<bool>(new[] { "--ignore-unsupported-features" }, "Acknowledges that upgrade-assistant will not be able to completely upgrade a project. This indicates that the solution must be redesigned (e.g. consider Blazor to replace Web Forms)."));
        }
    }
}
