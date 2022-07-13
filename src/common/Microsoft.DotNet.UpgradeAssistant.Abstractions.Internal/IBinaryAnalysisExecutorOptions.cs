// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public interface IBinaryAnalysisExecutorOptions : IUpgradeAssistantOptions
    {
        IReadOnlyCollection<string> Content { get; }

        bool Obsoletion { get; }

        bool AllowPrerelease { get; }

        IReadOnlyCollection<Platform> Platform { get; }
    }

    public enum Platform
    {
        Linux,
        Windows
    }
}
