// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Microsoft.DotNet.UpgradeAssistant.Cli
{
    public interface IUpgradeAssistantOptions
    {
        public bool IsVerbose { get; }

        public FileInfo Project { get; }

        bool IgnoreUnsupportedFeatures { get; }

        UpgradeTarget TargetTfmSupport { get; }

        IReadOnlyCollection<string> Extension { get; }

        IEnumerable<AdditionalOption> AdditionalOptions { get; }
    }
}
