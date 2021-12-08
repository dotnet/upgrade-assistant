// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    public record WinformsUpdaterResult(
        string RuleId,
        string RuleName,
        string FullDescription,
        bool Result,
        string Message,
        IList<string> FileLocations) : IUpdaterResult
    {
    }
}
