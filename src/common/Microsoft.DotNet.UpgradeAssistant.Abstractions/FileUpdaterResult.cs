// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record FileUpdaterResult(
        string RuleId,
        string RuleName,
        string FullDescription,
        bool Result,
        IEnumerable<string> FilePaths) : IUpdaterResult { }
}
