// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers
{
    /// <summary>
    /// Describes a mapping of syntaxes (namespaces, types, or members) with a
    /// diagnostic message that should be displayed if a user uses those syntaxes.
    /// </summary>
    public record TargetSyntaxMessage(string Id, IEnumerable<TargetSyntax> TargetSyntaxes, string Message);
}
