// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Microsoft.DotNet.UpgradeAssistant.Cli.Commands.ExtensionManagement
{
    internal class ExtensionManagementCommand : Command
    {
        private const string DefaultSource = "https://api.nuget.org/v3/index.json";

        public ExtensionManagementCommand()
            : base("extensions")
        {
            IsHidden = true;
            AddCommand(new AddExtensionCommand());
            AddCommand(new ListExtensionCommand());
            AddCommand(new RemoveExtensionCommand());
            AddCommand(new UpdateExtensionCommand());
        }

        private class AddExtensionCommand : Command
        {
            public AddExtensionCommand()
                : base("add")
            {
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.OneOrMore });
                AddOption(new Option<string>(new[] { "--source" }, () => DefaultSource, LocalizedStrings.ExtensionManagementSource));
            }
        }

        private class ListExtensionCommand : Command
        {
            public ListExtensionCommand()
                : base("list")
            {
            }
        }

        private class RemoveExtensionCommand : Command
        {
            public RemoveExtensionCommand()
                : base("remove")
            {
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.OneOrMore });
            }
        }

        private class UpdateExtensionCommand : Command
        {
            public UpdateExtensionCommand()
                : base("update")
            {
                AddArgument(new Argument<string>("name", LocalizedStrings.ExtensionManagementName) { Arity = ArgumentArity.ZeroOrMore });
            }
        }
    }
}
