// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    public record TypeMapping
    {
        public string OldName { get; init; }

        public string? NewName { get; init; }

        public string SimpleName { get; init; }

        public TypeMapping(string oldName, string? newName)
        {
            OldName = oldName ?? throw new System.ArgumentNullException(nameof(oldName));
            NewName = newName;
            SimpleName = OldName.LastIndexOf('.') < 0
            ? OldName
            : OldName!.Substring(OldName.LastIndexOf('.') + 1);
        }
    }
}
